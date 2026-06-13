using System;

namespace EntityTimelineService
{
    public sealed class TimelineRecordInfo
    {
        public string TableSchema { get; set; }
        public string TableName { get; set; }
        public string RecordKey { get; set; }
        public string RecordKeyValue { get; set; }

        public string DisplayName => $"{TableSchema}.{TableName} [{RecordKey}={RecordKeyValue}]";
    }

    public sealed class RecordTimelineService
    {
        public string GetTimeline(string server, string database, TimelineRecordInfo record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            return $"Timeline for record {record.DisplayName} in [{server}].[{database}].\n\n" +
                   "This will show which entities changed at what time, ordered chronologically, " +
                   "scoped to updates related to the selected parent record.";
        }
    }
}
