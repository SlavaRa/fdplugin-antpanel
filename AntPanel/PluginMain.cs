using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using PluginCore;
using PluginCore.Helpers;
using PluginCore.Managers;
using PluginCore.Utilities;
using ProjectManager;
using ProjectManager.Controls.TreeView;
using WeifenLuo.WinFormsUI.Docking;

namespace AntPanel
{
    public class PluginMain : IPlugin
    {
        public readonly List<string> BuildFilesList = new List<string>();
        public string StorageFileName => "antPanelData.txt";
        string settingFilename;
        Image pluginImage;
        PluginUI pluginUI;
        DockContent pluginPanel;
        TreeView projectTree;

        #region Required Properties

        /// <summary>
        /// Api level of the plugin
        /// </summary>
        public int Api => 1;

        /// <summary>
        /// Name of the plugin
        /// </summary> 
        public string Name => "AntPanel";

        /// <summary>
        /// GUID of the plugin
        /// </summary>
        public string Guid => "92d9a647-6cd3-4347-9db6-95f324292399";

        /// <summary>
        /// Author of the plugin
        /// </summary> 
        public string Author => "Canab, SlavaRa";

        /// <summary>
        /// Description of the plugin
        /// </summary> 
        public string Description => "AntPanel Plugin For FlashDevelop";

        /// <summary>
        /// Web address for help
        /// </summary> 
        public string Help => "http://www.flashdevelop.org/community/";

        /// <summary>
        /// Object that contains the settings
        /// </summary>
        [Browsable(false)]
        public object Settings { get; private set; }
        
        #endregion
        
        #region Required Methods
        
        /// <summary>
        /// Initializes the plugin
        /// </summary>
        public void Initialize()
        {
            InitBasics();
            LoadSettings();
            AddEventHandlers();
            CreateMenuItem();
            CreatePluginPanel();
        }

        /// <summary>
        /// Disposes the plugin
        /// </summary>
        public void Dispose() => SaveSettings();

        /// <summary>
        /// Adds the required event handlers
        /// </summary>
        public void AddEventHandlers() => EventManager.AddEventHandler(this, EventType.UIStarted | EventType.Command);

        /// <summary>
        /// Handles the incoming events
        /// </summary>
        public void HandleEvent(object sender, NotifyEvent e, HandlingPriority priority)
        {
            switch (e.Type)
            {
                case EventType.UIStarted:
                    DirectoryNode.OnDirectoryNodeRefresh += OnDirectoryNodeRefresh;
                    break;
                case EventType.Command:
                    var da = (DataEvent)e;
                    switch (da.Action)
                    {
                        case ProjectManagerEvents.Project:
                            if (PluginBase.CurrentProject != null) ReadBuildFiles();
                            pluginUI.RefreshData();
                            break;
                        case ProjectManagerEvents.TreeSelectionChanged:
                            OnTreeSelectionChanged();
                            break;
                    }
                    break;
            }
        }

        #endregion

        #region Custom Public Methods

        public void AddBuildFiles(IEnumerable<string> files)
        {
            foreach (var file in files.Where(file => !BuildFilesList.Contains(file)))
                BuildFilesList.Add(file);
            SaveBuildFiles();
            pluginUI.RefreshData();
        }

        public void RemoveBuildFile(string file)
        {
            if (BuildFilesList.Contains(file)) BuildFilesList.Remove(file);
            SaveBuildFiles();
            pluginUI.RefreshData();
        }

        public void RunTarget(string file, string target)
        {
            var antPath = ((Settings)Settings).AntPath;
            var addArgs = ((Settings)Settings).AdditionalArgs;
            var command = /*PluginBase.MainForm.CommandPromptExecutable ??*/ Path.Combine(Environment.SystemDirectory, "cmd.exe");
            var arguments = "/c \"";
            if (string.IsNullOrEmpty(antPath)) arguments += "ant";
            else arguments += "\"" + Path.Combine(Path.Combine(antPath, "bin"), "ant") + "\"";
            if (!string.IsNullOrEmpty(addArgs)) arguments += " " + addArgs;
            arguments += $" -buildfile \"{file}\" \"{target}\"\"";
            PluginBase.MainForm.CallCommand("RunProcessCaptured", $"{command};{arguments}");
        }

