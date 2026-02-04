using System.Collections;

namespace BrandVue.SourceData.Calculation.Expressions
{
    /// <summary>
    /// In python variable expressions, this represents a name that can either be used directly (implicitly using the entity values of the result in context), or as a function result.identifier(optionalentityspecifier=1)
    /// </summary>
    internal interface IVariableInstance : IVariable
    {
        string Identifier { get; }
        EntitiesReducer<Numeric> CreateNumericForEntities();
        EntitiesReducer<Memory<Numeric>> EnumerableForEntities(
            IReadOnlyCollection<ParsedArg> parsedArgs);

        /// <summary>
        /// Resulting function is NOT THREAD SAFE (see InstanceListVariable.CreateForSingleEntityIdAnswer impl)
        /// Future: The func passed will have access to EntityIds for multi-entity or non-entity-valued variables
        /// </summary>
        Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity();
    }

    public record ParsedArg(string Name, ICurriedReducer ForEntities);
}
