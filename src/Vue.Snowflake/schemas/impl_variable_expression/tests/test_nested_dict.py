"""
Pytest tests for NestedDict class covering boundary cases and typical usage.
"""

import sys
from pathlib import Path

# Add the functions directory to the path so we can import the module
sys.path.insert(0, str(Path(__file__).parent.parent / "functions"))

import pytest
from _nested_dict import NestedDict


class TestNestedDictBasicUsage:
    """Test basic usage patterns."""
    
    def test_single_dimension(self):
        """Test with single key dimension."""
        items = [[1, 'a'], [1, 'b'], [2, 'c']]
        nd = NestedDict(items, key_indices=[0])
        
        assert nd[[[1]]] == ['a', 'b']
        assert nd[[[2]]] == ['c']
        assert nd[[[1, 2]]] == ['a', 'b', 'c']
    
    def test_two_dimensions(self):
        """Test with two key dimensions."""
        items = [
            [1, 10, 'a'], 
            [1, 10, 'b'], 
            [1, 20, 'c'], 
            [2, 10, 'd']
        ]
        nd = NestedDict(items, key_indices=[0, 1])
        
        assert nd[[1], [10]] == ['a', 'b']
        assert nd[[1], [20]] == ['c']
        assert nd[[1], [10, 20]] == ['a', 'b', 'c']
        assert nd[[2], [10]] == ['d']
        assert nd[[1, 2], [10]] == ['a', 'b', 'd']
    
    def test_three_dimensions(self):
        """Test with three key dimensions."""
        items = [
            [1, 10, 100, 'a'],
            [1, 10, 200, 'b'],
            [1, 20, 100, 'c'],
            [2, 10, 100, 'd']
        ]
        nd = NestedDict(items, key_indices=[0, 1, 2])
        
        assert nd[[1], [10], [100]] == ['a']
        assert nd[[1], [10], [100, 200]] == ['a', 'b']
        assert nd[[1], [10, 20], [100]] == ['a', 'c']
        assert nd[[1, 2], [10], [100]] == ['a', 'd']


class TestNestedDictBoundaryConditions:
    """Test edge cases and boundary conditions."""
    
    def test_empty_items(self):
        """Test with no items."""
        nd = NestedDict([], key_indices=[0, 1])
        assert nd[[1], [10]] == []
    
    def test_empty_lookup(self):
        """Test lookup with empty value lists."""
        items = [[1, 10, 'a'], [2, 20, 'b']]
        nd = NestedDict(items, key_indices=[0, 1])
        
        assert nd[[], [10]] == []
        assert nd[[1], []] == []
        assert nd[[], []] == []
    
    def test_nonexistent_keys(self):
        """Test lookup for keys that don't exist."""
        items = [[1, 10, 'a'], [2, 20, 'b']]
        nd = NestedDict(items, key_indices=[0, 1])
        
        assert nd[[999], [10]] == []
        assert nd[[1], [999]] == []
        assert nd[[999], [999]] == []
    
    def test_mixed_existent_and_nonexistent(self):
        """Test lookup with mix of valid and invalid keys."""
        items = [[1, 10, 'a'], [1, 20, 'b'], [2, 10, 'c']]
        nd = NestedDict(items, key_indices=[0, 1])
        
        # Some keys exist, some don't
        assert nd[[1, 999], [10]] == ['a']
        assert nd[[1], [10, 999]] == ['a']
    
    def test_none_as_key(self):
        """Test using None as a key value."""
        items = [[None, 10, 'a'], [1, None, 'b'], [None, None, 'c']]
        nd = NestedDict(items, key_indices=[0, 1])
        
        assert nd[[None], [10]] == ['a']
        assert nd[[1], [None]] == ['b']
        assert nd[[None], [None]] == ['c']
        result = nd[[None, 1], [None, 10]]
        assert set(result) == {'a', 'b', 'c'}  # Order doesn't matter
    
    def test_duplicate_values(self):
        """Test items with duplicate key combinations."""
        items = [
            [1, 10, 'a'],
            [1, 10, 'b'],
            [1, 10, 'c']
        ]
        nd = NestedDict(items, key_indices=[0, 1])
        
        assert nd[[1], [10]] == ['a', 'b', 'c']


class TestNestedDictNonConsecutiveIndices:
    """Test using non-consecutive indices from source items."""
    
    def test_skip_middle_column(self):
        """Test key indices that skip columns."""
        items = [
            [1, 'ignored1', 10, 'a'],
            [1, 'ignored2', 20, 'b'],
            [2, 'ignored3', 10, 'c']
        ]
        # Use columns 0 and 2, skip column 1
        nd = NestedDict(items, key_indices=[0, 2])
        
        assert nd[[1], [10]] == ['a']
        assert nd[[1], [20]] == ['b']
        assert nd[[1, 2], [10]] == ['a', 'c']
    
    def test_reverse_order_indices(self):
        """Test key indices in non-sequential order."""
        items = [
            [10, 1, 'a'],
            [20, 1, 'b'],
            [10, 2, 'c']
        ]
        # Use indices [1, 0] instead of [0, 1]
        nd = NestedDict(items, key_indices=[1, 0])
        
        assert nd[[1], [10]] == ['a']
        assert nd[[1], [20]] == ['b']
        assert nd[[2], [10]] == ['c']


