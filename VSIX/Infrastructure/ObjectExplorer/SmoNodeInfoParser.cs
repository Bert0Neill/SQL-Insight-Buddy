using KnowledgeIncidentAnalysisAI.Domain.Models;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System;

namespace KnowledgeIncidentAnalysisAI.Infrastructure.ObjectExplorer
{
    internal static class SmoNodeInfoParser
    {
        public const string DatabaseUrnPath = "Server/Database";

        public static SmoNodeInfo Parse(INodeInformation node)
        {
            if (node == null)
            {
                return new SmoNodeInfo();
            }

            var info = new SmoNodeInfo
            {
                UrnPath = node.UrnPath,
                NavigationContext = node.NavigationContext,
                InvariantName = node.InvariantName
            };

            if (string.IsNullOrEmpty(node.NavigationContext))
            {
                return info;
            }

            string[] context = node.NavigationContext.Split(
                new[] { '/' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (string segment in context)
            {
                if (segment.StartsWith("Server[@Name='", StringComparison.Ordinal))
                {
                    info.Server = Unquote(segment, "Server[@Name='", "']");
                    continue;
                }

                if (segment.StartsWith("Database[@Name='", StringComparison.Ordinal))
                {
                    info.Database = Unquote(segment, "Database[@Name='", "']");
                    continue;
                }

                if (segment.StartsWith("Table[@Name='", StringComparison.Ordinal))
                {
                    string name;
                    string schema;
                    ParseSchemaName(segment, "Table[@Name='", out name, out schema);
                    info.Table = name;
                    info.Schema = schema;
                    continue;
                }

                if (segment.StartsWith("View[@Name='", StringComparison.Ordinal))
                {
                    string name;
                    string schema;
                    ParseSchemaName(segment, "View[@Name='", out name, out schema);
                    info.View = name;
                    info.Schema = schema;
                    continue;
                }

                if (segment.StartsWith("StoredProcedure[@Name='", StringComparison.Ordinal))
                {
                    string name;
                    string schema;
                    ParseSchemaName(segment, "StoredProcedure[@Name='", out name, out schema);
                    info.StoredProcedure = name;
                    info.Schema = schema;
                    continue;
                }

                if (segment.StartsWith("UserDefinedFunction[@Name='", StringComparison.Ordinal))
                {
                    string name;
                    string schema;
                    ParseSchemaName(segment, "UserDefinedFunction[@Name='", out name, out schema);
                    info.Function = name;
                    info.Schema = schema;
                    continue;
                }

                if (segment.StartsWith("Job[@Name='", StringComparison.Ordinal))
                {
                    info.Job = Unquote(segment, "Job[@Name='", "']");
                }
            }

            return info;
        }

        private static void ParseSchemaName(string segment, string prefix, out string name, out string schema)
        {
            name = null;
            schema = null;

            string[] parts = segment
                .Replace(prefix, string.Empty)
                .Replace("' and @Schema='", "|")
                .Replace("']", string.Empty)
                .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                name = parts[0];
                schema = parts[1];
            }
        }

        private static string Unquote(string segment, string prefix, string suffix)
        {
            if (!segment.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }

            string value = segment.Substring(prefix.Length);
            if (value.EndsWith(suffix, StringComparison.Ordinal))
            {
                value = value.Substring(0, value.Length - suffix.Length);
            }

            return value;
        }
    }
}
