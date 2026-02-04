using System.Collections.Generic;

namespace DashboardMetadataBuilder.MapProcessing.Typed
{
    /// <summary>
    /// All int/string/DateTime/enum properties with setters will be populated from columns regardless of accessibility.
    /// This means if the value needs processing before use, you can make a column property private, then provide a public get-only view over that property 
    /// </summary>
    public abstract class SheetRow
    {
        private Dictionary<string, string> _extraColumns = new Dictionary<string, string>();
        public Dictionary<string, string> ExtraColumns => _extraColumns;
    }
}