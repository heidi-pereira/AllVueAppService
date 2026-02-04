using System;
using System.IO;
using BrandVue.PublicApi.Models;

namespace Test.BrandVue.FrontEnd.DataWarehouseTests
{
    public class Class
    {
        public ProductToTest ProductToTest { get; set; }
        public SurveysetDescriptor SurveySet { get; set; }
        public ClassDescriptor ClassDescriptor { get; set; }

        public Class(ProductToTest productToTest, SurveysetDescriptor surveySet, ClassDescriptor classDescriptor)
        {
            ProductToTest = productToTest;
            SurveySet = surveySet;
            ClassDescriptor = classDescriptor;
        }

        public string ToPath()
        {
            string entityTypeName = ClassDescriptor.Name;    
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            string validEntityTypeName = string.Join("_", entityTypeName.Split(invalidChars.ToCharArray(), StringSplitOptions.RemoveEmptyEntries));

            string location = $"{ProductToTest}\\{SurveySet.SurveysetId}\\config\\entityinstances_{validEntityTypeName}";

            return location;
        }
    }
}