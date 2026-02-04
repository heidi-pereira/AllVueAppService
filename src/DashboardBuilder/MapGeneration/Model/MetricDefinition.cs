namespace MIG.SurveyPlatform.MapGeneration.Model
{
    /// <remarks>
    /// For some fields unlikely to be used I've removed the setter and set it to the empty string.
    /// Feel free to change this if you need it, it's just so it's obvious which things need setting when looking at intellisense.
    /// </remarks>
    internal class MetricDefinition
    {
        public FieldDefinition GeneratedFrom { get; }

        public MetricDefinition(FieldDefinition generatedFrom)
        {
            GeneratedFrom = generatedFrom;
        }

        public string Name { get; set; }
        public string Type { get; set; }
        public string BaseResponseType { get; set; }
        public string Field { get; set; }
        public string Field2 { get; set; }
        public string FieldOp { get; set; }
        public string CalcType { get; set; }
        public string TrueVals { get; set; }
        public string BaseField { get; set; }
        public string BaseVals { get; set; }
        public string KeyImage { get; } = "";
        public string Measure { get; set; }
        public string HelpText { get; } = "";
        public string NumFormat { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public string ExcludeWaves { get; } = "";
        public string StartDate { get; } = "";
        public string FilterValueMapping { get;} = "";
        public string FilterGroup { get; } = "";
        public string PreNormalisationMinimum { get; set; } = "";
        public string PreNormalisationMaximum { get; set; } = "";
        public string Subset { get; set; }
        public string DisableMeasure { get; } = "";
        public string DisableFilter { get; } = "";
        public string Environment { get; } = "";
    }
}