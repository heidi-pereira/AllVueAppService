using System.Diagnostics;

namespace BrandVue.Models
{
    [DebuggerDisplay("{Name}-{Organisation}")]
    public class EntitySetAverageMappingModel
    {
        public int Id { get; set; }
        public int ParentEntitySetId { get; set; }
        public int ChildEntitySetId { get; set; }
        public bool ExcludeMainInstance { get; set; }
    }
}
