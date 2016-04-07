using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using PluginCore;
using PluginCore.Managers;
using ScintillaNet;

namespace AntPanel
{
    /// <summary>
    /// </summary>
    public partial class PluginUI : DockPanelControl
    {
        const int ICON_FILE = 0;
        const int ICON_INTERNAL_TARGET = 2;
        const int ICON_PUBLIC_TARGET = 3;
        const Keys EDIT_KEYS = Keys.F4;
        const Keys DEL_KEYS = Keys.Delete;

        /// <summary>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void PluginUIEventHandler(object sender, PluginUIArgs e);

        /// <summary>
        /// </summary>
        public event PluginUIEventHandler OnChange;

        readonly PluginMain pluginMain;
        Image removeImage;
        Image editImage;
        IEnumerable<string> dropFiles;
        ContextMenuStrip buildFileMenu;
        ContextMenuStrip targetMenu;
        ContextMenuStrip errorMenu;

        /// <summary>
        /// Initializes a new instance of the AntPanel.PluginUI
        /// </summary>
        /// <param name="pluginMain"></param>
        public PluginUI(PluginMain pluginMain)
        {
            this.pluginMain = pluginMain;
            AutoKeyHandling = true;
            InitializeImages();
            InitializeComponent();
            toolStrip.Renderer = new DockPanelStripRenderer();
            InitializeButtons();
            InitializeContextMenu();
            SetupTreeImages();
            StartDragHandling();
            RefreshData();
        }

        void SetupTreeImages()
        {
            tree.ImageList.Images.SetKeyName(0, "ant_buildfile.png");
            tree.ImageList.Images.SetKeyName(1, "defaulttarget_obj.png");
            tree.ImageList.Images.SetKeyName(2, "targetinternal_obj.png");
            tree.ImageList.Images.SetKeyName(3, "targetpublic_obj.png");
            tree.ImageList.Images.Add(PluginBase.MainForm.FindImage("197"));
        }

        /// <summary>
        /// </summary>
        void InitializeImages()
        {
            removeImage = PluginBase.MainForm.FindImage("153");
            editImage = PluginBase.MainForm.FindImage("214");
        }

        /// <summary>
        /// </summary>
        void InitializeButtons()
        {
            add.Image = PluginBase.MainForm.FindImage("33");
            remove.Image = removeImage;
            run.Image = PluginBase.MainForm.FindImage("487");
            refresh.Image = PluginBase.MainForm.FindImage("66");
        }

        /// <summary>
        /// </summary>
        void UpdateButtons()
        {
            bool isNotEmpty = tree.Nodes.Count > 0;
            remove.Enabled = isNotEmpty;
            run.Enabled = isNotEmpty;
            refresh.Enabled = isNotEmpty;
        }

        /// <summary>
        /// Initializes the context menu
        /// </summary>
        void InitializeContextMenu()
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
            errorMenu = new ContextMenuStrip();
            errorMenu.Items.Add(new ToolStripMenuItem("Show in Editor", editImage, OnMenuEditClick)
            {
                ShortcutKeys = EDIT_KEYS
            });
            errorMenu.Items.Add(new ToolStripMenuItem("Remove", removeImage, OnMenuRemoveClick)
            {
                ShortcutKeys = DEL_KEYS
            });
        }

        /// <summary>
        /// </summary>
        void StartDragHandling()
        {
            tree.AllowDrop = true;
            tree.DragEnter += OnTreeDragEnter;
            tree.DragDrop += OnTreeDragDrop;
            tree.DragOver += OnTreeDragOver;
        }

