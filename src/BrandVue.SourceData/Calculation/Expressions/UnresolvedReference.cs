using System.Data;

namespace BrandVue.SourceData.Calculation.Expressions
{
    internal class UnresolvedReference : IVariableInstance
    {
        public string Identifier { get; }
        public string ErrorMessage => $"Could not resolve variable/question `{Identifier}`";

        public UnresolvedReference(string unresolvedVariableName) => Identifier = unresolvedVariableName;

        public EntitiesReducer<Numeric> CreateNumericForEntities() => new (_ => throw new InvalidExpressionException(ErrorMessage));

        public EntitiesReducer<Memory<Numeric>> EnumerableForEntities(
            IReadOnlyCollection<ParsedArg> parsedArgs) =>
            new(_ => throw new InvalidExpressionException(ErrorMessage));

        public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity() =>
            _ => throw new InvalidExpressionException(ErrorMessage);

        public bool OnlyDimensionIsEntityType() => false;
        public IEnumerable<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)> GetDatabaseOnlyDataTargets(Subset subset) =>
            throw new InvalidExpressionException(ErrorMessage);
        public IReadOnlyCollection<EntityType> UserEntityCombination { get; } = Array.Empty<EntityType>();
        public IReadOnlyCollection<ResponseFieldDescriptor> FieldDependencies { get; } = Array.Empty<ResponseFieldDescriptor>();
    }
}