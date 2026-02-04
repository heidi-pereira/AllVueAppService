using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashboardMetadataBuilder.MapProcessing.Typed
{
    [AttributeUsage(AttributeTargets.Property)]
    class ColumnAttribute : Attribute
    {
        public int? Index { get; set; } = null;
        public string Name { get; set; } = null;

        public ColumnAttribute(int index)
        {
            Index = index;
        }

        public ColumnAttribute()
        {
        }
    }
}
