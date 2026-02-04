namespace BrandVue.EntityFramework.MetaData.Reports
{
    public class TemplateImportLog()
    {
        public List<ImportLog> Logs { get; set; } = new List<ImportLog>();

        public void AddLog(EventType eventType, string message, Severity severity)
        {
            Logs.Add(new ImportLog
            {
                EventType = eventType,
                Message = message,
                Severity = severity,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public class ImportLog
    {
        public EventType EventType { get; set; }
        public string Message { get; set; }
        public Severity Severity { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum Severity
    {
        Error,
        Warning,
        Info
    }

    public enum EventType
    {
        Variable,
        Metric,
        Part,
        Report
    }
}
