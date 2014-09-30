using PluginCore;
using ScintillaNet;
using System;
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
            add.Image = PluginBase.MainForm.FindImage("33");
            run.Image = PluginBase.MainForm.FindImage("487");
            refresh.Image = PluginBase.MainForm.FindImage("66");
            CreateMenus();
            RefreshData();
        }

        public void RunTarget()
        {
            AntTreeNode node = tree.SelectedNode as AntTreeNode;
            if (node == null) return;
            pluginMain.RunTarget(node.File, node.Target);
        }

        public void RefreshData()
        {
            Enabled = (PluginBase.CurrentProject != null);
            if (Enabled) FillTree();
            else
            {
                tree.Nodes.Clear();
                tree.Nodes.Add(new TreeNode("No project opened"));
            }
        }

        private void CreateMenus()
        {
            buildFileMenu = new ContextMenuStrip();
            buildFileMenu.Items.Add("Run default target", run.Image, MenuRunClick);
            buildFileMenu.Items.Add("Edit file", null, MenuEditClick);
            buildFileMenu.Items.Add(new ToolStripSeparator());
            buildFileMenu.Items.Add("Remove", PluginBase.MainForm.FindImage("153"), MenuRemoveClick);
            targetMenu = new ContextMenuStrip();
            targetMenu.Items.Add("Run target", run.Image, MenuRunClick);
            targetMenu.Items.Add("Show in Editor", null, MenuEditClick);
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

        #region Event Handlers

        private void RefreshClick(object sender, EventArgs e)
        {
            RefreshData();
        }

        private void AddClick(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "BuildFiles (*.xml)|*.XML|" + "All files (*.*)|*.*";
            dialog.Multiselect = true;
            if (PluginBase.CurrentProject != null) dialog.InitialDirectory = Path.GetDirectoryName(PluginBase.CurrentProject.ProjectPath);
            if (dialog.ShowDialog() == DialogResult.OK) pluginMain.AddBuildFiles(dialog.FileNames);
        }

        private void RunClick(object sender, EventArgs e)
        {
            RunTarget();
        }

        private void TreeNodeKeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case (char)Keys.Enter:
                    e.Handled = true;
                    RunTarget();
                    break;
            }
        }

        private void TreeNodeKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Apps:
                    e.Handled = true;
                    TreeNode selectedNode = tree.SelectedNode;
                    if (selectedNode != null)
                    {
                        if (selectedNode.Parent == null) buildFileMenu.Show(tree, selectedNode.Bounds.Location);
                        else targetMenu.Show(tree, selectedNode.Bounds.Location);
                    }
                    break;
            }
        }

        private void TreeNodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                AntTreeNode currentNode = tree.GetNodeAt(e.Location) as AntTreeNode;
                tree.SelectedNode = currentNode;
                if (currentNode.Parent == null) buildFileMenu.Show(tree, e.Location);
                else targetMenu.Show(tree, e.Location);
            }
        }

        private void TreeNodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            RunTarget();
        }

        private void TreeKeyDown(object sender, KeyEventArgs e)
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
            }
        }

        private void MenuRunClick(object sender, EventArgs e)
        {
            RunTarget();
        }

        private void MenuEditClick(object sender, EventArgs e)
        {
            AntTreeNode node = tree.SelectedNode as AntTreeNode;
            PluginBase.MainForm.OpenEditableDocument(node.File, false);
            ScintillaControl sci = PluginBase.MainForm.CurrentDocument.SciControl;
            Match match = Regex.Match(sci.Text, "<target[^>]+name\\s*=\\s*\"" + node.Target + "\".*>", RegexOptions.Compiled);
            if (match.Success)
            {
                sci.GotoPos(match.Index);
                sci.SetSel(match.Index, match.Index + match.Length);
            }
        }

        private void MenuRemoveClick(object sender, EventArgs e)
        {
            pluginMain.RemoveBuildFile((tree.SelectedNode as AntTreeNode).File);
        }

        #endregion
    }

    internal class AntTreeNode : TreeNode
    {
        public string File;
        public string Target;

        public AntTreeNode(string text, int imageIndex)
            : base(text, imageIndex, imageIndex)
        {
        }
    }
}