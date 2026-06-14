// ==============================================
// KnowledgeIncidentAnalysisAI Class
//
// Description:
// This class is the main package for the VSIX extension. It interacts with Visual Studio's
// environment (DTE) to manage text editor windows, load SQL shortcuts, and interact with 
// status bars and text views. The package also initializes logging and handles events 
// related to editor window activation and document type checks for SQL files.
//
// Features:
// - Initializes the VSIX package with logging and configuration.
// - Loads and manages SQL shortcuts from a configuration file.
// - Monitors active editor windows and applies filters for SQL file documents.
// - Updates Visual Studio's status bar based on actions in the editor.
//
// License Agreement:
// © [Your Company Name] [Year]. All rights reserved.
//
// This Software and any associated materials are confidential and proprietary to Bert O'Neill ("Licensor").
// By using this Software, you are agreeing to comply with the terms of this License Agreement.
//
// 1. Grant of License:
//    Licensor grants you a limited, non-exclusive, non-transferable license to use the Software for internal business operations on a single machine or as otherwise specified in the applicable agreement with Licensor.
//
// 2. Restrictions:
//    You may not:
//    - Copy, modify, distribute, or create derivative works of the Software, in whole or in part, except as expressly permitted in writing by Licensor.
//    - Reverse-engineer, decompile, or disassemble the Software, or attempt to discover its source code or underlying algorithms.
//    - Use the Software for any illegal purpose or in a manner inconsistent with the terms of this License.
//
// 3. Ownership:
//    All rights, title, and interest in the Software, including any updates or modifications, remain exclusively with Licensor. The Software is licensed, not sold.
//
// 4. Warranties and Liability:
//    The Software is provided "as-is," and Licensor makes no warranties, either express or implied, regarding its functionality, fitness for a particular purpose, or any other aspect of its use. In no event shall Licensor be liable for any direct, indirect, incidental, or consequential damages arising from the use or inability to use the Software.
//
// 5. License Termination:
//    Licensor may terminate this license at any time if you breach any of the terms of this License Agreement. Upon termination, you must immediately cease all use of the Software and return or destroy all copies of the Software.
//
// 6. Support:
//    Licensor may offer support for the Software under a separate agreement. You must have an active support contract to receive any updates or assistance.
//
// For licensing inquiries, support, or additional information, please contact:
// Bert O'Neill
// Email: BertONeill@Yahoo.Com
//
// ==============================================
extern alias ssms21;
extern alias ssms22;

using EnvDTE;
using EnvDTE80;
using KnowledgeIncidentAnalysisAI.Application;
using KnowledgeIncidentAnalysisAI.Domain.Enums;
using KnowledgeIncidentAnalysisAI.Domain.Models;
using KnowledgeIncidentAnalysisAI.Infrastructure.ObjectExplorer;
using Microsoft.SqlServer.Management.UI.VSIntegration.Editors;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json.Linq;
using SSMS_SqlAdjust_Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Task = System.Threading.Tasks.Task;