        public void ReadBuildFiles()
        {
            BuildFilesList.Clear();
            var folder = GetBuildFilesStorageFolder();
            var fullName = Path.Combine(folder, StorageFileName);
            if (!File.Exists(fullName)) return;
            var file = new StreamReader(fullName);
            string line;
            while ((line = file.ReadLine()) != null)
                if (!string.IsNullOrEmpty(line) && !BuildFilesList.Contains(line))
                    BuildFilesList.Add(line);
            file.Close();
        }

        #endregion

        #region Custom Private Methods

        /// <summary>
        /// Initializes important variables
        /// </summary>
        void InitBasics()
        {
            pluginImage = PluginBase.MainForm.FindImage("486");
            var path = Path.Combine(PathHelper.DataDir, "AntPanel");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            settingFilename = Path.Combine(path, "Settings.fdb");
        }

        /// <summary>
        /// Creates a menu item for the plugin
        /// </summary>
        void CreateMenuItem()
        {
            var menuItem = new ToolStripMenuItem("Ant Panel", pluginImage, OpenPanel);
            PluginBase.MainForm.RegisterShortcutItem("ViewMenu.ShowAntPanel", menuItem);
            var menu = (ToolStripMenuItem)PluginBase.MainForm.FindMenuItem("ViewMenu");
            menu.DropDownItems.Add(menuItem);
        }

        /// <summary>
        /// Creates a plugin panel for the plugin
        /// </summary>
        void CreatePluginPanel()
        {
            pluginUI = new PluginUI(this) {Text = "Ant"};
            pluginUI.OnChange += OnPluginUIChange;
            pluginPanel = PluginBase.MainForm.CreateDockablePanel(pluginUI, Guid, pluginImage, DockState.DockRight);
        }

        /// <summary>
        /// Loads the plugin settings
        /// </summary>
        void LoadSettings()
        {
            Settings = new Settings();
            if (!File.Exists(settingFilename)) SaveSettings();
            else Settings = (Settings)ObjectSerializer.Deserialize(settingFilename, Settings);
        }

        /// <summary>
        /// Saves the plugin settings
        /// </summary>
        void SaveSettings() => ObjectSerializer.Serialize(settingFilename, Settings);

        /// <summary>
        /// Opens the plugin panel if closed
        /// </summary>
        void OpenPanel(object sender, EventArgs e) => pluginPanel.Show();

        void SaveBuildFiles()
        {
            var folder = GetBuildFilesStorageFolder();
            var fullName = Path.Combine(folder, StorageFileName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            var file = new StreamWriter(fullName);
            foreach (var line in BuildFilesList)
                file.WriteLine(line);
            file.Close();
        }

        static string GetBuildFilesStorageFolder() => Path.Combine(Path.GetDirectoryName(PluginBase.CurrentProject.ProjectPath), "obj");

        #endregion

        #region Event Handlers

        void OnDirectoryNodeRefresh(DirectoryNode node) => projectTree = node.TreeView;

        void OnTreeSelectionChanged()
        {
            if (!(projectTree?.SelectedNode is FileNode)) return;
            var path = Path.GetFullPath(((FileNode)projectTree.SelectedNode).BackingPath);
            if (BuildFilesList.Contains(path) || Path.GetExtension(path) != ".xml") return;
            projectTree.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            projectTree.ContextMenuStrip.Items.Add("Add as Ant Build File", pluginImage, OnAddAsAntBuildFile);
        }

        void OnAddAsAntBuildFile(object sender, EventArgs e)
        {
            var path = Path.GetFullPath(((FileNode)projectTree.SelectedNode).BackingPath);
            if (BuildFilesList.Contains(path)) return;
            BuildFilesList.Add(path);
            SaveBuildFiles();
            pluginUI.RefreshData();
        }

        void OnPluginUIChange(object sender, PluginUIArgs e)
        {
            BuildFilesList.Clear();
            BuildFilesList.AddRange(e.Paths);
            SaveBuildFiles();
        }

        #endregion
    }
}