class TestNestedDictDictLikeInterface:
    """Test dict-like interface methods."""
    
    def test_getitem_wrong_dimension_count(self):
        """Test __getitem__ with wrong number of dimensions."""
        items = [[1, 10, 'a']]
        nd = NestedDict(items, key_indices=[0, 1])
        
        with pytest.raises(ValueError, match="Expected 2 key dimensions, got 1"):
            nd[[1]]
        
        with pytest.raises(ValueError, match="Expected 2 key dimensions, got 3"):
            nd[[1], [10], [999]]
    
    def test_get_method(self):
        """Test .get() method with default values."""
        items = [[1, 10, 'a'], [2, 20, 'b']]
        nd = NestedDict(items, key_indices=[0, 1])
        
        # Successful lookup
        assert nd.get([[1], [10]]) == ['a']
        
        # Failed lookup returns None by default
        assert nd.get([[999], [999]]) is None
        
        # Failed lookup with custom default
        assert nd.get([[999], [999]], default=[]) == []
        assert nd.get([[999], [999]], default='missing') == 'missing'
    
    def test_get_with_wrong_dimensions(self):
        """Test .get() method with wrong dimension count."""
        items = [[1, 10, 'a']]
        nd = NestedDict(items, key_indices=[0, 1])
        
        # Should return default on error
        assert nd.get([[1]], default='error') == 'error'
    
    def test_len(self):
        """Test __len__ method."""
        items = [[1, 10, 'a'], [2, 20, 'b'], [3, 30, 'c']]
        nd = NestedDict(items, key_indices=[0, 1])
        
        # Length is number of first-level keys
        assert len(nd) == 3
    
    def test_repr(self):
        """Test __repr__ method."""
        items = [[1, 10, 'a']]
        nd = NestedDict(items, key_indices=[0, 1])
        
        repr_str = repr(nd)
        assert 'NestedDict' in repr_str
        assert 'depth=2' in repr_str


class TestNestedDictValueTypes:
    """Test different value types stored in the nested dict."""
    
    def test_numeric_values(self):
        """Test with numeric values."""
        items = [[1, 10, 42], [1, 20, 99], [2, 10, 123]]
        nd = NestedDict(items, key_indices=[0, 1])
        
        assert nd[[1], [10]] == [42]
        assert nd[[1], [10, 20]] == [42, 99]
    
    def test_none_values(self):
        """Test with None as values."""
        items = [[1, 10, None], [1, 20, None]]
        nd = NestedDict(items, key_indices=[0, 1])
        
        assert nd[[1], [10]] == [None]
        assert nd[[1], [10, 20]] == [None, None]
    
    def test_mixed_value_types(self):
        """Test with mixed value types."""
        items = [
            [1, 10, 'string'],
            [1, 20, 42],
            [1, 30, None],
            [1, 40, [1, 2, 3]]
        ]
        nd = NestedDict(items, key_indices=[0, 1])
        
        assert nd[[1], [10, 20, 30, 40]] == ['string', 42, None, [1, 2, 3]]


class TestNestedDictPerformanceScenarios:
    """Test scenarios relevant to performance characteristics."""
    
    def test_large_single_dimension(self):
        """Test with many items in single dimension."""
        items = [[i, f'value_{i}'] for i in range(1000)]
        nd = NestedDict(items, key_indices=[0])
        
        # Single lookup
        assert nd[[[500]]] == ['value_500']
        
        # Multiple lookups
        result = nd[[[100, 200, 300]]]
        assert result == ['value_100', 'value_200', 'value_300']
    
    def test_sparse_data(self):
        """Test with sparse data (many missing combinations)."""
        items = [
            [1, 1, 'a'],
            [100, 100, 'b'],
            [1000, 1000, 'c']
        ]
        nd = NestedDict(items, key_indices=[0, 1])
        
        # Valid combinations
        assert nd[[1], [1]] == ['a']
        assert nd[[100], [100]] == ['b']
        
        # Invalid combinations return empty
        assert nd[[1], [100]] == []
        assert nd[[100], [1]] == []
    
    def test_many_values_per_key(self):
        """Test key with many associated values."""
        items = [[1, 10, f'value_{i}'] for i in range(100)]
        nd = NestedDict(items, key_indices=[0, 1])
        
        result = nd[[1], [10]]
        assert len(result) == 100
        assert result[0] == 'value_0'
        assert result[99] == 'value_99'


class TestNestedDictRealWorldScenarios:
    """Test scenarios matching real-world usage patterns."""
    
    def test_survey_question_filtering(self):
        """Test pattern matching survey question variable filtering."""
        # Simulates filtering survey answers by respondent, question, and response option
        items = [
            [1, 'Q1', 'A', 5],    # Respondent 1, Question 1, Option A, score 5
            [1, 'Q1', 'B', 3],
            [1, 'Q2', 'A', 4],
            [2, 'Q1', 'A', 2],
            [2, 'Q2', 'B', 5]
        ]
        nd = NestedDict(items, key_indices=[0, 1, 2])
        
        # Get all scores for respondent 1, question Q1
        assert nd[[1], ['Q1'], ['A', 'B']] == [5, 3]
        
        # Get scores for all respondents, question Q1, option A
        assert nd[[1, 2], ['Q1'], ['A']] == [5, 2]
        
        # Get all Q2 responses
        assert nd[[1, 2], ['Q2'], ['A', 'B']] == [4, 5]
