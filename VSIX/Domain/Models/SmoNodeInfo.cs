namespace KnowledgeIncidentAnalysisAI.Domain.Models
{
    public sealed class SmoNodeInfo
    {
        public string UrnPath { get; set; }
        public string NavigationContext { get; set; }
        public string Server { get; set; }
        public string Database { get; set; }
        public string Table { get; set; }
        public string View { get; set; }
        public string StoredProcedure { get; set; }
        public string Function { get; set; }
        public string Schema { get; set; }
        public string Job { get; set; }
        public string InvariantName { get; set; }

        public bool HasDatabaseContext => !string.IsNullOrEmpty(Server) && !string.IsNullOrEmpty(Database);
    }
}
