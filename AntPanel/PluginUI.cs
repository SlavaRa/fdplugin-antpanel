using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using PluginCore;
using ScintillaNet;

namespace AntPanel
{
    public partial class PluginUI : UserControl
    {
        public const int ICON_FILE = 0;
        public const int ICON_DEFAULT_TARGET = 1;
        public const int ICON_INTERNAL_TARGET = 2;
        public const int ICON_PUBLIC_TARGET = 3;
        private readonly PluginMain pluginMain;
        public delegate void PluginUIEventHandler(object sender, PluginUIArgs e);
        public event PluginUIEventHandler OnChange;
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
            foreach (string file in pluginMain.BuildFilesList.Where(File.Exists))
            {
                tree.Nodes.Add(GetBuildFileNode(file));
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
            AntTreeNode rootNode = new AntTreeNode(projectName, ICON_FILE)
            {
                File = file,
                Target = defaultTarget,
                ToolTipText = description
            };
            XmlNodeList nodes = xml.DocumentElement.ChildNodes;
            int nodeCount = nodes.Count;
            for (int i = 0; i < nodeCount; i++)
            {
                XmlNode child = nodes[i];
                if (child.Name != "target") continue;
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
                targetNode = new AntTreeNode(targetName, ICON_PUBLIC_TARGET)
                {
                    NodeFont = new Font(tree.Font.Name, tree.Font.Size, FontStyle.Bold)
                };
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
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "BuildFiles (*.xml)|*.XML|" + "All files (*.*)|*.*",
                Multiselect = true
            };
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
            if (e.Data.GetDataPresent("AntPanel.AntTreeNode"))
            {
                TreeNode node = (AntTreeNode)e.Data.GetData(("AntPanel.AntTreeNode"));
                if (node.ImageIndex != ICON_FILE) return;
                tree.SelectedNode = null;
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                string[] s = (string[])e.Data.GetData(DataFormats.FileDrop);
                string[] xmls = s.Where(t => t.EndsWith(".xml", true, null)).ToArray();
                if (xmls.Length > 0)
                {
                    e.Effect = DragDropEffects.Copy;
                    dropFiles = xmls;
                }
                else dropFiles = null;
            }
        }

        private void OnTreeDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("AntPanel.AntTreeNode"))
            {
                TreeNode node = (AntTreeNode)e.Data.GetData(("AntPanel.AntTreeNode"));
                if (node.ImageIndex != ICON_FILE) return;
                Point p = tree.PointToClient(new Point(e.X, e.Y));
                TreeNode dropTarget = tree.GetNodeAt(p);
                if (dropTarget == null) dropTarget = tree.Nodes[tree.Nodes.Count - 1];
                else if (dropTarget.ImageIndex != ICON_FILE) dropTarget = dropTarget.Parent;
                tree.SelectedNode = dropTarget;
            }
            else if (dropFiles != null) e.Effect = DragDropEffects.Copy;
        }

        private void OnTreeDragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("AntPanel.AntTreeNode"))
            {
                TreeNode node = (AntTreeNode)e.Data.GetData(("AntPanel.AntTreeNode"));
                if (node.ImageIndex != ICON_FILE) return;
                Point p = tree.PointToClient(new Point(e.X, e.Y));
                TreeNode dropTarget = tree.GetNodeAt(p);
                if (dropTarget == null)
                {
                    node.Remove();
                    tree.Nodes.Add(node);
                }
                else
                {
                    if (dropTarget.ImageIndex != ICON_FILE) dropTarget = dropTarget.Parent;
                    int index = dropTarget.Index;
                    node.Remove();
                    tree.Nodes.Insert(index, node);
                }
                tree.SelectedNode = node;
                if (OnChange == null) return;
                List<string> paths = (from AntTreeNode antTreeNode in tree.Nodes select antTreeNode.File).ToList();
                OnChange(this, new PluginUIArgs(paths));
            }
            else if (dropFiles != null) pluginMain.AddBuildFiles(dropFiles);
        }

        private void OnTreeItemDrag(object sender, ItemDragEventArgs e)
        {
            DoDragDrop(e.Item, DragDropEffects.Move);
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

    public class PluginUIArgs
    {
        public PluginUIArgs(IEnumerable<string> paths)
        {
            Paths = paths;
        }

        public IEnumerable<string> Paths { get; private set; }
    }
}