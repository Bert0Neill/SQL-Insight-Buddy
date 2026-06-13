using System;

namespace IncidentInsightsService
{
    public sealed class IncidentRecordInfo
    {
        public string TableSchema { get; set; }
        public string TableName { get; set; }
        public string RecordKey { get; set; }
        public string RecordKeyValue { get; set; }

        public string DisplayName => $"{TableSchema}.{TableName} [{RecordKey}={RecordKeyValue}]";
    }

    public sealed class RootCauseAnalysisService
    {
        public string AnalyzeRecord(string server, string database, IncidentRecordInfo record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            return $"Root cause analysis for record {record.DisplayName} in [{server}].[{database}].\n\n" +
                   "This will explain in English what happened, including contributing events, " +
                   "data changes, and likely causes tied to the selected parent record.";
        }
    }
}
