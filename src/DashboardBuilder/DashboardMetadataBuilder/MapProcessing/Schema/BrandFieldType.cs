using System;

namespace DashboardMetadataBuilder.MapProcessing.Schema
{
    public enum BrandFieldType
    {
        /// <summary>
        /// Arbitrary text in the [data].[text] field. This currently passes straight through DashboardBuilder and BrandVue reads from the Temp database.
        /// </summary>
        OpenText,
        /// <summary>
        /// Numeric value in the [data].[optValue] field.
        /// </summary>
        Value
    }

    public class BrandFieldTypeHelper
    {
        public static string EnumsToString(string separator = ", ")
        {
            return string.Join(separator, Enum.GetNames(typeof(BrandFieldType)));
        }
    }
}