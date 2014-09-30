using System.Windows.Forms;
using System;
using System.Collections.Generic;

namespace AntPanel
{
    partial class PluginUI
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private String[] dropFiles = null;
        private bool preventExpand = false;
        private DateTime lastMouseDown = DateTime.Now;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PluginUI));
            this.toolStrip = new System.Windows.Forms.ToolStrip();
            this.addButton = new System.Windows.Forms.ToolStripButton();
            this.refreshButton = new System.Windows.Forms.ToolStripButton();
            this.runButton = new System.Windows.Forms.ToolStripButton();
            this.tree = new System.Windows.Forms.TreeView();
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.toolStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip
            // 
            this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addButton,
            this.refreshButton,
            this.runButton});
            this.toolStrip.Location = new System.Drawing.Point(0, 0);
            this.toolStrip.Name = "toolStrip";
            this.toolStrip.Size = new System.Drawing.Size(279, 25);
            this.toolStrip.TabIndex = 0;
            this.toolStrip.Text = "toolStrip1";
            // 
            // addButton
            // 
            this.addButton.Image = ((System.Drawing.Image)(resources.GetObject("addButton.Image")));
            this.addButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(49, 22);
            this.addButton.Text = "Add";
            this.addButton.ToolTipText = "Add build file";
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            // 
            // refreshButton
            // 
            this.refreshButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.refreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.refreshButton.Image = ((System.Drawing.Image)(resources.GetObject("refreshButton.Image")));
            this.refreshButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(23, 22);
            this.refreshButton.Text = "toolStripButton2";
            this.refreshButton.ToolTipText = "Refresh";
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // runButton
            // 
            this.runButton.Image = ((System.Drawing.Image)(resources.GetObject("runButton.Image")));
            this.runButton.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.runButton.Name = "runButton";
            this.runButton.Size = new System.Drawing.Size(48, 22);
            this.runButton.Text = "Run";
            this.runButton.Click += new System.EventHandler(this.runButton_Click);
            // 
            // treeView
            // 
            this.tree.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tree.HideSelection = false;
            this.tree.ImageIndex = 0;
            this.tree.ImageList = this.imageList;
            this.tree.Location = new System.Drawing.Point(0, 25);
            this.tree.Name = "treeView";
            this.tree.SelectedImageIndex = 0;
            this.tree.ShowNodeToolTips = true;
            this.tree.Size = new System.Drawing.Size(279, 285);
            this.tree.TabIndex = 1;
            this.tree.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView_NodeMouseDoubleClick);
            this.tree.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeView_BeforeExpand);
            this.tree.BeforeCollapse += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeView_BeforeCollapse);
            this.tree.MouseDown += new System.Windows.Forms.MouseEventHandler(this.treeView_MouseDown);
            this.tree.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeView_NodeMouseClick);
            this.tree.KeyUp += treeView_NodeKeyUp;
            this.tree.KeyPress += treeView_NodeKeyPress;
            // 
            // imageList
            // 
            this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList.Images.SetKeyName(0, "ant_buildfile.png");
            this.imageList.Images.SetKeyName(1, "defaulttarget_obj.png");
            this.imageList.Images.SetKeyName(2, "targetinternal_obj.png");
            this.imageList.Images.SetKeyName(3, "targetpublic_obj.png");
            // 
            // PluginUI
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tree);
            this.Controls.Add(this.toolStrip);
            this.Name = "PluginUI";
            this.Size = new System.Drawing.Size(279, 310);
            this.toolStrip.ResumeLayout(false);
            this.toolStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip;
        private System.Windows.Forms.TreeView tree;
        private System.Windows.Forms.ToolStripButton addButton;
        private System.Windows.Forms.ToolStripButton refreshButton;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.ToolStripButton runButton;

        private void treeView_MouseDown(object sender, MouseEventArgs e)
        {
            int delta = (int)DateTime.Now.Subtract(lastMouseDown).TotalMilliseconds;
            preventExpand = (delta < SystemInformation.DoubleClickTime);
            lastMouseDown = DateTime.Now;
        }

        private void treeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = preventExpand;
            preventExpand = false;
        }

        private void treeView_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = preventExpand;
            preventExpand = false;
        }

        internal void StartDragHandling()
        {
            this.tree.AllowDrop = true;
            this.tree.DragEnter += new DragEventHandler(treeView_DragEnter);
            this.tree.DragDrop += new DragEventHandler(treeView_DragDrop);
            this.tree.DragOver += new DragEventHandler(treeView_DragOver);
        }

        void treeView_DragEnter(object sender, DragEventArgs e)
        {
            String[] s = (String[])e.Data.GetData(DataFormats.FileDrop);
            List<String> xmls = new List<String>();
            for (Int32 i = 0; i < s.Length; i++)
            {
                if (s[i].EndsWith(".xml", true, null))
                {
                    xmls.Add(s[i]);
                }
            }
            if (xmls.Count > 0)
            {
                e.Effect = DragDropEffects.Copy;
                this.dropFiles = xmls.ToArray();
            }
            else this.dropFiles = null;
        }

        void treeView_DragOver(object sender, DragEventArgs e)
        {
            if (this.dropFiles != null)
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        void treeView_DragDrop(object sender, DragEventArgs e)
        {
            if (this.dropFiles != null)
            {
                this.pluginMain.AddBuildFiles(this.dropFiles);
            }
        }

    }
}
