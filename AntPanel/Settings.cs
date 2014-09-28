using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;

namespace AntPanel
{
    [Serializable]
    public class Settings
    {
        [DisplayName("Path to Ant")]
        [Description("Path to Ant installation dir")]
        [DefaultValue("")]
        [Editor(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string AntPath { get; set; }
    }
}