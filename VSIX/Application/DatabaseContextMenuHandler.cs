using DatabaseAnalysisService;
using EntityGraphDBService;
using EntityTimelineService;
using IncidentInsightsService;
using KnowledgeIncidentAnalysisAI.Domain.Enums;
using KnowledgeIncidentAnalysisAI.Domain.Models;
using KnowledgeIncidentAnalysisAI.UI;
using RagService;
using System;
using System.Windows.Forms;

namespace KnowledgeIncidentAnalysisAI.Application
{
    internal sealed class DatabaseContextMenuHandler
    {
        private readonly RagIndexService _ragIndexService = new RagIndexService();
        private readonly DataDictionaryService _dataDictionaryService = new DataDictionaryService();
        private readonly RootCauseAnalysisService _rootCauseAnalysisService = new RootCauseAnalysisService();
        private readonly KnowledgeGraphService _knowledgeGraphService = new KnowledgeGraphService();
        private readonly RecordTimelineService _recordTimelineService = new RecordTimelineService();

        public void Handle(DatabaseContextMenuAction action, SmoNodeInfo nodeInfo)
        {
            if (!nodeInfo.HasDatabaseContext)
            {
                MessageBox.Show(
                    "Could not determine server and database from the selected node.",
                    "SQL Insight Buddy",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            switch (action)
            {
                case DatabaseContextMenuAction.BuildAiIndex:
                    HandleBuildAiIndex(nodeInfo);
                    break;
                case DatabaseContextMenuAction.DataDictionaryGenerator:
                    HandleDataDictionary(nodeInfo);
                    break;
                case DatabaseContextMenuAction.RecordRootCauseAnalysis:
                    HandleRecordAction(nodeInfo, "Root Cause Analysis", record =>
                        _rootCauseAnalysisService.AnalyzeRecord(nodeInfo.Server, nodeInfo.Database, ToIncidentRecord(record)));
                    break;
                case DatabaseContextMenuAction.RecordKnowledgeGraph:
                    HandleRecordAction(nodeInfo, "Knowledge Graph", record =>
                        _knowledgeGraphService.GetRelatedObjects(nodeInfo.Server, nodeInfo.Database, ToGraphRecord(record)));
                    break;
                case DatabaseContextMenuAction.RecordTimeline:
                    HandleRecordAction(nodeInfo, "Record Timeline", record =>
                        _recordTimelineService.GetTimeline(nodeInfo.Server, nodeInfo.Database, ToTimelineRecord(record)));
                    break;
            }
        }

        private void HandleBuildAiIndex(SmoNodeInfo nodeInfo)
        {
            string result = _ragIndexService.BuildAiIndex(nodeInfo.Server, nodeInfo.Database);
            AnalysisResultForm.ShowResult(
                "Build AI Index",
                $"Server: {nodeInfo.Server}\nDatabase: {nodeInfo.Database}\n\n{result}");
        }

        private void HandleDataDictionary(SmoNodeInfo nodeInfo)
        {
            using (var picker = new DataDictionaryObjectPickerForm())
            {
                if (picker.ShowDialog() != DialogResult.OK || picker.SelectedObject == null)
                {
                    return;
                }

                DatabaseObjectReference selectedObject = picker.SelectedObject;
                var serviceObject = new DatabaseObjectInfo
                {
                    ObjectType = selectedObject.ObjectType,
                    Schema = selectedObject.Schema,
                    Name = selectedObject.Name
                };

                string result = _dataDictionaryService.GenerateExplanation(
                    nodeInfo.Server,
                    nodeInfo.Database,
                    serviceObject);

                AnalysisResultForm.ShowResult(
                    "Data Dictionary Generator",
                    $"Server: {nodeInfo.Server}\nDatabase: {nodeInfo.Database}\nObject: {selectedObject.DisplayName}\n\n{result}");
            }
        }

        private void HandleRecordAction(
            SmoNodeInfo nodeInfo,
            string title,
            Func<RecordReference, string> action)
        {
            using (var picker = new RecordPickerForm())
            {
                if (picker.ShowDialog() != DialogResult.OK || picker.SelectedRecord == null)
                {
                    return;
                }

                RecordReference record = picker.SelectedRecord;
                string result = action(record);

                AnalysisResultForm.ShowResult(
                    title,
                    $"Server: {nodeInfo.Server}\nDatabase: {nodeInfo.Database}\nRecord: {record.DisplayName}\n\n{result}");
            }
        }

        private static IncidentRecordInfo ToIncidentRecord(RecordReference record)
        {
            return new IncidentRecordInfo
            {
                TableSchema = record.TableSchema,
                TableName = record.TableName,
                RecordKey = record.RecordKey,
                RecordKeyValue = record.RecordKeyValue
            };
        }

        private static GraphRecordInfo ToGraphRecord(RecordReference record)
        {
            return new GraphRecordInfo
            {
                TableSchema = record.TableSchema,
                TableName = record.TableName,
                RecordKey = record.RecordKey,
                RecordKeyValue = record.RecordKeyValue
            };
        }

        private static TimelineRecordInfo ToTimelineRecord(RecordReference record)
        {
            return new TimelineRecordInfo
            {
                TableSchema = record.TableSchema,
                TableName = record.TableName,
                RecordKey = record.RecordKey,
                RecordKeyValue = record.RecordKeyValue
            };
        }
    }
}