namespace KnowledgeIncidentAnalysisAI
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    //[ProvideAutoLoad("d114938f-591c-46cf-a785-500a82d97410", PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(KnowledgeIncidentAnalysisAIPackage.PackageGuidString)]
    public sealed class KnowledgeIncidentAnalysisAIPackage : AsyncPackage
    {
        /// <summary>
        /// DataChatAIPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "ad7390f9-52ec-46c9-9274-a5e174a30245";

        private OptionsDialogPage options;
        private TreeView treeView;
        private IObjectExplorerService objectExplorerService;
        private SsmsHelper _helperService;
        public static IObjectExplorerHelper _smoObjectExplorer;

        private DTE _dte;
        private IVsTextViewFilter _textViewFilter;
        private IVsStatusbar _statusBar;
        private const string SELECTION_EVENT = "Provider_SelectionChanged";
        private string _lastServer;
        private string _lastDb;

        //private ObjectExplorerMenuHook _objectExplorerMenuHook;
        //private DatabaseContextMenuHandler _databaseContextMenuHandler;

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            Debug.WriteLine("Package loaded");

            _helperService = new SsmsHelper();

            _statusBar = await GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
            if (_statusBar != null)
            {
                StatusBarService.Initialize(_statusBar);
            }

            await InitializeSsmsIntegrationAsync();
        }

        private async Task InitializeSsmsIntegrationAsync()
        {
            await Task.Delay(3000); // give SSMS time to fully load UI

            try
            {
                var ssmsVersion = _helperService.RetrieveSsmsVersion();

                InitializeSmoObjectExplorer(ssmsVersion);

                objectExplorerService = _smoObjectExplorer.GetObjectExplorer();
            }
            catch (Exception ex)
            {
                LoggerService.Error(ex.ToString());
                return;
            }

            if (objectExplorerService == null)
                return;

            var treeProperty = objectExplorerService.GetType()
                .GetProperty("Tree", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (treeProperty == null)
                return;

            treeView = treeProperty.GetValue(objectExplorerService, null) as TreeView;

            if (treeView == null)
                return;

            treeView.ContextMenuStripChanged += TreeView_ContextMenuStripChanged;

            Debug.WriteLine("SSMS integration complete");
        }

        //protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        //{
        //    await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        //    System.Diagnostics.Debug.WriteLine("KnowledgeIncidentAnalysisAI loaded");

        //    //_databaseContextMenuHandler = new DatabaseContextMenuHandler();
        //    //_objectExplorerMenuHook = new ObjectExplorerMenuHook(this, OnDatabaseContextMenuAction);
        //    //_objectExplorerMenuHook.Initialize();


        //    //load settings from options dialog
        //    (this as IVsPackage).GetAutomationObject("SQL Server Object Explorer.SSMS Object Explorer Menu", out object automationObject);
        //    if (automationObject == null)
        //    {
        //        MessageBox.Show("Automation Object not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
        //        return;
        //    }
        //    options = (OptionsDialogPage)automationObject;

        //    _helperService = new SsmsHelper();
        //    await LoadSsmsStatusBarReference(); // allow ext to update SSMS statusbar

        //    var ssmsVersion = _helperService.RetrieveSsmsVersion();
        //    Debug.WriteLine($"SSMS AI Buddy: detected host version '{ssmsVersion}'.");

        //    try
        //    {
        //        InitializeSmoObjectExplorer(ssmsVersion); // register the correct SSMS version to its Obj. Explorer
        //        objectExplorerService = _smoObjectExplorer.GetObjectExplorer(); // based on loaded SSMS version

        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"SSMS AI Buddy: Object Explorer hook failed: {ex}");
        //        LoggerService.Error($"Object Explorer hook failed: {ex}");
        //    }


        //    //find tree control in the Object Explorer window
        //    if (objectExplorerService == null)
        //    {
        //        //Error("Object Explorer Service not found");
        //        return;
        //    }
        //    var treeProperty = objectExplorerService.GetType().GetProperty("Tree", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
        //    if (treeProperty == null)
        //    {
        //        //Error("Tree property not found");
        //        return;
        //    }
        //    treeView = (TreeView)treeProperty.GetValue(objectExplorerService, null);
        //    if (treeView == null)
        //    {
        //        //Error("TreeView control not found");
        //        return;
        //    }
        //    treeView.ContextMenuStripChanged += TreeView_ContextMenuStripChanged;

        //    // await MonitorEditorWindow(); // capture events when user types into SQL editor

        //    StatusBarService.UpdateStatusBarText($"AI Buddy integrated into {ssmsVersion}"); // update SSMS statusbar with log file path
        //}

        //private void OnDatabaseContextMenuAction(DatabaseContextMenuAction action, SmoNodeInfo nodeInfo)
        //{
        //    _databaseContextMenuHandler.Handle(action, nodeInfo);
        //}

        protected override void Dispose(bool disposing)
        {
            //if (disposing)
            //{
            //    _objectExplorerMenuHook?.Dispose();
            //}

            base.Dispose(disposing);
        }

        private void InitializeSmoObjectExplorer(string ssmsVersion)
        {
            switch (ssmsVersion)
            {
                //case "SSMS 18":
                //    _smoObjectExplorer = new ssms18::SSMS_18_SqlAdjust.SmoObjectExplorer(this);
                //    break;
                //case "SSMS 19":
                //    _smoObjectExplorer = new ssms19::SSMS_19_SqlAdjust.SmoObjectExplorer(this);
                //    break;
                //case "SSMS 20":
                //    _smoObjectExplorer = new ssms20::SSMS_20_SqlAdjust.SmoObjectExplorer(this);
                //    break;
                case "SSMS 21":
                    _smoObjectExplorer = new ssms21::SSMS_21_SqlAdjust.SmoObjectExplorer(this);
                    break;
                case "SSMS 22":
                    _smoObjectExplorer = new ssms22::SSMS_22_SqlAdjust.SmoObjectExplorer(this);
                    break;
                default:
                    // default to SSMS 21 - currently most common version
                    _smoObjectExplorer = new ssms21::SSMS_21_SqlAdjust.SmoObjectExplorer(this);
                    break;
            }

            _smoObjectExplorer.HookSelectionChanged(SELECTION_EVENT);
        }

        private async Task LoadSsmsStatusBarReference()
        {
            _statusBar = await GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar; // reference SSMS status bar

            if (_statusBar != null)
            {
                StatusBarService.Initialize(_statusBar); // Initialize our status bar service
            }
        }

        private void TreeView_ContextMenuStripChanged(object sender, EventArgs e)
        {
            //sanity check objects
            if (treeView == null || options == null || objectExplorerService == null)
            {
                return;
            }

            if (treeView.SelectedNode == null || treeView.ContextMenuStrip == null)
            {
                return;
            }

            if (treeView.ContextMenuStrip.Items == null)
            {
                return;
            }

            //get the selected tree node
            objectExplorerService.GetSelectedNodes(out int arraySize, out INodeInformation[] nodes);
            if (arraySize == 0 || nodes.Length == 0)
            {
                return;
            }

            //get meta data from tree node
            NodeInfo nodeInfo = nodes[0].GetInfo();

            //build context menu
            ToolStripMenuItem myScriptsMenu = new ToolStripMenuItem(options.Text);
            if (options.ShowIcon)
            {
                myScriptsMenu.Image = Properties.Resources.plus;
            }

            if (options.MenuItems != null)
            {
                foreach (var menuItem in options.MenuItems)
                {
                    if (!menuItem.Enabled || menuItem.Name == string.Empty)
                    {
                        continue;
                    }

                    if (menuItem.Context == "All" || menuItem.Context == nodes[0].UrnPath)
                    {
                        MenuItemInstance instance = new MenuItemInstance(menuItem, nodeInfo, nodes[0].InvariantName);

                        ToolStripMenuItem s = new ToolStripMenuItem(menuItem.Name)
                        {
                            Tag = instance
                        };
                        if (menuItem.Execute)
                        {
                            s.Image = Properties.Resources.Play;
                        }
                        if (menuItem.Confirm)
                        {
                            s.Image = Properties.Resources.QuestionMark;
                        }
                        s.Click += delegate (object send, EventArgs ev) {
                            _ = Menu_ClickAsync(send, ev);
                        };
                        s.ForeColor = treeView.ForeColor;
                        s.BackColor = treeView.BackColor;
                        myScriptsMenu.DropDownItems.Add(s);
                    }
                }
            }

            if (myScriptsMenu.DropDownItems.Count > 0
                && (
                    options.ShowNewButton
                    || options.ShowOptionsButton
                    || options.ShowImportButton
                    || options.ShowExportButton
                ))
            {
                myScriptsMenu.DropDownItems.Add(new ToolStripSeparator());
            }


            if (options.ShowNewButton)
            {
                ToolStripMenuItem add = new ToolStripMenuItem("New");
                add.Click += Add_Click;
                add.Tag = nodeInfo;
                add.ForeColor = treeView.ForeColor;
                add.BackColor = treeView.BackColor;
                myScriptsMenu.DropDownItems.Add(add);
            }

            if (options.ShowImportButton)
            {
                ToolStripMenuItem imp = new ToolStripMenuItem("Import");
                imp.Click += Import_Click;
                imp.Tag = nodeInfo;
                imp.ForeColor = treeView.ForeColor;
                imp.BackColor = treeView.BackColor;
                myScriptsMenu.DropDownItems.Add(imp);
            }

            if (options.ShowExportButton)
            {
                ToolStripMenuItem exp = new ToolStripMenuItem("Export");
                exp.Click += Export_Click;
                exp.Tag = nodeInfo;
                exp.ForeColor = treeView.ForeColor;
                exp.BackColor = treeView.BackColor;
                myScriptsMenu.DropDownItems.Add(exp);
            }

            if (options.ShowOptionsButton)
            {
                ToolStripMenuItem opt = new ToolStripMenuItem("Options");
                opt.Click += Options_Click;
                opt.Tag = nodeInfo;
                opt.ForeColor = treeView.ForeColor;
                opt.BackColor = treeView.BackColor;
                myScriptsMenu.DropDownItems.Add(opt);
            }


            if (myScriptsMenu.DropDownItems.Count == 0)
            {
                return;
            }

            treeView.ContextMenuStrip.Items.Add(myScriptsMenu);
            treeView.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        }

        private void Add_Click(object sender, EventArgs e)
        {
            NodeInfo nodeInfo = (NodeInfo)(sender as ToolStripMenuItem).Tag;

            //AddMenuItem add = new AddMenuItem(nodeInfo);

            //DialogResult d = add.ShowDialog();

            //if (d == DialogResult.OK)
            //{
            //    options.MenuItems.Add(add.GetMenuItem());
            //    options.SaveSettingsToStorage();
            //}
        }

        private void Options_Click(object sender, EventArgs e)
        {
            ShowOptionPage(typeof(OptionsDialogPage));
        }

        private void Export_Click(object sender, EventArgs e)
        {
            SaveFileDialog save = new SaveFileDialog
            {
                AddExtension = true,
                Filter = "Xml (*.xml)|*.xml",
                DefaultExt = "xml",
                Title = "Export menu items as XML",
                ValidateNames = true
            };

            if (save.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(save.FileName, options.MenuItemsXml);
                }
                catch (Exception ex)
                {
                    Error($"Error saving to {save.FileName}: {ex.Message}");
                }
            }
        }

        private void Import_Click(object sender, EventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                DefaultExt = "xml",
                Filter = "Xml (*.xml)|*.xml",
                Multiselect = false,
                Title = "Import menu items from XML"
            };

            if (open.ShowDialog() == DialogResult.OK)
            {
                int count = 0;

                try
                {
                    List<Domain.Models.MenuItem> menuItems = File.ReadAllText(open.FileName).DeserializeObject<List<Domain.Models.MenuItem>>();

                    foreach (Domain.Models.MenuItem item in menuItems)
                    {
                        if (options.MenuItems.FindIndex(x => (x.Name == item.Name && x.Context == item.Context)) == -1)
                        {
                            options.MenuItems.Add(item);
                            count += 1;
                        }
                    }

                    options.SaveSettingsToStorage();

                    Show($"{count} menu item{((count == 1) ? "" : "s")} imported", MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Error($"Error importing menu items from {open.FileName}: {ex.Message}");
                }
            }
        }

        private async Task Menu_ClickAsync(object sender, EventArgs e)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);

            if (treeView == null || sender == null)
            {
                return;
            }

            if (treeView.SelectedNode == null)
            {
                return;
            }

            ToolStripMenuItem tool = (sender as ToolStripMenuItem);

            if (tool.Tag == null)
            {
                return;
            }

            MenuItemInstance itemInstance = (MenuItemInstance)tool.Tag;

            string script;

            if (File.Exists(itemInstance.MenuItem.Script))
            {
                try
                {
                    script = File.ReadAllText(itemInstance.MenuItem.Script);
                    script = $"-- File:\t\t{itemInstance.MenuItem.Script}{Environment.NewLine}" + script;
                }
                catch (Exception ex)
                {
                    Error($"Error reading {itemInstance.MenuItem.Script}: {ex.Message}");
                    return;
                }
            }
            else
            {
                script = itemInstance.MenuItem.Script;
            }


            script = script
                    .Replace("{OBJECT}", itemInstance.Name)
                    .Replace("{SERVER}", itemInstance.NodeInfo.Server)
                    .Replace("{DATABASE}", itemInstance.NodeInfo.Database)
                    .Replace("{TABLE}", itemInstance.NodeInfo.Table)
                    .Replace("{VIEW}", itemInstance.NodeInfo.View)
                    .Replace("{STORED_PROCEDURE}", itemInstance.NodeInfo.StoredProcedure)
                    .Replace("{FUNCTION}", itemInstance.NodeInfo.Function)
                    .Replace("{SCHEMA}", itemInstance.NodeInfo.Schema)
                    .Replace("{JOB}", itemInstance.NodeInfo.Job)
                    .Replace("{YYYY-MM-DD}", DateTime.Now.ToString("yyyy-MM-dd"))
                    .Replace("{HH:mm:ss}", DateTime.Now.ToString("HH:mm:ss"))
                    .Replace("{YYYY-MM-DD HH:mm:ss}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));




            DTE2 dte = (DTE2)await this.GetServiceAsync(typeof(DTE));
            if (dte == null)
            {
                return;
            }

            //IScriptFactory scriptFactory = ServiceCache.ScriptFactory;
            //if (scriptFactory == null)
            //{
            //    return;
            //}

            //var s = scriptFactory.CreateNewBlankScript(ScriptType.Sql);


            //if (dte.ActiveDocument != null)
            //{
            //    TextSelection ts = (TextSelection)dte.ActiveDocument.Selection;
            //    ts.Insert(script, (int)vsInsertFlags.vsInsertFlagsInsertAtStart);

            //    dte.ActiveDocument.Activate();

            //    if (itemInstance.MenuItem.Execute || itemInstance.MenuItem.Confirm)
            //    {
            //        if (itemInstance.MenuItem.Confirm)
            //        {
            //            if (MessageBox.Show($"Execute \"{itemInstance.MenuItem.Name}\"?{Environment.NewLine}{Environment.NewLine}{itemInstance.NodeInfo}", "SSMS Object Explorer Menu", MessageBoxButtons.OKCancel) == DialogResult.OK)
            //            {
            //                dte.ActiveDocument.DTE.ExecuteCommand("Query.Execute");
            //            }
            //            else
            //            {
            //                ts.Cancel();
            //            }
            //        }
            //        else
            //        {
            //            dte.ActiveDocument.DTE.ExecuteCommand("Query.Execute");
            //        }
            //    }
            //    else
            //    {
            //        ts.Cancel();
            //    }
            //}
        }

        private void Error(string message)
        {
            Show(message, MessageBoxIcon.Error);
        }

        private void Show(string message, MessageBoxIcon icon, MessageBoxButtons buttons = MessageBoxButtons.OK, Action action = null)
        {
            if (MessageBox.Show(message, "SSMS Object Explorer Menu", buttons, icon) == DialogResult.OK)
            {
                action?.Invoke();
            }
        }

        #endregion
    }
}
