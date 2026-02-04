using System.Collections.Generic;
using DashboardMetadataBuilder.MapProcessing.Schema.Sheets;

namespace BrandVueBuilder
{
    internal class TextLookup
    {
        public string Name { get; }
        public IReadOnlyCollection<TextLookupData> Data { get; }

        public TextLookup(string name, IReadOnlyCollection<TextLookupData> data)
        {
            Name = name;
            Data = data;
        }
    }
}