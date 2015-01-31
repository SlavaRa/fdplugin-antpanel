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
    /// <summary>
    /// </summary>
    public partial class PluginUI : UserControl
    {
        private const int ICON_FILE = 0;
        private const int ICON_INTERNAL_TARGET = 2;
        private const int ICON_PUBLIC_TARGET = 3;
        private const Keys EDIT_KEYS = Keys.F4;
        private const Keys DEL_KEYS = Keys.Delete;

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void PluginUIEventHandler(object sender, PluginUIArgs e);

        /// <summary>
        /// </summary>
        public event PluginUIEventHandler OnChange;

        private readonly PluginMain pluginMain;
        private Image removeImage;
        private Image editImage;
        private IEnumerable<string> dropFiles;
        private ContextMenuStrip buildFileMenu;
        private ContextMenuStrip targetMenu;

        /// <summary>
        /// Initializes a new instance of the AntPanel.PluginUI
        /// </summary>
        /// <param name="pluginMain"></param>
        public PluginUI(PluginMain pluginMain)
        {
            this.pluginMain = pluginMain;
            InitializeImages();
            InitializeComponent();
            toolStrip.Renderer = new DockPanelStripRenderer();
            InitializeButtons();
            InitializeContextMenu();
            StartDragHandling();
            RefreshData();
        }

        /// <summary>
        /// </summary>
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

        /// <summary>
        /// </summary>
        private void InitializeImages()
        {
            removeImage = PluginBase.MainForm.FindImage("153");
            editImage = PluginBase.MainForm.FindImage("214");
        }

        /// <summary>
        /// </summary>
        private void InitializeButtons()
        {
            add.Image = PluginBase.MainForm.FindImage("33");
            remove.Image = removeImage;
            run.Image = PluginBase.MainForm.FindImage("487");
            refresh.Image = PluginBase.MainForm.FindImage("66");
        }

        /// <summary>
        /// </summary>
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
            buildFileMenu.Items.Add(new ToolStripMenuItem("Run default target", run.Image, OnMenuRunClick)
            {
                ShortcutKeyDisplayString = "Enter"
            });
            buildFileMenu.Items.Add(new ToolStripMenuItem("Edit file", editImage, OnMenuEditClick)
            {
                ShortcutKeys = EDIT_KEYS
            });
            buildFileMenu.Items.Add(new ToolStripSeparator());
            buildFileMenu.Items.Add(new ToolStripMenuItem("Remove", removeImage, OnMenuRemoveClick)
            {
                ShortcutKeys = DEL_KEYS
            });
            targetMenu = new ContextMenuStrip();
            targetMenu.Items.Add(new ToolStripMenuItem("Run target", run.Image, OnMenuRunClick)
            {
                ShortcutKeyDisplayString = "Enter"
            });
            targetMenu.Items.Add(new ToolStripMenuItem("Show in Editor", editImage, OnMenuEditClick)
            {
                ShortcutKeys = EDIT_KEYS
            });
        }

        /// <summary>
        /// </summary>
        private void StartDragHandling()
        {
            tree.AllowDrop = true;
            tree.DragEnter += OnTreeDragEnter;
            tree.DragDrop += OnTreeDragDrop;
            tree.DragOver += OnTreeDragOver;
        }

        /// <summary>
        /// </summary>
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

        /// <summary>
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
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
            if (string.IsNullOrEmpty(projectName)) projectName = file;
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

        /// <summary>
        /// </summary>
        /// <param name="node"></param>
        /// <param name="defaultTarget"></param>
        /// <returns></returns>
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

        /// <summary>
        /// </summary>
        private void RunSelectedTarget()
        {
            AntTreeNode node = tree.SelectedNode as AntTreeNode;
            if (node != null) pluginMain.RunTarget(node.File, node.Target);
        }

        /// <summary>
        /// </summary>
        private void RemoveSelectedTarget()
        {
            AntTreeNode node = tree.SelectedNode as AntTreeNode;
            if (node != null) pluginMain.RemoveBuildFile(node.File);
        }

        /// <summary>
        /// </summary>
        private void EditSelectedNode()
        {
            AntTreeNode node = tree.SelectedNode as AntTreeNode;
            if (node == null) return;
            PluginBase.MainForm.OpenEditableDocument(node.File, false);
            ScintillaControl sci = PluginBase.MainForm.CurrentDocument.SciControl;
            Match match = Regex.Match(sci.Text, "<target[^>]+name\\s*=\\s*\"" + node.Target + "\".*>", RegexOptions.Compiled);
            if (!match.Success) return;
            sci.GotoPos(match.Index);
            sci.SetSel(match.Index, match.Index + match.Length);
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
            RemoveSelectedTarget();
        }

        private void OnRunClick(object sender, EventArgs e)
        {
            RunSelectedTarget();
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
            RunSelectedTarget();
        }

        private void OnTreeKeyDown(object sender, KeyEventArgs e)
        {
            TreeNode selectedNode = tree.SelectedNode;
            if (selectedNode == null) return;
            switch (e.KeyCode)
            {
                case EDIT_KEYS:
                    EditSelectedNode();
                    break;
                case Keys.Enter:
                    RunSelectedTarget();
                    break;
                case Keys.Apps:
                    if (selectedNode.Parent == null) buildFileMenu.Show(tree, selectedNode.Bounds.Location);
                    else targetMenu.Show(tree, selectedNode.Bounds.Location);
                    break;
                case DEL_KEYS:
                    if (selectedNode.ImageIndex == ICON_FILE) RemoveSelectedTarget();
                    break;
                default: return;
            }
            e.Handled = true;
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
                IEnumerable<string> xmls = s.Where(t => t.EndsWith(".xml", true, null));
                if (xmls.Any())
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
            RunSelectedTarget();
        }

        private void OnMenuEditClick(object sender, EventArgs e)
        {
            EditSelectedNode();
        }

        private void OnMenuRemoveClick(object sender, EventArgs e)
        {
            RemoveSelectedTarget();
        }

        #endregion
    }

    /// <summary>
    /// </summary>
    class AntTreeNode : TreeNode
    {
        /// <summary>
        /// </summary>
        public string File;

        /// <summary>
        /// </summary>
        public string Target;

        /// <summary>
        /// Initializes a new instance of the AntPanel.AntTreeNode
        /// </summary>
        /// <param name="text"></param>
        /// <param name="imageIndex"></param>
        public AntTreeNode(string text, int imageIndex)
            : base(text, imageIndex, imageIndex)
        {
        }
    }

    /// <summary>
    /// </summary>
    public class PluginUIArgs
    {
        /// <summary>
        /// Initializes a new instance of the AntPanel.PluginUIArgs
        /// </summary>
        /// <param name="paths"></param>
        public PluginUIArgs(IEnumerable<string> paths)
        {
            Paths = paths;
        }

        /// <summary>
        /// </summary>
        public IEnumerable<string> Paths { get; private set; }
    }
}