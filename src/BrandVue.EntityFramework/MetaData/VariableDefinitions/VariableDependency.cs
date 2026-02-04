using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    [Table("VariableDependencies"), Keyless]
    public class VariableDependency
    {
        [Required]
        public int VariableId { get; set; }

        public VariableConfiguration Variable { get; set; }

        [Required]
        public int DependentUponVariableId { get; set; }

        public VariableConfiguration DependentUponVariable { get; set; }
    }
}