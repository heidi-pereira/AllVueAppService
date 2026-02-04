-- Non-vectorized scalar UDF for evaluating data wave variables at respondent level
-- Maps respondent timestamp to wave entity instances based on date ranges
-- Returns array of answer arrays for requested wave entity combinations
-- Optimized with binary search for O(log n) timestamp matching

create or replace function impl_variable_expression._evaluate_data_wave_variable_for_response(
    response_id integer,
    response_timestamp timestamp_ntz,
    definition object,
    requested_wave_entity_ids array  -- Array of wave entity IDs to evaluate (or null for all waves)
)
returns array  -- Array of arrays: [[asked_entity_id_1, asked_entity_id_2, asked_entity_id_3, answer_entity_id, answer_value], ...]
language python
immutable
runtime_version = '3.13'
handler = 'evaluate_data_wave_variable_for_response'
as $$
import logging
from bisect import bisect_left
from dataclasses import dataclass
from datetime import datetime
from typing import Optional

logger = logging.getLogger(__name__)

@dataclass
class DateRangeComponent:
    """Represents a DateRangeVariableComponent from C#"""
    MinDate: str
    MaxDate: str
    
    @property
    def min_datetime(self) -> datetime:
        return datetime.fromisoformat(self.MinDate.replace('Z', '+00:00'))
    
    @property
    def max_datetime(self) -> datetime:
        return datetime.fromisoformat(self.MaxDate.replace('Z', '+00:00'))

@dataclass
class WaveGroup:
    """Represents a VariableGrouping from GroupedVariableDefinition"""
    ToEntityInstanceId: int
    Component: dict  # Will be converted to DateRangeComponent
    
    def __post_init__(self):
        # Auto-convert Component dict to DateRangeComponent
        if isinstance(self.Component, dict):
            self.Component = DateRangeComponent(**self.Component)

@dataclass
class Wave:
    """Represents a single wave with date range and entity ID, optimized for lookup"""
    entity_id: int
    min_timestamp: float
    max_timestamp: float
    
    @classmethod
    def from_group(cls, group: WaveGroup) -> 'Wave':
        """Create Wave from WaveGroup with timestamp caching"""
        component = group.Component
        min_dt = component.min_datetime
        max_dt = component.max_datetime
        return cls(
            entity_id=group.ToEntityInstanceId,
            min_timestamp=min_dt.timestamp(),
            max_timestamp=max_dt.timestamp()
        )

def parse_wave_definition(definition):
    """
    Parse GroupedVariableDefinition to extract wave configurations.
    
    Args:
        definition: Object with Groups array, each containing:
            - ToEntityInstanceId: integer wave entity ID
            - Component: Object with MinDate and MaxDate
    
    Returns:
        List of Wave objects sorted by max_date, then min_date
    """
    if not definition or 'Groups' not in definition:
        logger.error(f"Invalid definition: missing Groups. Definition: {definition}")
        return []
    
    groups_data = definition['Groups']
    if not groups_data:
        return []
    
    waves = []
    for group_dict in groups_data:
        try:
            # Deserialize directly to dataclass
            group = WaveGroup(**group_dict)
            wave = Wave.from_group(group)
            waves.append(wave)
            
        except Exception as e:
            logger.error(f"Failed to parse wave group: {group_dict}")
            logger.exception(e)
            continue
    
    # Sort by max_date (primary), then min_date (secondary) for binary search optimization
    waves.sort(key=lambda w: (w.max_timestamp, w.min_timestamp))
    
    return waves

def find_matching_waves(response_timestamp, waves, requested_wave_ids=None):
    """
    Find all waves that contain the given timestamp.
    Uses binary search to find first candidate wave efficiently.
    
    Args:
        response_timestamp: Timestamp to check
        waves: Sorted list of Wave objects
        requested_wave_ids: Set of wave entity IDs to filter results (or None for all)
    
    Returns:
        List of matching wave entity IDs
    """
    if not waves:
        return []
    
    # Convert timestamp to comparable format
    if isinstance(response_timestamp, datetime):
        ts = response_timestamp.timestamp()
    else:
        ts = response_timestamp
    
    # Extract max_date timestamps for binary search
    max_timestamps = [w.max_timestamp for w in waves]
    
    # Binary search: find first wave where max_date >= response_timestamp
    # This is the C# GetIndexOfFirstWaveWithMaxDateExceeding logic
    start_index = bisect_left(max_timestamps, ts)
    
    matching_waves = []
    
    # Check all waves from start_index onwards
    for i in range(start_index, len(waves)):
        wave = waves[i]
        
        # Check if timestamp falls within [min_date, max_date]
        if wave.min_timestamp <= ts <= wave.max_timestamp:
            # Apply filter if requested_wave_ids specified
            if requested_wave_ids is None or wave.entity_id in requested_wave_ids:
                matching_waves.append(wave.entity_id)
        elif wave.min_timestamp > ts:
            # Since waves are sorted by max_date, we can stop once min_date exceeds timestamp
            break
    
    return matching_waves

def evaluate_data_wave_variable_for_response(
    response_id, response_timestamp, definition, requested_wave_entity_ids
):
    """
    Evaluate data wave variable for a single response.
    
    Args:
        response_id: Integer response ID (for logging)
        response_timestamp: Timestamp of the response
        definition: GroupedVariableDefinition object with wave configurations
        requested_wave_entity_ids: Array of wave entity IDs to evaluate (or None for all)
    
    Returns:
        Array of answer arrays: [[None, None, None, wave_entity_id, wave_entity_id], ...]
        Each matching wave returns an answer where:
        - First 3 elements are None (no asked entities for data wave variables)
        - 4th element is the wave entity ID (answer_entity_id)
        - 5th element is the wave entity ID as the answer value
        
        Returns None if no waves match
    """
    try:
        # Parse wave definitions from the definition object
        waves = parse_wave_definition(definition)
        
        if not waves:
            logger.warning(f"No valid waves found in definition for response {response_id}")
            return None
        
        # Convert requested_wave_entity_ids to set for O(1) lookup
        requested_wave_set = set(requested_wave_entity_ids) if requested_wave_entity_ids else None
        
        # Find all matching waves for this timestamp
        matching_wave_ids = find_matching_waves(response_timestamp, waves, requested_wave_set)
        
        if not matching_wave_ids:
            return None
        
        # Build result array in the standard format
        # Data wave variables have no asked entities (first 3 are None)
        # The answer_entity_id and answer_value are both the wave entity ID
        result = []
        for wave_id in matching_wave_ids:
            result.append([None, None, None, wave_id, wave_id])
        
        return result
        
    except Exception as e:
        logger.error(f"Failed to evaluate data wave variable for response {response_id}")
        logger.error(f"response_timestamp: {response_timestamp}, definition: {definition}")
        logger.exception(e)
        return None
$$;
