using DashboardMetadataBuilder.MapProcessing.SupportFiles;

namespace BrandVueBuilder
{
    internal class NumberValueConstraint : IFieldConstraint
    {
        private readonly string _column;
        private readonly int _value;

        public NumberValueConstraint(string column, int value)
        {
            _column = column;
            _value = value;
        }

        public JsonFilterColumn GenerateFilterColumnOrNull()
        {
            return new JsonFilterColumn(_column, _value);
        }
    }
}