import logging
from itertools import product
from _nested_dict import NestedDict

logger = logging.getLogger(__name__)

class Response:
    def __init__(self, dependency_shapes, dependency_answers):
        """
        Build a Response object with QuestionVariable attributes.
        For each dependency adds a callable QuestionVariable attribute to allow response.Positive_buzz(...)
        
        Args:
            dependency_shapes: dict mapping variable names to entity types
            dependency_answers: dict mapping variable names to answer data
        """
        for var_name, var_data in dependency_answers.items():
            if var_data is not None and var_name in dependency_shapes:
                entity_types = dependency_shapes[var_name]
                setattr(self, var_name, QuestionVariable(entity_types, var_data))


class QuestionVariable:
    def __init__(self, entity_types, answers):
        self.entity_types = entity_types
        self.answers = answers

        # Line up the entities with answer shape skipping Nones.
        # Answers are of shape [asked_entity_1, asked_entity_2, asked_entity_3, answer_entity].
        example_answer = answers and answers[0] or [0, 0, 0, 0]
        populated_indices = [i for i, v in enumerate(example_answer) if v is not None]
        self.index_from_entity = {dim: idx for dim, idx in zip(self.entity_types, populated_indices)}
        
        # Cache for adaptive O(1) lookups: maps kwarg signature to NestedDict
        # Structure: {(kwarg_entity1, kwarg_entity2, ...): NestedDict}
        self._cache_by_kwargs = {}
    
    def __call__(self, **kwargs):
        """
        Makes this object callable like a function with keyword arguments. e.g. Positive_buzz(brand=[3,4], product = 5)
        """
        if not kwargs:
            return [answer[-1] for answer in self.answers]

        # Need a consistent order. Doesn't matter what it is, but nice to be consistent with the variable's entity order.
        kwarg_signature = tuple(sorted(kwargs.keys(), key=lambda k: self.index_from_entity[k]))

        # Normalize values to lists and extract in consistent order
        filter_instances = [
            kwargs[kw] if isinstance(kwargs[kw], list) else [kwargs[kw]]
            for kw in kwarg_signature
        ]

        # Build cache for this kwarg combination if not yet created
        if kwarg_signature not in self._cache_by_kwargs:
            # kwarg_signature is the ordered tuple of kwarg names used as the cache key
            kwarg_indices = [self.index_from_entity[kw] for kw in kwarg_signature]
            self._cache_by_kwargs[kwarg_signature] = NestedDict(self.answers, kwarg_indices)
        answers = self._cache_by_kwargs[kwarg_signature]
        
        return answers[filter_instances]


class Result:
    def __init__(self, entity_combination, entity_names):
        """
        Build a Result object with an attribute for each entity
        Allows accessing like result.brand if brand is in entity_names for example
        
        Args:
            entity_combination: tuple of entity IDs for this combination
            entity_names: array of entity dimension names
        """
        entity_values = list(entity_combination)
        for key, value in zip(entity_names, entity_values):
            if value is not None:
                setattr(self, key, value)


def compile_expression(variable_expression):
    """
    Compile a variable expression into a callable lambda.
    
    Args:
        variable_expression: string expression to evaluate
    
    Returns:
        Function to evaluate the expression with response and result args
    """    
    try:
        return eval(f'lambda response, result: {variable_expression}')
    except Exception as e:
        logger.error(f"Failed to compile expression: {variable_expression}")
        logger.exception(e)
        raise e


def normalize_inputs(entity_names, entity_instance_arrays, dependency_shapes, dependency_answers):
    """
    Normalize and coalesce inputs for evaluate_expression_core_for_response.

    Returns a tuple: (entity_names, entity_instance_arrays, dependency_shapes, dependency_answers)
    """
    # Ensure entity names is a list
    entity_names = list(entity_names) if entity_names else []

    # Coalesce dict-like inputs
    dependency_shapes = dependency_shapes or {}
    dependency_answers = dependency_answers or {}

    # Normalize entity instance arrays to length-4 arrays
    # Example: [[1,2,3], [10,11]] becomes [[1,2,3], [10,11], [None], [None]]
    entity_instance_arrays = entity_instance_arrays or []
    entity_instance_arrays = entity_instance_arrays + [[None]] * (4 - len(entity_instance_arrays))

    return (entity_names, entity_instance_arrays, dependency_shapes, dependency_answers)


def evaluate_expression_core_for_response(
    response_id, variable_expression, entity_names, entity_instance_arrays,
    dependency_shapes, dependency_answers
):
    """
    Process a single respondent and return array of answer arrays.
    
    Args:
        response_id: integer response_id
        variable_expression: string expression to evaluate
        entity_names: array of entity dimension names
        entity_instance_arrays: array of arrays, one per entity dimension
        dependency_shapes: object mapping variable names to entity types
        dependency_answers: object mapping variable names to answer data
    
    Returns:
        Array of arrays: [[asked_entity_id_1, asked_entity_id_2, asked_entity_id_3, answer_entity_id, answer_value], ...]
    """
    (entity_names, entity_instance_arrays, dependency_shapes, dependency_answers) = normalize_inputs(
        entity_names, entity_instance_arrays, dependency_shapes, dependency_answers
    )

    evaluate_expression_core = compile_expression(variable_expression)
    response = Response(dependency_shapes, dependency_answers)
    
    def eval_for_entity_combination(entity_combination):
        result = Result(entity_combination, entity_names)
        return evaluate_expression_core(response, result)
    
    respondent_answers = [
        list(entity_combination) + [int(answer_value)]
        for entity_combination in product(*entity_instance_arrays)
        # answer_value of None means "no answer"
        if (answer_value := eval_for_entity_combination(entity_combination)) is not None
    ]
    
    return respondent_answers
