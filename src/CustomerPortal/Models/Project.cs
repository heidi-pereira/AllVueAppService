using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BrandVue.EntityFramework.MetaData;

namespace CustomerPortal.Models
{
    public class Project
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public string SubProductId { get; set; }
        [Required]
        public string UniqueSurveyId { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public bool IsOpen { get; set; }
        [Required]
        public bool IsPaused { get; set; }
        [Required]
        public bool IsClosed { get; set; }
        [Required]
        public int Complete { get; set; }
        [Required]
        public int Target { get; set; }
        [Required]
        public int PercentComplete { get; set; }
        public DateTime? LaunchDate { get; set; }
        public DateTime? CompleteDate { get; set; }
        [Required]
        public ProjectType ProjectType { get; set; }
        [Required]
        public string DataPageUrl { get; set; }
        [Required]
        public string ReportsPageUrl { get; set; }
        [Required]
        public int[] ChildSurveysIds { get; set; }
        [Required]
        public int NumberOfUsers { get; set; }
        [Required]
        public bool IsSharedWithAllUsers { get; set; }
        [Required]
        public bool IsQuotaTabAvailable { get; set; }
        [Required]
        public bool IsDocumentsTabAvailable { get; set; }
        [Required]
        public List<CustomUIIntegration> CustomIntegrations { get; set; }
        public bool IsHelpIconAvailable { get; set; }
        [Required]
        public AllVueDocumentationConfiguration AllVueDocumentationConfiguration { get; set; }
        [Required]
        public string CompanyAuthId { get; set; }
    }

    public enum ProjectType
    {
        Survey,
        SurveyGroup,
    }
}