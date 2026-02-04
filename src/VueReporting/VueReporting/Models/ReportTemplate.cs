using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace VueReporting.Models
{
    public class ReportTemplate: BaseEntity
    {

        public ReportTemplate()
        {
            DateCreated = DateModified = DateTime.UtcNow;
            PowerPointFileData = new PowerPointFileData();
        }

        [Required]
        public string Name { get; set; }
        [Required]
        public string MetaDescription { get; set; }

        [Required]
        public string UserName { get; set; }
        [Required]
        public string ProductName { get; set; }

        [Required]
        public DateTime DateCreated { get; set; }

        [Required]
        public DateTime DateModified { get; set; }

        [Required]
        [JsonIgnore]
        public PowerPointFileData PowerPointFileData { get; set; }
    }

    public class PowerPointFileData : BaseEntity {
        public string FileExtension => "pptx";
        [Required]
        public ReportTemplate ReportTemplate { get; set; }
        [Required]
        public byte[] PowerPointTemplate { get; set; }
    }
}