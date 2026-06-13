using System;

namespace DatabaseAnalysisService
{
    public sealed class DatabaseObjectInfo
    {
        public string ObjectType { get; set; }
        public string Schema { get; set; }
        public string Name { get; set; }

        public string DisplayName => string.IsNullOrEmpty(Schema) ? Name : $"{Schema}.{Name}";
    }

    public sealed class DataDictionaryService
    {
        public string GenerateExplanation(string server, string database, DatabaseObjectInfo objectReference)
        {
            if (objectReference == null)
            {
                throw new ArgumentNullException(nameof(objectReference));
            }

            return $"Generating English data dictionary explanation for {objectReference.DisplayName} ({objectReference.ObjectType}) " +
                   $"in [{server}].[{database}].\n\n" +
                   "The explanation will cover:\n" +
                   "- Table columns, keys, and constraints (for tables)\n" +
                   "- Relationships to other tables and objects\n" +
                   "- Purpose and behavior (for stored procedures, functions, and views)";
        }
    }
}
