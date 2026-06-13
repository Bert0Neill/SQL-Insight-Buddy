using System;

namespace EntityGraphDBService
{
    public sealed class GraphRecordInfo
    {
        public string TableSchema { get; set; }
        public string TableName { get; set; }
        public string RecordKey { get; set; }
        public string RecordKeyValue { get; set; }

        public string DisplayName => $"{TableSchema}.{TableName} [{RecordKey}={RecordKeyValue}]";
    }

    public sealed class KnowledgeGraphService
    {
        public string GetRelatedObjects(string server, string database, GraphRecordInfo record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            return $"Knowledge graph for record {record.DisplayName} in [{server}].[{database}].\n\n" +
                   "This will list all objects updated from the parent record and show how each object " +
                   "relates to other tables, views, procedures, and functions in the database.";
        }
    }
}
