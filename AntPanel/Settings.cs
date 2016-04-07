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
        [Description("Path to Ant installation directory")]
        [DefaultValue("")]
        [Editor(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string AntPath { get; set; }

        [DisplayName("Hide no-description targets"), DefaultValue(false)]
        [Description("Hide targets that don't have a description (unless all of the targets don't have one)")]
        public Boolean SkipHiddenTargets { get; set; }
    }
}