        /// <summary>
        /// </summary>
        public void RefreshData()
        {
            Enabled = PluginBase.CurrentProject != null;
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
        void FillTree()
        {
            tree.BeginUpdate();
            tree.Nodes.Clear();
            foreach (string file in pluginMain.BuildFilesList.Where(File.Exists))
            {
                TreeNode node;
                try
                {
                    node = GetBuildFileNode(file);
                }
                catch (Exception ex)
                {
                    string[] strings = ex.Message.Split('.');
                    int msgLength = strings.Length - 2;
                    string positions = strings[msgLength];
                    MatchCollection matches = Regex.Matches(positions, @"(\d+)");
                    if (matches.Count == 0) return;
                    string text = string.Empty;
                    for (int i = 0; i < msgLength; i++) text += strings[i];
                    string line = matches[0].Value;
                    string position = (int.Parse(matches[1].Value) - 1).ToString();
                    TraceManager.Add($"{file}:{line}: chars {position}-{position} : {text}.", (int) TraceType.Error);
                    PluginBase.MainForm.CallCommand("PluginCommand", "ResultsPanel.ShowResults");
                    node = new ErrorTreeNode(file, 4, int.Parse(line) - 1, int.Parse(position));
                }
                if (node != null) tree.Nodes.Add(node);
            }
            tree.EndUpdate();
        }

        /// <summary>
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        AntTreeNode GetBuildFileNode(string file)
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(file);
            XmlElement documentElement = xml.DocumentElement;
            Debug.Assert(documentElement != null, "documentElement != null");
            XmlAttribute defTargetAttr = documentElement.Attributes["default"];
            string defaultTarget = defTargetAttr?.InnerText ?? "";
            XmlAttribute nameAttr = documentElement.Attributes["name"];
            string projectName = nameAttr?.InnerText ?? file;
            XmlAttribute descrAttr = documentElement.Attributes["description"];
            string description = descrAttr?.InnerText ?? "";
            if (string.IsNullOrEmpty(projectName)) projectName = file;
            AntTreeNode rootNode = new AntTreeNode(projectName, ICON_FILE)
            {
                File = file,
                Target = defaultTarget,
                ToolTipText = description
            };
            bool skipHiddenTargets = ((Settings)pluginMain.Settings).SkipHiddenTargets;
            if (skipHiddenTargets)
            {
                // no-description targets should be hidden only if at least one target has a description
                skipHiddenTargets = false;
                foreach (XmlNode node in documentElement.ChildNodes)
                {
                    if (node.Name != "target") continue;
                    XmlAttributeCollection attributes = node.Attributes;
                    Debug.Assert(attributes != null, "attributes != null");
                    if (!string.IsNullOrEmpty(attributes["description"]?.InnerText))
                    {
                        skipHiddenTargets = true;
                        break;
                    }
                }
            }
            foreach (XmlNode node in documentElement.ChildNodes)
            {
                if (node.Name != "target") continue;
                // skip private and optionally hidden targets
                XmlAttributeCollection attributes = node.Attributes;
                Debug.Assert(attributes != null, "attributes != null");
                XmlAttribute targetNameAttr = attributes["name"];
                string targetName = targetNameAttr?.InnerText;
                if (!string.IsNullOrEmpty(targetName) && (targetName[0] == '-'))
                    continue;
                if (skipHiddenTargets && string.IsNullOrEmpty(attributes["description"]?.InnerText))
                    continue;
                AntTreeNode targetNode = GetBuildTargetNode(node, defaultTarget);
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
        AntTreeNode GetBuildTargetNode(XmlNode node, string defaultTarget)
        {
            XmlAttributeCollection attributes = node.Attributes;
            Debug.Assert(attributes != null, "attributes != null");
            XmlAttribute nameAttr = attributes["name"];
            string targetName = nameAttr?.InnerText ?? "";
            XmlAttribute descrAttr = attributes["description"];
            string description = descrAttr?.InnerText ?? "";
            AntTreeNode result;
            if (targetName == defaultTarget)
            {
                result = new AntTreeNode(targetName, ICON_PUBLIC_TARGET)
                {
                    NodeFont = new Font(tree.Font.Name, tree.Font.Size, FontStyle.Bold)
                };
            }
            else if (!string.IsNullOrEmpty(description)) result = new AntTreeNode(targetName, ICON_PUBLIC_TARGET);
            else result = new AntTreeNode(targetName, ICON_INTERNAL_TARGET);
            result.Target = targetName;
            result.ToolTipText = description;
            return result;
        }

        /// <summary>
        /// </summary>
        void RunSelectedTarget()
        {
            AntTreeNode node = tree.SelectedNode as AntTreeNode;
            if (node != null) pluginMain.RunTarget(node.File, node.Target);
        }

        /// <summary>
        /// </summary>
        void RemoveSelectedTarget()
        {
            TreeNode node = tree.SelectedNode;
            string file;
            if (node is AntTreeNode)
                file = ((AntTreeNode) node).File;
            else if (node != null) file = node.Text;
            else return;
            string text = $"\"{node.Text}\" will be removed from AntPanel.";
            if (MessageBox.Show(text, "Confirm", MessageBoxButtons.OKCancel) == DialogResult.OK)
                pluginMain.RemoveBuildFile(file);
        }

        /// <summary>
        /// </summary>
        void EditSelectedNode()
        {
            TreeNode node = tree.SelectedNode;
            if (node == null) return;
            string file;
            string target = null;
            if (node is AntTreeNode)
            {
                AntTreeNode antTreeNode = (AntTreeNode)node;
                file = antTreeNode.File;
                target = antTreeNode.Target;
            }
            else file = node.Text;
            PluginBase.MainForm.OpenEditableDocument(file, false);
            ScintillaControl sci = PluginBase.MainForm.CurrentDocument.SciControl;
            if (!string.IsNullOrEmpty(target))
            {
                Match match = Regex.Match(sci.Text, $"<target[^>]+name\\s*=\\s*\"{target}\".*>", RegexOptions.Compiled);
                if (!match.Success) return;
                int index = match.Index;
                sci.SetSel(index, index + match.Length);
            }
            else if (node is ErrorTreeNode)
            {
                ErrorTreeNode errorTreeNode = (ErrorTreeNode) node;
                int positionFromLine = sci.PositionFromLine(errorTreeNode.Line);
                positionFromLine += errorTreeNode.Position;
                sci.SetSel(positionFromLine, positionFromLine);
            }
        }

        void ShowContextMenu(TreeNode node, Point location)
        {
            if (node is ErrorTreeNode) errorMenu.Show(tree, location);
            else if (node.Parent == null) buildFileMenu.Show(tree, location);
            else targetMenu.Show(tree, location);
        }

        #region Event Handlers

        void OnAddClick(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "BuildFiles (*.xml)|*.XML|All files (*.*)|*.*",
                Multiselect = true
            };
            if (PluginBase.CurrentProject != null) dialog.InitialDirectory = Path.GetDirectoryName(PluginBase.CurrentProject.ProjectPath);
            if (dialog.ShowDialog() == DialogResult.OK) pluginMain.AddBuildFiles(dialog.FileNames);
        }

        void OnRemoveClick(object sender, EventArgs e) => RemoveSelectedTarget();

        void OnRunClick(object sender, EventArgs e) => RunSelectedTarget();

        void OnRefreshClick(object sender, EventArgs e) => RefreshData();

        void OnTreeNodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            TreeNode node = tree.GetNodeAt(e.Location);
            if (node == null) return;
            tree.SelectedNode = node;
            ShowContextMenu(node, e.Location);
        }

