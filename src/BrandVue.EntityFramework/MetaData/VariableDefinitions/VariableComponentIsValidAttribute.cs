using System.ComponentModel.DataAnnotations;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    public class VariableComponentIsValidAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return new ValidationResult($"{nameof(VariableComponent)} must have a value");
            }
            
            var variableComponent = value as VariableComponent;

            if (variableComponent == null)
                throw new Exception($"{nameof(VariableComponentIsValidAttribute)} was used on a wrong object");
            
            if (!variableComponent.IsValid(out var errorMessage))
            {
                return new ValidationResult(errorMessage);
            }
            
            return ValidationResult.Success;
        }
    }
}