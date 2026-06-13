using KnowledgeIncidentAnalysisAI.Domain.Enums;
using KnowledgeIncidentAnalysisAI.Domain.Models;
using KnowledgeIncidentAnalysisAI.Infrastructure.ObjectExplorer;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using Microsoft.VisualStudio.Shell;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace KnowledgeIncidentAnalysisAI.Infrastructure.ObjectExplorer
{
    internal sealed class ObjectExplorerMenuHook : IDisposable
    {
        private const string MenuRootText = "SQL Insight Buddy";

        private readonly AsyncPackage _package;
        private readonly Action<DatabaseContextMenuAction, SmoNodeInfo> _menuActionHandler;
        private IObjectExplorerService _objectExplorerService;
        private TreeView _treeView;

        public ObjectExplorerMenuHook(
            AsyncPackage package,
            Action<DatabaseContextMenuAction, SmoNodeInfo> menuActionHandler)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            _menuActionHandler = menuActionHandler ?? throw new ArgumentNullException(nameof(menuActionHandler));
        }

        public void Initialize()
        {
            _objectExplorerService = ((IServiceProvider)_package).GetService(typeof(IObjectExplorerService)) as IObjectExplorerService;
            if (_objectExplorerService == null)
            {
                return;
            }

            PropertyInfo treeProperty = _objectExplorerService.GetType().GetProperty(
                "Tree",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (treeProperty == null)
            {
                return;
            }

            _treeView = treeProperty.GetValue(_objectExplorerService, null) as TreeView;
            if (_treeView == null)
            {
                return;
            }

            _treeView.ContextMenuStripChanged += OnContextMenuStripChanged;
        }

        private void OnContextMenuStripChanged(object sender, EventArgs e)
        {
            if (_treeView == null || _objectExplorerService == null || _treeView.ContextMenuStrip == null)
            {
                return;
            }

            if (_treeView.SelectedNode == null)
            {
                return;
            }

            _objectExplorerService.GetSelectedNodes(out int arraySize, out INodeInformation[] nodes);
            if (arraySize == 0 || nodes == null || nodes.Length == 0)
            {
                return;
            }

            if (nodes[0].UrnPath != SmoNodeInfoParser.DatabaseUrnPath)
            {
                return;
            }

            SmoNodeInfo nodeInfo = SmoNodeInfoParser.Parse(nodes[0]);
            if (!nodeInfo.HasDatabaseContext)
            {
                return;
            }

            ToolStripMenuItem rootMenu = new ToolStripMenuItem(MenuRootText);
            rootMenu.DropDownItems.Add(CreateMenuItem(
                "Build AI Index",
                "Build a searchable AI index for this database.",
                DatabaseContextMenuAction.BuildAiIndex,
                nodeInfo));
            rootMenu.DropDownItems.Add(CreateMenuItem(
                "Data Dictionary Generator",
                "Describe table schema, relationships, and explain a selected object in English.",
                DatabaseContextMenuAction.DataDictionaryGenerator,
                nodeInfo));
            rootMenu.DropDownItems.Add(new ToolStripSeparator());
            rootMenu.DropDownItems.Add(CreateMenuItem(
                "Record - Root Cause Analysis",
                "Explain in English what happened for a selected record.",
                DatabaseContextMenuAction.RecordRootCauseAnalysis,
                nodeInfo));
            rootMenu.DropDownItems.Add(CreateMenuItem(
                "Record - Knowledge GraphDB",
                "Show objects updated from a parent record and how they relate.",
                DatabaseContextMenuAction.RecordKnowledgeGraph,
                nodeInfo));
            rootMenu.DropDownItems.Add(CreateMenuItem(
                "Record - Timeline",
                "Show which entities changed and when for a selected record.",
                DatabaseContextMenuAction.RecordTimeline,
                nodeInfo));

            ApplyTreeColors(rootMenu);
            _treeView.ContextMenuStrip.Items.Add(rootMenu);
            _treeView.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        }

        private ToolStripMenuItem CreateMenuItem(
            string text,
            string toolTip,
            DatabaseContextMenuAction action,
            SmoNodeInfo nodeInfo)
        {
            var menuItem = new ToolStripMenuItem(text)
            {
                Tag = new MenuActionContext(action, nodeInfo),
                ToolTipText = toolTip
            };

            menuItem.Click += OnMenuItemClick;
            ApplyTreeColors(menuItem);
            return menuItem;
        }

        private void ApplyTreeColors(ToolStripMenuItem menuItem)
        {
            menuItem.ForeColor = _treeView.ForeColor;
            menuItem.BackColor = _treeView.BackColor;

            foreach (ToolStripItem child in menuItem.DropDownItems)
            {
                child.ForeColor = _treeView.ForeColor;
                child.BackColor = _treeView.BackColor;
            }
        }

        private void OnMenuItemClick(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            var context = menuItem?.Tag as MenuActionContext;
            if (context == null)
            {
                return;
            }

            _menuActionHandler(context.Action, context.NodeInfo);
        }

        public void Dispose()
        {
            if (_treeView != null)
            {
                _treeView.ContextMenuStripChanged -= OnContextMenuStripChanged;
            }
        }

        private sealed class MenuActionContext
        {
            public MenuActionContext(DatabaseContextMenuAction action, SmoNodeInfo nodeInfo)
            {
                Action = action;
                NodeInfo = nodeInfo;
            }

            public DatabaseContextMenuAction Action { get; }
            public SmoNodeInfo NodeInfo { get; }
        }
    }
}
