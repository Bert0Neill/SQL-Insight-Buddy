using System;

namespace RagService
{
    public sealed class RagIndexService
    {
        public string BuildAiIndex(string server, string database)
        {
            return $"AI index build started for [{server}].[{database}].\n\n" +
                   "This will index schema metadata, object definitions, and relationships for RAG retrieval.";
        }
    }
}
