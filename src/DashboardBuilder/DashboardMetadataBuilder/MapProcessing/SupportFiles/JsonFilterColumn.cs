namespace DashboardMetadataBuilder.MapProcessing.SupportFiles
{
    public class JsonFilterColumn
    {
        public string ColumnName { get; }
        public int Value { get; }

        public JsonFilterColumn(string columnName, int value)
        {
            ColumnName = columnName;
            Value = value;
        }
    }
}