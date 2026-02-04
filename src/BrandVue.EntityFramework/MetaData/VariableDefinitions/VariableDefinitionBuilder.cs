namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{

    public class GroupedVariableDefinitionBuilder
    {
        private GroupedVariableDefinition Definition { get; set; }

        public GroupedVariableDefinitionBuilder(string toEntityTypeName)
        {
            Definition = new GroupedVariableDefinition
            {
                ToEntityTypeName = toEntityTypeName,
                ToEntityTypeDisplayNamePlural = toEntityTypeName,
                Groups = new List<VariableGrouping>()
            };
        }

        public GroupedVariableDefinitionBuilder WithInclusiveRangeGroup(string descriptor, int min, int max,
            string fieldId, VariableRangeComparisonOperator op)
        {
            var definition = Definition;
            definition.Groups.Add(new VariableGrouping
            {
                ToEntityInstanceName = descriptor,
                ToEntityInstanceId = definition.Groups.Count + 1,
                Component = new InclusiveRangeVariableComponent()
                {
                    Min = min,
                    Max = max,
                    FromVariableIdentifier = fieldId,
                    Operator = op
                }
            });
            return this;
        }

        public GroupedVariableDefinitionBuilder WithGreaterThanGroup(string descriptor, int min, string fieldId)
        {
            var definition = Definition;
            definition.Groups.Add(new VariableGrouping
            {
                ToEntityInstanceName = descriptor,
                ToEntityInstanceId = definition.Groups.Count + 1,
                Component = new InclusiveRangeVariableComponent()
                {
                    Min = min,
                    FromVariableIdentifier = fieldId,
                    Operator = VariableRangeComparisonOperator.GreaterThan
                }
            });
            return this;
        }

        public GroupedVariableDefinition Build()
        {
            return Definition;
        }
    }
}