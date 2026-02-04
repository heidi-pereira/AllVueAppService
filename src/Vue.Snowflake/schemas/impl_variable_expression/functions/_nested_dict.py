from typing import Any, List, Dict


class NestedDict:
    """
    A dictionary-like structure optimized for multi-dimensional lookups.
    Crucial when evaluating the same expression for many entity combinations per response (e.g. Positive_buzz)
    
    Build once from items, then query efficiently by providing lists of values
    for each dimension. Acts like a specialized index for filtering data.
    Feel free to replace with some other approach like a dictionary of tuples, or something from an existing library.
    
    Structure by depth:
    - 1 key:  {key1: [value1, value2, ...]}
    - 2 keys: {key1: {key2: [value1, value2, ...]}}
    - 3 keys: {key1: {key2: {key3: [value1, value2, ...]}}}
    
    Examples:
        >>> items = [[1, 10, 'a'], [1, 10, 'b'], [1, 20, 'c'], [2, 10, 'd']]
        >>> nd = NestedDict(items, key_indices=[0, 1])
        >>> nd[[1], [10, 20]]  # Get values where first key is 1 and second key is 10 or 20
        ['a', 'b', 'c']
        >>> nd[[1, 2], [10]]  # Get values where first key is 1 or 2, and second key is 10
        ['a', 'b', 'd']
    """
    
    def __init__(self, items: List[List[Any]], key_indices: List[int]):
        """
        Build a nested dictionary from items.
        
        Args:
            items: List of lists where each inner list contains keys and a final value
            key_indices: Indices within each item to use as keys (in nesting order)
        """
        self._key_indices = key_indices
        self._depth = len(key_indices)
        self._data = self._build(items)
    
    def _build(self, items: List[List[Any]]) -> Dict:
        """Build the internal nested dictionary structure."""
        if not self._key_indices:
            # No keys means just collect all values
            return [item[-1] for item in items]
        
        nested_dict = {}
        
        for item in items:
            current_level = nested_dict
            
            # Navigate/create nested structure for all but the last key
            for idx in self._key_indices[:-1]:
                key = item[idx]
                if key not in current_level:
                    current_level[key] = {}
                current_level = current_level[key]
            
            # At the final level, store the values in a list
            final_key = item[self._key_indices[-1]]
            if final_key not in current_level:
                current_level[final_key] = []
            current_level[final_key].append(item[-1])
        
        return nested_dict
    
    def __getitem__(self, key_values: List[List[Any]]) -> List[Any]:
        """
        Lookup values by providing a list of possible values for each dimension.
        
        Args:
            key_values: List of lists, where each inner list contains possible values
                       for that key level. Must match the number of key_indices.
        
        Returns:
            Flat list of all matching values
        """
        if len(key_values) != self._depth:
            raise ValueError(
                f"Expected {self._depth} key dimensions, got {len(key_values)}"
            )
        return self._lookup(self._data, key_values, depth=0)
    
    def _lookup(self, nested_dict: Dict, key_values: List[List[Any]], depth: int) -> List[Any]:
        """Recursively lookup values in nested dictionary structure."""
        if depth == self._depth - 1:
            # At final level: collect all values for the requested keys
            final_keys = key_values[depth]
            result = []
            for key in final_keys:
                if key in nested_dict:
                    result.extend(nested_dict[key])
            return result
        else:
            # Navigate deeper into nested structure
            current_keys = key_values[depth]
            result = []
            for key in current_keys:
                if key in nested_dict:
                    result.extend(self._lookup(
                        nested_dict[key], 
                        key_values, 
                        depth + 1
                    ))
            return result
    
    def get(self, key_values: List[List[Any]], default: Any = None) -> List[Any]:
        """
        Get values with a default fallback.
        
        Args:
            key_values: List of lists for lookup
            default: Value to return if lookup fails or finds nothing
        
        Returns:
            List of matching values or default
        """
        try:
            result = self[key_values]
            return result if result else default
        except (KeyError, ValueError):
            return default
    
    def __repr__(self) -> str:
        return f"NestedDict(depth={self._depth}, data={self._data!r})"
    
    def __len__(self) -> int:
        """Return count of entries at the first level."""
        return len(self._data)
