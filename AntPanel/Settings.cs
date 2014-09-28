using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;

namespace AntPlugin
{
    [Serializable]
    public class Settings
    {
        private const string DEFAULT_ANT_PATH = "";
        private string antPath = DEFAULT_ANT_PATH;

        [DisplayName("Path to Ant")]
        [Description("Path to Ant installation dir")]
        [DefaultValue(DEFAULT_ANT_PATH)]
        [Editor(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string AntPath
        {
            get { return antPath; }
            set { antPath = value; }
        }

    }
}