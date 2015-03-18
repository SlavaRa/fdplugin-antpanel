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
    /// <summary>
    /// </summary>
    public class PluginMain : IPlugin
	{
        const string PLUGIN_NAME = "AntPanel";
        const string PLUGIN_GUID = "92d9a647-6cd3-4347-9db6-95f324292399";
        const string PLUGIN_HELP = "http://www.flashdevelop.org/community/";
        const string PLUGIN_AUTH = "Canab, SlavaRa";
	    const string SETTINGS_FILE = "Settings.fdb";
        const string PLUGIN_DESC = "AntPanel Plugin For FlashDevelop";
        const string STORAGE_FILE_NAME = "antPanelData.txt";

        /// <summary>
        /// </summary>
        public List<string> BuildFilesList { get; private set; }

	    Image pluginImage;
        string settingFilename;
        Settings settings;
	    PluginUI pluginUI;
        readonly Dictionary<DockState, DockState> panelDockStateToNewState = new Dictionary<DockState, DockState>
        {
            { DockState.DockBottom, DockState.DockBottomAutoHide },
            { DockState.DockLeft, DockState.DockLeftAutoHide },
            { DockState.DockRight, DockState.DockRightAutoHide },
            { DockState.DockTop, DockState.DockTopAutoHide }
        };
        DockContent pluginPanel;
        TreeView projectTree;

	    #region Required Properties

        /// <summary>
        /// Api level of the plugin
        /// </summary>
        public int Api { get { return 1; }}
        
        /// <summary>
        /// Name of the plugin
        /// </summary> 
        public string Name { get { return PLUGIN_NAME; }}

        /// <summary>
        /// GUID of the plugin
        /// </summary>
        public string Guid { get { return PLUGIN_GUID; }}

        /// <summary>
        /// Author of the plugin
        /// </summary> 
        public string Author { get { return PLUGIN_AUTH; }}

        /// <summary>
        /// Description of the plugin
        /// </summary> 
        public string Description { get { return PLUGIN_DESC; }}

        /// <summary>
        /// Web address for help
        /// </summary> 
        public string Help { get { return PLUGIN_HELP; }}

        /// <summary>
        /// Object that contains the settings
        /// </summary>
        [Browsable(false)]
        public object Settings { get { return settings; }}
		
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
		public void Dispose()
		{
            SaveSettings();
		}
		
		/// <summary>
		/// Handles the incoming events
		/// </summary>
        public void AddEventHandlers()
        {
            EventManager.AddEventHandler(this, EventType.UIStarted | EventType.Command | EventType.Keys);
        }

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
                    DataEvent da = (DataEvent)e;
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
                case EventType.Keys:
                    KeyEvent ke = (KeyEvent)e;
                    if (ke.Value == PluginBase.MainForm.GetShortcutItemKeys("ViewMenu.ShowAntPanel") && !pluginPanel.IsHidden && pluginPanel.IsActivated)
                    {
                        DockState dockState;
                        if (panelDockStateToNewState.TryGetValue(pluginPanel.DockState, out dockState))
                            pluginPanel.DockState = dockState;
                        pluginPanel.DockHandler.GiveUpFocus();
                        e.Handled = true;    
                    }
                    break;
            }
		}

        #endregion

        #region Custom Public Methods

	    /// <summary>
	    /// </summary>
	    /// <param name="files"></param>
	    public void AddBuildFiles(IEnumerable<string> files)
        {
            foreach (string file in files.Where(file => !BuildFilesList.Contains(file)))
            {
                BuildFilesList.Add(file);
            }
            SaveBuildFiles();
            pluginUI.RefreshData();
        }

	    /// <summary>
	    /// </summary>
	    /// <param name="file"></param>
	    public void RemoveBuildFile(string file)
        {
            if (BuildFilesList.Contains(file)) BuildFilesList.Remove(file);
            SaveBuildFiles();
            pluginUI.RefreshData();
        }

	    /// <summary>
	    /// </summary>
	    /// <param name="file"></param>
	    /// <param name="target"></param>
	    public void RunTarget(string file, string target)
        {
            string command = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            string arguments = "/c ";
            if (string.IsNullOrEmpty(settings.AntPath)) arguments += "ant";
            else arguments += Path.Combine(Path.Combine(settings.AntPath, "bin"), "ant");
            arguments += " -buildfile \"" + file + "\" \"" + target + "\"";
            PluginBase.MainForm.CallCommand("RunProcessCaptured", command + ";" + arguments);
        }

        /// <summary>
        /// </summary>
        /// <exception cref="OutOfMemoryException">There is insufficient memory to allocate a buffer for the returned string. </exception>
        public void ReadBuildFiles()
        {
            BuildFilesList.Clear();
            string folder = GetBuildFilesStorageFolder();
            string fullName = Path.Combine(folder, STORAGE_FILE_NAME);
            if (!File.Exists(fullName)) return;
            StreamReader file = new StreamReader(fullName);
            string line;
            while ((line = file.ReadLine()) != null)
                if (!string.IsNullOrEmpty(line) && !BuildFilesList.Contains(line)) BuildFilesList.Add(line);
            file.Close();
        }

        #endregion

        #region Custom Private Methods

        /// <summary>
        /// Initializes important variables
        /// </summary>
        void InitBasics()
        {
            BuildFilesList = new List<string>();
            pluginImage = PluginBase.MainForm.FindImage("486");
            string dataPath = Path.Combine(PathHelper.DataDir, PLUGIN_NAME);
            if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
            settingFilename = Path.Combine(dataPath, SETTINGS_FILE);
        }

        /// <summary>
        /// Creates a menu item for the plugin
        /// </summary>
        void CreateMenuItem()
        {
            ToolStripMenuItem menuItem = new ToolStripMenuItem("Ant Panel", pluginImage, OpenPanel);
            PluginBase.MainForm.RegisterShortcutItem("ViewMenu.ShowAntPanel", menuItem);
            ToolStripMenuItem menu = (ToolStripMenuItem)PluginBase.MainForm.FindMenuItem("ViewMenu");
            menu.DropDownItems.Add(menuItem);
        }

        /// <summary>
        /// Creates a plugin panel for the plugin
        /// </summary>
        void CreatePluginPanel()
        {
            pluginUI = new PluginUI(this) {Text = "Ant"};
            pluginUI.OnChange += OnPluginUIChange;
            pluginPanel = PluginBase.MainForm.CreateDockablePanel(pluginUI, PLUGIN_GUID, pluginImage, DockState.DockRight);
        }

	    /// <summary>
        /// Loads the plugin settings
        /// </summary>
        void LoadSettings()
        {
            settings = new Settings();
            if (!File.Exists(settingFilename)) SaveSettings();
            else settings = (Settings)ObjectSerializer.Deserialize(settingFilename, settings);
        }

        /// <summary>
        /// Saves the plugin settings
        /// </summary>
        void SaveSettings()
        {
            ObjectSerializer.Serialize(settingFilename, settings);
        }

        /// <summary>
        /// Opens the plugin panel if closed
        /// </summary>
        void OpenPanel(object sender, EventArgs e)
        {
            pluginPanel.Show();
        }

        /// <summary>
        /// </summary>
        void SaveBuildFiles()
        {
            string folder = GetBuildFilesStorageFolder();
            string fullName = Path.Combine(folder, STORAGE_FILE_NAME);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            StreamWriter file = new StreamWriter(fullName);
            foreach (string line in BuildFilesList)
                file.WriteLine(line);
            file.Close();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        static string GetBuildFilesStorageFolder()
        {
            return Path.Combine(Path.GetDirectoryName(PluginBase.CurrentProject.ProjectPath), "obj");
        }

		#endregion

        #region Event Handlers

        void OnDirectoryNodeRefresh(DirectoryNode node)
        {
            projectTree = node.TreeView;
        }

        void OnTreeSelectionChanged()
        {
            if (projectTree == null || !(projectTree.SelectedNode is FileNode)) return;
            string path = Path.GetFullPath(((FileNode)projectTree.SelectedNode).BackingPath);
            if (BuildFilesList.Contains(path) || Path.GetExtension(path) != ".xml") return;
            projectTree.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            projectTree.ContextMenuStrip.Items.Add("Add as Ant Build File", pluginImage, OnAddAsAntBuildFile);
        }

        void OnAddAsAntBuildFile(object sender, EventArgs e)
        {
            string path = Path.GetFullPath(((FileNode)projectTree.SelectedNode).BackingPath);
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