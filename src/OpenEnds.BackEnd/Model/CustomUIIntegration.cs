namespace OpenEnds.BackEnd.Model
{
    public class CustomUIIntegration
    {
        public enum IntegrationStyle
        {
            Tab,
            Help,
        }

        public enum IntegrationPosition
        {
            Left,
            Right,
        }

        public enum IntegrationReferenceType
        {
            WebLink,
            ReportVue,
            SurveyManagement,
            Page,
        }

        public CustomUIIntegration()
        {

        }

        public CustomUIIntegration(CustomUIIntegration other)
        {
            Style = other.Style;
            Position = other.Position;
            ReferenceType = other.ReferenceType;
            Icon = other.Icon;
            Name = other.Name;
            AltText = other.AltText;
            Path = other.Path;
        }

        public string Path { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string AltText { get; set; }
        public IntegrationStyle Style { get; set; }
        public IntegrationPosition Position { get; set; }
        public IntegrationReferenceType ReferenceType { get; set; }
    }
}