        void OnTreeNodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e) => RunSelectedTarget();

        void OnTreePreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == EDIT_KEYS || e.KeyCode == DEL_KEYS) e.IsInputKey = true;
        }

        void OnTreeKeyDown(object sender, KeyEventArgs e)
        {
            TreeNode node = tree.SelectedNode;
            if (node == null) return;
            switch (e.KeyCode)
            {
                case EDIT_KEYS:
                    EditSelectedNode();
                    break;
                case Keys.Enter:
                    RunSelectedTarget();
                    break;
                case Keys.Apps:
                    ShowContextMenu(node, node.Bounds.Location);
                    break;
                case DEL_KEYS:
                    if (node.ImageIndex == ICON_FILE || !(node is AntTreeNode)) RemoveSelectedTarget();
                    break;
                default: return;
            }
            e.Handled = true;
        }

        void OnTreeMouseDown(object sender, MouseEventArgs e)
        {
            int delta = (int)DateTime.Now.Subtract(lastMouseDown).TotalMilliseconds;
            preventExpand = delta < SystemInformation.DoubleClickTime;
            lastMouseDown = DateTime.Now;
        }

        void OnTreeBeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = preventExpand;
            preventExpand = false;
        }

        void OnTreeBeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = preventExpand;
            preventExpand = false;
        }

        void OnTreeDragEnter(object sender, DragEventArgs e)
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

        void OnTreeDragOver(object sender, DragEventArgs e)
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

        void OnTreeDragDrop(object sender, DragEventArgs e)
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

        void OnTreeItemDrag(object sender, ItemDragEventArgs e) => DoDragDrop(e.Item, DragDropEffects.Move);

        void OnMenuRunClick(object sender, EventArgs e) => RunSelectedTarget();

        void OnMenuEditClick(object sender, EventArgs e) => EditSelectedNode();

        void OnMenuRemoveClick(object sender, EventArgs e) => RemoveSelectedTarget();

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

    class ErrorTreeNode : TreeNode
    {
        public readonly int Line;
        public readonly int Position;
        public ErrorTreeNode(string text, int imageIndex, int line, int position) : base(text, imageIndex, imageIndex)
        {
            Line = line;
            Position = position;
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