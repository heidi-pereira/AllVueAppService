namespace BrandVue.Models
{
    public class VariableWarningModel
    {
        public ObjectThatReferencesVariable ObjectThatReferencesVariable { get;set;}
        public IEnumerable<string> Names {get;set;}
    }

    public enum ObjectThatReferencesVariable
    {
        Report,
        Break,
        Variable,
        Weighting,
        Filter,
        Wave,
        Base
    }
}
