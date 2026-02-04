using System;
using System.Linq;
using DashboardMetadataBuilder.MapProcessing.Typed;

namespace DashboardMetadataBuilder.MapProcessing.Schema.Sheets
{
    public class TextLookupData : SheetRow
    {
        public short Id { get; internal set; }
        public string Lookup { get; internal set; }

        private string[] _lookupValues;

        public string[] LookupValues => _lookupValues ??= Lookup
                                        .Split(new []{'|'}, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(lookupWord => lookupWord.Trim())
                                        .ToArray();
    }
}