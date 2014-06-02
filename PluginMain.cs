using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;
using PluginCore.Utilities;
using PluginCore.Managers;
using PluginCore.Helpers;
using PluginCore;
using WeifenLuo.WinFormsUI.Docking;

namespace AntPlugin
{
	public class PluginMain : IPlugin
	{
        private const int PLUGIN_API = 1;
        private const string PLUGIN_NAME = "AntPlugin";
        private const string PLUGIN_GUID = "92d9a647-6cd3-4347-9db6-95f324292399";
        private const string PLUGIN_HELP = "www.flashdevelop.org/community/";
        private const string PLUGIN_AUTH = "Canab";
	    private const string SETTINGS_FILE = "Settings.fdb";
        private const string PLUGIN_DESC = "Ant plugin";
        private const string STORAGE_FILE_NAME = "antPluginData.txt";
        public List<string> BuildFilesList { get; private set; }
        private string settingFilename;
        private Settings settings;
        private DockContent pluginPanel;
	    private PluginUI pluginUI;
	    private Image pluginImage;

	    #region Required Properties

        public int Api
        {
            get { return PLUGIN_API; }
        }
        
        /// <summary>
        /// Name of the plugin
        /// </summary> 
        public string Name
		{
			get { return PLUGIN_NAME; }
		}

        /// <summary>
        /// GUID of the plugin
        /// </summary>
        public string Guid
		{
			get { return PLUGIN_GUID; }
		}

        /// <summary>
        /// Author of the plugin
        /// </summary> 
        public string Author
		{
			get { return PLUGIN_AUTH; }
		}

        /// <summary>
        /// Description of the plugin
        /// </summary> 
        public string Description
		{
			get { return PLUGIN_DESC; }
		}

        /// <summary>
        /// Web address for help
        /// </summary> 
        public string Help
		{
			get { return PLUGIN_HELP; }
		}

        /// <summary>
        /// Object that contains the settings
        /// </summary>
        [Browsable(false)]
        public object Settings
        {
            get { return settings; }
        }
		
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
            CreateMenuItems();
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
            EventManager.AddEventHandler(this, EventType.Command);
        }
        
        public void HandleEvent(object sender, NotifyEvent e, HandlingPriority prority)
		{
            if (e.Type == EventType.Command)
            {
                string cmd = (e as DataEvent).Action;
                if (cmd == "ProjectManager.Project")
                {
                    if (PluginBase.CurrentProject != null) ReadBuildFiles();
                    pluginUI.RefreshData();
                }
            }
		}
		
		#endregion

        #region Custom Public Methods

        public void AddBuildFiles(string[] files)
        {
            foreach (string file in files)
                if (!BuildFilesList.Contains(file)) BuildFilesList.Add(file);
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
            string command = Path.Combine(Environment.SystemDirectory, "cmd.exe");
            string arguments = "/c ";
            if (string.IsNullOrEmpty(settings.AntPath)) arguments += "ant";
            else arguments += Path.Combine(Path.Combine(settings.AntPath, "bin"), "ant");
            arguments += " -buildfile \"" + file + "\" \"" + target + "\"";
            PluginBase.MainForm.CallCommand("RunProcessCaptured", command + ";" + arguments);
        }

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

        private void InitBasics()
        {
            BuildFilesList = new List<string>();
            pluginImage = PluginBase.MainForm.FindImage("486");
            string dataPath = Path.Combine(PathHelper.DataDir, PLUGIN_NAME);
            if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
            settingFilename = Path.Combine(dataPath, SETTINGS_FILE);
        }

        private void CreateMenuItems()
        {
            ToolStripMenuItem menuItem = new ToolStripMenuItem("Ant Window", pluginImage, ShowPanel);
            ToolStripMenuItem menu = (ToolStripMenuItem)PluginBase.MainForm.FindMenuItem("ViewMenu");
            PluginBase.MainForm.RegisterShortcutItem("ViewMenu.ShowAntPanel", menuItem);
            menu.DropDownItems.Add(menuItem);
        }

        private void CreatePluginPanel()
        {
            pluginUI = new PluginUI(this);
            pluginUI.Text = "Ant";
            pluginUI.StartDragHandling();
            pluginPanel = PluginBase.MainForm.CreateDockablePanel(pluginUI, PLUGIN_GUID, pluginImage, DockState.DockRight);
        }

        private void ShowPanel(object sender, EventArgs e)
	    {
            pluginPanel.Show();
	    }

        private void SaveBuildFiles()
        {
            string folder = GetBuildFilesStorageFolder();
            string fullName = Path.Combine(folder, STORAGE_FILE_NAME);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            StreamWriter file = new StreamWriter(fullName);
            foreach (string line in BuildFilesList)
                file.WriteLine(line);
            file.Close();
        }

        private string GetBuildFilesStorageFolder()
        {
            string projectFolder = Path.GetDirectoryName(PluginBase.CurrentProject.ProjectPath);
            return Path.Combine(projectFolder, "obj");
        }

        private void LoadSettings()
        {
            settings = new Settings();
            if (!File.Exists(settingFilename)) SaveSettings();
            else settings = (Settings)ObjectSerializer.Deserialize(settingFilename, settings);
        }

        private void SaveSettings()
        {
            ObjectSerializer.Serialize(settingFilename, settings);
        }

		#endregion
    }
}