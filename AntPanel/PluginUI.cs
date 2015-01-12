using PluginCore;
using ScintillaNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;

namespace AntPanel
{
    public partial class PluginUI : UserControl
    {
        public const int ICON_FILE = 0;
        public const int ICON_DEFAULT_TARGET = 1;
        public const int ICON_INTERNAL_TARGET = 2;
        public const int ICON_PUBLIC_TARGET = 3;
        private PluginMain pluginMain;
        private ContextMenuStrip buildFileMenu;
        private ContextMenuStrip targetMenu;
        
        public PluginUI(PluginMain pluginMain)
        {
            this.pluginMain = pluginMain;
            InitializeComponent();
            toolStrip.Renderer = new DockPanelStripRenderer();
            InitializeButtons();
            InitializeContextMenu();
            StartDragHandling();
            RefreshData();
        }

        public void RefreshData()
        {
            Enabled = (PluginBase.CurrentProject != null);
            if (Enabled) 
            {
                FillTree();
                UpdateButtons();
            }
            else
            {
                tree.Nodes.Clear();
                tree.Nodes.Add(new TreeNode("No project opened"));
            }
        }

        private void InitializeButtons()
        {
            add.Image = PluginBase.MainForm.FindImage("33");
            remove.Image = PluginBase.MainForm.FindImage("153");
            run.Image = PluginBase.MainForm.FindImage("487");
            refresh.Image = PluginBase.MainForm.FindImage("66");
        }

        private void UpdateButtons()
        {
            bool isNotEmpty = tree.Nodes.Count > 0;
            remove.Enabled = isNotEmpty;
            run.Enabled = isNotEmpty;
            refresh.Enabled = isNotEmpty;
        }

        /// <summary>
        /// Initializes the context menu
        /// </summary>
        private void InitializeContextMenu()
        {
            buildFileMenu = new ContextMenuStrip();
            buildFileMenu.Items.Add("Run default target", run.Image, OnMenuRunClick);
            buildFileMenu.Items.Add("Edit file", null, OnMenuEditClick);
            buildFileMenu.Items.Add(new ToolStripSeparator());
            buildFileMenu.Items.Add("Remove", PluginBase.MainForm.FindImage("153"), OnMenuRemoveClick);
            targetMenu = new ContextMenuStrip();
            targetMenu.Items.Add("Run target", run.Image, OnMenuRunClick);
            targetMenu.Items.Add("Show in Editor", null, OnMenuEditClick);
        }

        private void StartDragHandling()
        {
            tree.AllowDrop = true;
            tree.DragEnter += OnTreeDragEnter;
            tree.DragDrop += OnTreeDragDrop;
            tree.DragOver += OnTreeDragOver;
        }

        private void FillTree()
        {
            tree.BeginUpdate();
            tree.Nodes.Clear();
            foreach (string file in pluginMain.BuildFilesList)
            {
                if (File.Exists(file)) tree.Nodes.Add(GetBuildFileNode(file));
            }
            tree.EndUpdate();
        }

        private TreeNode GetBuildFileNode(string file)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(file);
            XmlAttribute defTargetAttr = xml.DocumentElement.Attributes["default"];
            string defaultTarget = (defTargetAttr != null) ? defTargetAttr.InnerText : "";
            XmlAttribute nameAttr = xml.DocumentElement.Attributes["name"];
            string projectName = (nameAttr != null) ? nameAttr.InnerText : file;
            XmlAttribute descrAttr = xml.DocumentElement.Attributes["description"];
            string description = (descrAttr != null) ? descrAttr.InnerText : "";
            if (projectName.Length == 0)
            projectName = file;
            AntTreeNode rootNode = new AntTreeNode(projectName, ICON_FILE);
            rootNode.File = file;
            rootNode.Target = defaultTarget;
            rootNode.ToolTipText = description;
            XmlNodeList nodes = xml.DocumentElement.ChildNodes;
            int nodeCount = nodes.Count;
            for (int i = 0; i < nodeCount; i++)
            {
                XmlNode child = nodes[i];
                if (child.Name == "target")
                {
                    // skip private targets
                    XmlAttribute targetNameAttr = child.Attributes["name"];
                    if (targetNameAttr != null)
                    {
                        string targetName = targetNameAttr.InnerText;
                        if (!string.IsNullOrEmpty(targetName) && (targetName[0] == '-'))
                        {
                            continue;
                        }
                    }
                    AntTreeNode targetNode = GetBuildTargetNode(child, defaultTarget);
                    targetNode.File = file;
                    rootNode.Nodes.Add(targetNode);
                }
            }
            rootNode.Expand();
            return rootNode;
        }

        private AntTreeNode GetBuildTargetNode(XmlNode node, string defaultTarget)
        {
            XmlAttribute nameAttr = node.Attributes["name"];
            string targetName = (nameAttr != null) ? nameAttr.InnerText : "";
            XmlAttribute descrAttr = node.Attributes["description"];
            string description = (descrAttr != null) ? descrAttr.InnerText : "";
            AntTreeNode targetNode;
            if (targetName == defaultTarget)
            {
                targetNode = new AntTreeNode(targetName, ICON_PUBLIC_TARGET);
                targetNode.NodeFont = new Font(tree.Font.Name, tree.Font.Size, FontStyle.Bold);
            }
            else if (description.Length > 0) targetNode = new AntTreeNode(targetName, ICON_PUBLIC_TARGET);
            else targetNode = new AntTreeNode(targetName, ICON_INTERNAL_TARGET);
            targetNode.Target = targetName;
            targetNode.ToolTipText = description;
            return targetNode;
        }

