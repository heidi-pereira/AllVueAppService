using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VueReporting.Models
{
    public class QueueStatus
    {
        public IEnumerable<QueueSystem.ItemInfo> Items { get; set; }
        public string GeneratedReportsFolderLink { get; set; }
    }
}
