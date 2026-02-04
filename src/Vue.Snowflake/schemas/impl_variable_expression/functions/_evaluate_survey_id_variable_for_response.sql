-- Non-vectorized scalar UDF for evaluating survey ID variables at respondent level
-- Maps respondent survey ID to survey ID group entity instances
-- Returns array of answer arrays for requested survey ID group entity combinations
-- Optimized with dictionary lookup for O(1) survey ID matching

create or replace function impl_variable_expression._evaluate_survey_id_variable_for_response(
    response_id integer,
    response_survey_id integer,
    definition object,
    requested_survey_group_entity_ids array  -- Array of survey group entity IDs to evaluate (or null for all groups)
)
returns array  -- Array of arrays: [[asked_entity_id_1, asked_entity_id_2, asked_entity_id_3, answer_entity_id, answer_value], ...]
language python
immutable
runtime_version = '3.13'
handler = 'evaluate_survey_id_variable_for_response'
as $$
import logging
from dataclasses import dataclass
from typing import List

logger = logging.getLogger(__name__)

@dataclass
class SurveyIdComponent:
    """Represents a SurveyIdVariableComponent from C#"""
    SurveyIds: List[int]

@dataclass
class SurveyGroup:
    """Represents a VariableGrouping from GroupedVariableDefinition"""
    ToEntityInstanceId: int
    Component: dict  # Will be converted to SurveyIdComponent
    
    def __post_init__(self):
        # Auto-convert Component dict to SurveyIdComponent
        if isinstance(self.Component, dict):
            self.Component = SurveyIdComponent(**self.Component)

def parse_survey_id_definition(definition):
    """
    Parse GroupedVariableDefinition to extract survey ID group configurations.
    
    Args:
        definition: Object with Groups array, each containing:
            - ToEntityInstanceId: integer group entity ID
            - Component: Object with SurveyIds array
    
    Returns:
        Tuple of (group_id_to_survey_ids, survey_id_to_group_ids)
        - group_id_to_survey_ids: dict mapping group entity ID to set of survey IDs
        - survey_id_to_group_ids: dict mapping survey ID to list of group entity IDs
    """
    if not definition or 'Groups' not in definition:
        logger.error(f"Invalid definition: missing Groups. Definition: {definition}")
        return {}, {}
    
    groups_data = definition['Groups']
    if not groups_data:
        return {}, {}
    
    group_id_to_survey_ids = {}
    survey_id_to_group_ids = {}
    
    for group_dict in groups_data:
        try:
            # Deserialize directly to dataclass
            group = SurveyGroup(**group_dict)
            group_id = group.ToEntityInstanceId
            survey_ids = group.Component.SurveyIds
            
            # Store group -> survey IDs mapping (as set for O(1) lookup)
            group_id_to_survey_ids[group_id] = set(survey_ids)
            
            # Build reverse lookup: survey ID -> list of group IDs
            for survey_id in survey_ids:
                if survey_id not in survey_id_to_group_ids:
                    survey_id_to_group_ids[survey_id] = []
                survey_id_to_group_ids[survey_id].append(group_id)
            
        except Exception as e:
            logger.error(f"Failed to parse survey group: {group_dict}")
            logger.exception(e)
            continue
    
    return group_id_to_survey_ids, survey_id_to_group_ids

def find_matching_groups(response_survey_id, survey_id_to_group_ids, requested_group_ids=None):
    """
    Find all survey ID groups that contain the given survey ID.
    Uses dictionary lookup for O(1) survey ID matching.
    
    Args:
        response_survey_id: Survey ID to check
        survey_id_to_group_ids: Dict mapping survey ID to list of group entity IDs
        requested_group_ids: Set of group entity IDs to filter results (or None for all)
    
    Returns:
        List of matching group entity IDs
    """
    # O(1) lookup for survey ID
    if response_survey_id not in survey_id_to_group_ids:
        return []
    
    group_ids = survey_id_to_group_ids[response_survey_id]
    
    # Apply filter if requested_group_ids specified
    if requested_group_ids is not None:
        group_ids = [gid for gid in group_ids if gid in requested_group_ids]
    
    return group_ids

def evaluate_survey_id_variable_for_response(
    response_id, response_survey_id, definition, requested_survey_group_entity_ids
):
    """
    Evaluate survey ID variable for a single response.
    
    Args:
        response_id: Integer response ID (for logging)
        response_survey_id: Survey ID of the response
        definition: GroupedVariableDefinition object with survey ID group configurations
        requested_survey_group_entity_ids: Array of group entity IDs to evaluate (or None for all)
    
    Returns:
        Array of answer arrays: [[None, None, None, group_entity_id, group_entity_id], ...]
        Each matching group returns an answer where:
        - First 3 elements are None (no asked entities for survey ID variables)
        - 4th element is the group entity ID (answer_entity_id)
        - 5th element is the group entity ID as the answer value
        
        Returns None if no groups match
    """
    try:
        # Parse survey ID definitions from the definition object
        group_id_to_survey_ids, survey_id_to_group_ids = parse_survey_id_definition(definition)
        
        if not survey_id_to_group_ids:
            logger.warning(f"No valid survey groups found in definition for response {response_id}")
            return None
        
        # Convert requested_survey_group_entity_ids to set for O(1) lookup
        requested_group_set = set(requested_survey_group_entity_ids) if requested_survey_group_entity_ids else None
        
        # Find all matching groups for this survey ID
        matching_group_ids = find_matching_groups(response_survey_id, survey_id_to_group_ids, requested_group_set)
        
        if not matching_group_ids:
            return None
        
        # Build result array in the standard format
        # Survey ID variables have no asked entities (first 3 are None)
        # The answer_entity_id and answer_value are both the group entity ID
        result = []
        for group_id in matching_group_ids:
            result.append([None, None, None, group_id, group_id])
        
        return result
        
    except Exception as e:
        logger.error(f"Failed to evaluate survey ID variable for response {response_id}")
        logger.error(f"response_survey_id: {response_survey_id}, definition: {definition}")
        logger.exception(e)
        return None
$$;
