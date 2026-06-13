namespace KnowledgeIncidentAnalysisAI.Domain.Models
{
    public sealed class RecordReference
    {
        public string TableSchema { get; set; }
        public string TableName { get; set; }
        public string RecordKey { get; set; }
        public string RecordKeyValue { get; set; }

        public string DisplayName => $"{TableSchema}.{TableName} [{RecordKey}={RecordKeyValue}]";
    }
}
