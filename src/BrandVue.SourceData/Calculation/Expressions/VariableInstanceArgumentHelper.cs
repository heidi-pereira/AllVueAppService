namespace BrandVue.SourceData.Calculation.Expressions
{
    internal static class VariableInstanceArgumentHelper
    {
        public static EntityType[] ValidateContextSensitiveEntityTypes(IReadOnlyCollection<ParsedArg> parsedArgs,
            IReadOnlyCollection<EntityType> entityCombination)
        {
            var unmatchedArgs = parsedArgs.Where(arg => entityCombination.All(et => et.Identifier != arg.Name)).ToArray();
            if (unmatchedArgs.Any())
                throw new ArgumentException(
                    $"Argument names {unmatchedArgs.Select(a => a.Name).JoinAsQuotedList()} must exactly match field entity names {entityCombination.Select(a => a.Identifier).JoinAsQuotedList()}");
            var omittedEntityTypes = entityCombination.Where(et => parsedArgs.All(a => a.Name != et.Identifier)).ToArray();
            return omittedEntityTypes;
        }
    }
}