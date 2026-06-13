namespace KnowledgeIncidentAnalysisAI.Domain.Models
{
    public sealed class DatabaseObjectReference
    {
        public string ObjectType { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }

        public string DisplayName => string.IsNullOrEmpty(Schema) ? Name : $"{Schema}.{Name}";
    }
}