        private void RunTarget()
        {
            AntTreeNode node = tree.SelectedNode as AntTreeNode;
            if (node != null) pluginMain.RunTarget(node.File, node.Target);
        }

        private void RemoveTarget()
        {
            AntTreeNode node = tree.SelectedNode as AntTreeNode;
            if (node != null) pluginMain.RemoveBuildFile(node.File);
        }

        #region Event Handlers

        private void OnAddClick(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "BuildFiles (*.xml)|*.XML|" + "All files (*.*)|*.*";
            dialog.Multiselect = true;
            if (PluginBase.CurrentProject != null) dialog.InitialDirectory = Path.GetDirectoryName(PluginBase.CurrentProject.ProjectPath);
            if (dialog.ShowDialog() == DialogResult.OK) pluginMain.AddBuildFiles(dialog.FileNames);
        }

        private void OnRemoveClick(object sender, EventArgs e)
        {
            RemoveTarget();
        }

        private void OnRunClick(object sender, EventArgs e)
        {
            RunTarget();
        }
        
        private void OnRefreshClick(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void OnTreeNodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            AntTreeNode currentNode = tree.GetNodeAt(e.Location) as AntTreeNode;
            tree.SelectedNode = currentNode;
            if (currentNode.Parent == null) buildFileMenu.Show(tree, e.Location);
            else targetMenu.Show(tree, e.Location);
        }

        private void OnTreeNodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            RunTarget();
        }

        private void OnTreeKeyDown(object sender, KeyEventArgs e)
        {
            if (tree.SelectedNode == null) return;
            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (tree.SelectedNode.NextVisibleNode == null)
                    {
                        e.Handled = true;
                        tree.SelectedNode = tree.Nodes[0];
                    }
                    break;
                case Keys.Up:
                    if (tree.SelectedNode.PrevVisibleNode == null)
                    {
                        e.Handled = true;
                        TreeNode node = tree.SelectedNode;
                        while (node.NextVisibleNode != null) node = node.NextVisibleNode;
                        tree.SelectedNode = node;
                    }
                    break;
                case Keys.Enter:
                    e.Handled = true;
                    RunTarget();
                    break;
                case Keys.Apps:
                    e.Handled = true;
                    TreeNode selectedNode = tree.SelectedNode;
                    if (selectedNode.Parent == null) buildFileMenu.Show(tree, selectedNode.Bounds.Location);
                    else targetMenu.Show(tree, selectedNode.Bounds.Location);
                    break;
                case Keys.Delete:
                    if (tree.SelectedNode.ImageIndex == ICON_FILE)
                    {
                        e.Handled = true;
                        RemoveTarget();
                    }
                    break;
            }
        }

        private void OnTreeMouseDown(object sender, MouseEventArgs e)
        {
            int delta = (int)DateTime.Now.Subtract(lastMouseDown).TotalMilliseconds;
            preventExpand = (delta < SystemInformation.DoubleClickTime);
            lastMouseDown = DateTime.Now;
        }

        private void OnTreeBeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = preventExpand;
            preventExpand = false;
        }

        private void OnTreeBeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = preventExpand;
            preventExpand = false;
        }

        private void OnTreeDragEnter(object sender, DragEventArgs e)
        {
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop);
            List<string> xmls = new List<string>();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i].EndsWith(".xml", true, null))
                {
                    xmls.Add(s[i]);
                }
            }
            if (xmls.Count > 0)
            {
                e.Effect = DragDropEffects.Copy;
                dropFiles = xmls.ToArray();
            }
            else dropFiles = null;
        }

        private void OnTreeDragOver(object sender, DragEventArgs e)
        {
            if (dropFiles != null) e.Effect = DragDropEffects.Copy;
        }

        private void OnTreeDragDrop(object sender, DragEventArgs e)
        {
            if (dropFiles != null) pluginMain.AddBuildFiles(dropFiles);
        }

        private void OnMenuRunClick(object sender, EventArgs e)
        {
            RunTarget();
        }

        private void OnMenuEditClick(object sender, EventArgs e)
        {
            AntTreeNode node = tree.SelectedNode as AntTreeNode;
            PluginBase.MainForm.OpenEditableDocument(node.File, false);
            ScintillaControl sci = PluginBase.MainForm.CurrentDocument.SciControl;
            Match match = Regex.Match(sci.Text, "<target[^>]+name\\s*=\\s*\"" + node.Target + "\".*>", RegexOptions.Compiled);
            if (!match.Success) return;
            sci.GotoPos(match.Index);
            sci.SetSel(match.Index, match.Index + match.Length);
        }

        private void OnMenuRemoveClick(object sender, EventArgs e)
        {
            RemoveTarget();
        }

        #endregion
    }

    class AntTreeNode : TreeNode
    {
        public string File;
        public string Target;

        public AntTreeNode(string text, int imageIndex)
            : base(text, imageIndex, imageIndex)
        {
        }
    }
}