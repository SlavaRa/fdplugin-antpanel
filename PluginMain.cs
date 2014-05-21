using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;
using FlashDevelop;
using PluginCore.Utilities;
using PluginCore.Managers;
using PluginCore.Helpers;
using PluginCore;
using WeifenLuo.WinFormsUI.Docking;

namespace AntPlugin
{
	public class PluginMain : IPlugin
	{
        private const Int32 PLUGIN_API = 1;
        private const String PLUGIN_NAME = "AntPlugin";
        private const String PLUGIN_GUID = "92d9a647-6cd3-4347-9db6-95f324292399";
        private const String PLUGIN_HELP = "www.flashdevelop.org/community/";
        private const String PLUGIN_AUTH = "Canab";
	    private const String SETTINGS_FILE = "Settings.fdb";
        private const String PLUGIN_DESC = "Ant plugin";

        private const String STORAGE_FILE_NAME = "antPluginData.txt";
        
        private List<String> buildFilesList = new List<string>();
        public List<string> BuildFilesList
        {
            get { return buildFilesList; }
        }
        
        private String settingFilename;
        private Settings settingObject;
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
        public String Name
		{
			get { return PLUGIN_NAME; }
		}

        /// <summary>
        /// GUID of the plugin
        /// </summary>
        public String Guid
		{
			get { return PLUGIN_GUID; }
		}

        /// <summary>
        /// Author of the plugin
        /// </summary> 
        public String Author
		{
			get { return PLUGIN_AUTH; }
		}

        /// <summary>
        /// Description of the plugin
        /// </summary> 
        public String Description
		{
			get { return PLUGIN_DESC; }
		}

        /// <summary>
        /// Web address for help
        /// </summary> 
        public String Help
		{
			get { return PLUGIN_HELP; }
		}

        /// <summary>
        /// Object that contains the settings
        /// </summary>
        [Browsable(false)]
        public Object Settings
        {
            get { return settingObject; }
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
        
        public void HandleEvent(Object sender, NotifyEvent e, HandlingPriority prority)
		{
            if (e.Type == EventType.Command)
            {
                string cmd = (e as DataEvent).Action;
                if (cmd == "ProjectManager.Project")
                {
                    if (PluginBase.CurrentProject != null)
                        ReadBuildFiles();
                    pluginUI.RefreshData();
                }
            }
		}
		
		#endregion

        #region Custom Methods

        public void InitBasics()
        {
            pluginImage = PluginBase.MainForm.FindImage("486");
            String dataPath = Path.Combine(PathHelper.DataDir, PLUGIN_NAME);
            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(dataPath);
            settingFilename = Path.Combine(dataPath, SETTINGS_FILE);
        }

        public void CreateMenuItems()
        {
            ToolStripMenuItem menu = (ToolStripMenuItem)PluginBase.MainForm.FindMenuItem("ViewMenu");
            ToolStripMenuItem menuItem;

            menuItem = new ToolStripMenuItem("Ant Window",
                pluginImage, new EventHandler(ShowAntWindow));
            menu.DropDownItems.Add(menuItem);
        }

        private void CreatePluginPanel()
        {
            pluginUI = new PluginUI(this);
            pluginUI.Text = "Ant";
            pluginUI.StartDragHandling();
            pluginPanel = PluginBase.MainForm.CreateDockablePanel(
                pluginUI, PLUGIN_GUID, pluginImage, DockState.DockRight);
        }

        private void ShowAntWindow(object sender, EventArgs e)
	    {
            pluginPanel.Show();
	    }

        public void RunTarget(String file, String target)
        {
            String command = Environment.SystemDirectory + "\\cmd.exe";
            
            String arguments = "/c ";
            if (settingObject.AntPath.Length > 0)
                arguments += Path.Combine(settingObject.AntPath, "bin") + "\\ant";
            else
                arguments += "ant";

            arguments += " -buildfile \"" + file + "\" \"" + target + "\"";
            
			//TraceManager.Add(command + " " + arguments);
            
			Globals.MainForm.CallCommand("RunProcessCaptured", command + ";" + arguments);
        }

        public void AddBuildFiles(String[] files)
        {
            foreach (String file in files)
            {
                if (!buildFilesList.Contains(file))
                    buildFilesList.Add(file);
            }
            SaveBuildFiles();
            pluginUI.RefreshData();
        }

        public void RemoveBuildFile(String file)
        {
            if (buildFilesList.Contains(file))
                buildFilesList.Remove(file);
            SaveBuildFiles();
            pluginUI.RefreshData();
        }

        private void ReadBuildFiles()
        {
            buildFilesList.Clear();
            String folder = GetBuildFilesStorageFolder();
            String fullName = folder + "\\" + STORAGE_FILE_NAME;

            if (File.Exists(fullName))
            {
                StreamReader file = new StreamReader(fullName);
                String line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Length > 0 && !buildFilesList.Contains(line))
                        buildFilesList.Add(line);
                }
                file.Close();
            }
        }

        private void SaveBuildFiles()
        {
            String folder = GetBuildFilesStorageFolder();
            String fullName = folder + "\\" + STORAGE_FILE_NAME;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            StreamWriter file = new StreamWriter(fullName);
            foreach (String line in buildFilesList)
            {
                file.WriteLine(line);
            }
            file.Close();
        }

        private String GetBuildFilesStorageFolder()
        {
            String projectFolder = Path.GetDirectoryName(
                PluginBase.CurrentProject.ProjectPath);
            return Path.Combine(projectFolder, "obj");
        }
        
        public void LoadSettings()
        {
            if (File.Exists(settingFilename))
            {
                try
                {
                    settingObject = new Settings();
                    settingObject = (Settings) ObjectSerializer.Deserialize(settingFilename, settingObject);
                }
                catch
                {
                    settingObject = new Settings();
                    SaveSettings();
                }
            }
            else
            {
                settingObject = new Settings();
                SaveSettings();
            }
        }

        public void SaveSettings()
        {
            ObjectSerializer.Serialize(settingFilename, settingObject);
        }

		#endregion

    }
	
}
