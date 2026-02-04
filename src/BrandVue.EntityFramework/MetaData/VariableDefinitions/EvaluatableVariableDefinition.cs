#nullable enable

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    /// <summary>
    /// Abstract base class for variable definitions that can be evaluated using Python expressions.
    /// Excludes QuestionVariableDefinition which does not support Python expression evaluation.
    /// </summary>
    public abstract class EvaluatableVariableDefinition : VariableDefinition
    {
        /// <summary>
        /// Cached Python expression for this variable definition. 
        /// This is automatically populated when the variable is saved and kept up to date.
        /// </summary>
        public string? CachedPythonExpression { get; set; }
    }
}