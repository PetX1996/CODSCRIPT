using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

using System.Runtime.InteropServices;
using System.Security;

using CODSCRIPT;

namespace CODSCRIPTNpp
{
    #region NaturalStringComparer
    [SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogicalW(string psz1, string psz2);
    }

    public sealed class NaturalStringComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            return SafeNativeMethods.StrCmpLogicalW(a, b);
        }
    }
    #endregion

    enum AssemblyTreeType
    {
        None,
        Classic,
        Search,
        References
    };

    class AssemblyTreeView : TreeView
    {
        #region Type
        private AssemblyTreeType _type = AssemblyTreeType.None;
        public AssemblyTreeType Type 
        { 
            get { return _type; } 
            set { _lastType = _type; _type = value; } 
        }

        private AssemblyTreeType _lastType = AssemblyTreeType.None;
        public AssemblyTreeType LastType 
        { 
            get { return _lastType; } 
        }
        #endregion

        private AssemblyTreeFolder rootFolder;

        public AssemblyTreeView()
        {
            ImageList = AssemblyTreeIcons.ImageList;

            ClearNodes();

            BeforeExpand += new TreeViewCancelEventHandler(AssemblyTreeView_BeforeExpand);
            BeforeCollapse += new TreeViewCancelEventHandler(AssemblyTreeView_BeforeCollapse);
        }

        public void Add(AssemblyTreeFile file)
        {
            AssemblyTreeFolder curFolder = rootFolder;
            if (!String.IsNullOrEmpty(file.Path))
            {
                AssemblyTreeFolder parentFolder = rootFolder;

                string[] folders = file.Path.Split('\\');
                foreach (string folderName in folders)
                {
                    curFolder = parentFolder.FindFolder(folderName);
                    if (curFolder == null)
                    {
                        curFolder = new AssemblyTreeFolder(folderName);
                        parentFolder.Add(curFolder);
                    }

                    parentFolder = curFolder;
                }
            }

            curFolder.Add(file);
        }

        public AssemblyTreeFile FindFile(ScriptFile sf)
        {
            return rootFolder.FindFile(sf);
        }

        public void ClearNodes()
        {
            rootFolder = new AssemblyTreeFolder(true);
        }

        // adds only root folder content
        public void RefreshNodes()
        {
            this.BeginUpdate();

            // add root
            TreeNode[] nodes = rootFolder.Refresh();

            Nodes.Clear();
            foreach (TreeNode n in nodes)
                Nodes.Add(n);

            this.EndUpdate();
            //this.Refresh();

            RefreshSelectedNode(true);
        }

        public void RefreshSelectedNode(bool expand)
        {
            this.BeginUpdate();

            // reset last selected node
            if (_lastNode != null)
                _lastNode.BackColor = Color.Transparent;

            if (_lastFile != null)
                _lastFile.BackColor = Color.Transparent;

            // search or references
            if (expand && Type != AssemblyTreeType.Classic)
                ExpandAll();

            // select, expand and ensure visible selected node
            if (_currentFile != null)
            {
                if (expand && Type == AssemblyTreeType.Classic)
                {
                    //Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "======================");
                    ExpandNodeRecursive(_currentFile, _lastFile);
                    //Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "======================");
                    //Main.Trace.Flush();
                }

                _currentFile.BackColor = SelectedNodeColor;

                if (_currentNode != null)
                {
                    _currentNode.BackColor = SelectedNodeColor;

                    if (_currentNode.TreeView != null && _currentNode.NextNode != null)
                        _currentNode.NextNode.EnsureVisible();

                    _currentNode.EnsureVisible();
                }
            }

            this.EndUpdate();
            this.Refresh();
        }

        /// <summary>
        /// Expand node and all his parents and collapse lastNode and all his parents
        /// </summary>
        /// <param name="node">TreeNode to expand</param>
        /// <param name="lastNode">TreeNode to collapse</param>
        private void ExpandNodeRecursive(TreeNode curNode, TreeNode lastNode)
        {
            // DO THE MAGIC :)

            AssemblyTreeMember curMember = curNode as AssemblyTreeMember;
            AssemblyTreeMember lastMember = lastNode as AssemblyTreeMember;
            if (curMember != null && lastMember != null)
            {
                if (curMember != lastMember)
                    ExpandNodeRecursive(curMember.ParentFile, lastMember.ParentFile);

                return;
            }
            else if (curMember != null)
            {
                ExpandNodeRecursive(curMember.ParentFile, lastNode);
                return;
            }
            else if (lastMember != null)
            {
                ExpandNodeRecursive(curNode, lastMember.ParentFile);
                return;
            }

            AssemblyTreeFile curFile = curNode as AssemblyTreeFile;
            AssemblyTreeFile lastFile = lastNode as AssemblyTreeFile;
            if (curFile != null && lastFile != null)
            {
                if (curFile != lastFile)
                {
                    Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Collapsing " + lastFile.Text);
                    lastFile.Collapse();

                    ExpandNodeRecursive(curFile.ParentFolder, lastFile.ParentFolder);

                    Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Expanding " + curFile.Text);
                    curFile.Expand();
                }
                else
                {
                    if (!curFile.IsExpanded)
                    {
                        Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Expanding " + curFile.Text);
                        curFile.Expand();
                    }
                }
                return;
            }
            else if (curFile != null)
            {
                ExpandNodeRecursive(curFile.ParentFolder, lastNode);

                Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Expanding " + curFile.Text);
                curFile.Expand();
                return;
            }
            else if (lastFile != null)
            {
                Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Collapsing " + lastFile.Text);
                lastFile.Collapse();

                ExpandNodeRecursive(curNode, lastFile.ParentFolder);
                return;
            }

            AssemblyTreeFolder curFolder = curNode as AssemblyTreeFolder;
            AssemblyTreeFolder lastFolder = lastNode as AssemblyTreeFolder;

            if (curFolder != null && !curFolder.IsRootFolder
                && lastFolder != null && !lastFolder.IsRootFolder) // collapse last, expand current
            {
                //if (curFolder != lastFolder) // has not expanded yet
                //{
                    if (curFolder.Level > lastFolder.Level)
                    {
                        ExpandNodeRecursive(curFolder.ParentFolder, lastFolder);

                        if (!curFolder.IsExpanded)
                        {
                            Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Expanding " + curFolder.Text);
                            curFolder.Expand();
                        }
                    }
                    else if (curFolder.Level < lastFolder.Level)
                    {
                        Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Collapsing " + lastFolder.Text);
                        lastFolder.Collapse();

                        ExpandNodeRecursive(curFolder, lastFolder.ParentFolder);
                    }
                    else
                    {
                        if (curFolder != lastFolder)
                        {
                            Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Collapsing " + lastFolder.Text);
                            lastFolder.Collapse();
                        }

                        ExpandNodeRecursive(curFolder.ParentFolder, lastFolder.ParentFolder);

                        if (!curFolder.IsExpanded)
                        {
                            Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Expanding " + curFolder.Text);
                            curFolder.Expand();
                        }
                    }
                //}
                return;
            }
            else if (curFolder != null && !curFolder.IsRootFolder) // expand current
            {
                ExpandNodeRecursive(curFolder.ParentFolder, lastNode);

                Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Expanding " + curFolder.Text);
                curFolder.Expand();
                return;
            }
            else if (lastFolder != null && !lastFolder.IsRootFolder) // collapse last
            {
                Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Collapsing " + lastFolder.Text);
                lastFolder.Collapse();

                ExpandNodeRecursive(curNode, lastFolder.ParentFolder);
                return;
            }
        }

        public static Color SelectedNodeColor = Color.LightSkyBlue;
        private AssemblyTreeFile _currentFile;
        private AssemblyTreeFile _lastFile;

        private TreeNode _currentNode;
        private TreeNode _lastNode;
        public TreeNode CurrentNode
        {
            get { return _currentNode; }
            set 
            {
                _lastNode = _currentNode;
                _currentNode = value;

                _lastFile = _currentFile;
                if (_currentNode is AssemblyTreeFile)
                    _currentFile = (AssemblyTreeFile)_currentNode;
                else if (_currentNode is AssemblyTreeMember)
                    _currentFile = ((AssemblyTreeMember)_currentNode).ParentFile;
            }
        }

        #region Expanding and Collapsing - deleting nodes
        private void AssemblyTreeView_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            AssemblyTreeFolder folder = e.Node as AssemblyTreeFolder;
            if (folder != null)
            {
                folder.Refresh();
                return;
            }

            AssemblyTreeFile file = e.Node as AssemblyTreeFile;
            if (file != null)
            {
                file.Refresh();
                return;
            }
        }

        private void AssemblyTreeView_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            AssemblyTreeFolder folder = e.Node as AssemblyTreeFolder;
            if (folder != null)
            {
                folder.Nodes.Clear();
                if (folder.HasChildren)
                    folder.Nodes.Add("FakeNode");

                return;
            }

            AssemblyTreeFile file = e.Node as AssemblyTreeFile;
            if (file != null)
            {
                file.Nodes.Clear();
                if (file.HasChildren)
                    file.Nodes.Add("FakeNode");

                return;
            }
        }
        #endregion

        #region Disable DoubleClick expanding
        public override void Refresh()
        {
            btnDoubleClick = false; // it is necessary when it opens file with NodeDoubleClick event!!!
            base.Refresh();
        }

        private bool btnDoubleClick; // pozor na túto premennú!!! dokáže zkurviť všetko!!!
        protected override void OnBeforeCollapse(TreeViewCancelEventArgs e)
        {
            if (btnDoubleClick == true && e.Action == TreeViewAction.Collapse)
                e.Cancel = true;
            else
                base.OnBeforeCollapse(e);
        }

        protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
        {
            if (btnDoubleClick == true && e.Action == TreeViewAction.Expand)
                e.Cancel = true;
            else
                base.OnBeforeExpand(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Clicks > 1)
                btnDoubleClick = true;
            else
                btnDoubleClick = false;

            base.OnMouseDown(e);
        }

        protected override void OnNodeMouseDoubleClick(TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Bounds.Contains(e.Location))
                base.OnNodeMouseDoubleClick(e);

            btnDoubleClick = false;
        }
        #endregion
    }

    public class AssemblyTreeFolder : TreeNode
    {
        private List<TreeNode> _nodes;
        private List<AssemblyTreeFile> _globalFiles;
        private List<AssemblyTreeFolder> _folders;
        private List<AssemblyTreeFile> _files;

        public AssemblyTreeFolder ParentFolder { get; set; }

        public bool HasChildren { get { return _globalFiles.Count > 0 || _folders.Count > 0 || _files.Count > 0; } }

        public bool IsRootFolder { get; private set; }

        bool anyUpdate = true;

        public AssemblyTreeFolder(string folderName)
        {
            Name = folderName;
            Text = folderName;

            #region Image
            ImageIndex = AssemblyTreeIcons.FolderClosed;
            SelectedImageIndex = ImageIndex;
            #endregion

            _nodes = new List<TreeNode>();
            _globalFiles = new List<AssemblyTreeFile>();
            _folders = new List<AssemblyTreeFolder>();
            _files = new List<AssemblyTreeFile>();
        }

        public AssemblyTreeFolder(bool isRootFolder)
            : this("rootFolder")
        {
            IsRootFolder = true;
        }

        public AssemblyTreeFolder FindFolder(string folderName)
        {
            foreach (AssemblyTreeFolder f in _folders)
                if (f.Name == folderName)
                    return f;

            return null;
        }

        public AssemblyTreeFile FindFile(ScriptFile sf)
        {
            AssemblyTreeFile file = null;
            foreach (AssemblyTreeFile f in _globalFiles)
                if (f.SF == sf)
                    return f;

            foreach (AssemblyTreeFile f in _files)
                if (f.SF == sf)
                    return f;

            foreach (AssemblyTreeFolder f in _folders)
            {
                file = f.FindFile(sf);
                if (file != null)
                    return file;
            }

            return null;
        }

        public void Add(AssemblyTreeFolder folder)
        {
            _folders.Add(folder);

            folder.ParentFolder = this;

            anyUpdate = true;
        }

        public void Add(AssemblyTreeFile file)
        {
            if (file.SF.SI != null && file.SF.SI.IsGlobal)
                _globalFiles.Add(file);
            else
                _files.Add(file);

            file.ParentFolder = this;

            anyUpdate = true;
        }

        public void Remove(AssemblyTreeFile file)
        {
            int i = _files.IndexOf(file);
            if (i != -1)
                _files.RemoveAt(i);
            else
            {
                i = _globalFiles.IndexOf(file);
                if (i != -1)
                    _globalFiles.RemoveAt(i);
            }

            anyUpdate = true;
        }

        // Clear() nefunguje pri rootFolderi...WTF?!
        public TreeNode[] Refresh()
        {
            BuildNodes();

            if (!IsRootFolder)
                Nodes.Clear(); // z kolekcie sa nedajú odstraňovať elementy....!!!!

            TreeNode[] nodeArray = _nodes.ToArray();
            if (!IsRootFolder)
                Nodes.AddRange(nodeArray);

            return nodeArray;
        }

        private void BuildNodes()
        {
            if (!anyUpdate)
                return;

            _nodes.Clear();

            NaturalStringComparer c = new NaturalStringComparer();
            _globalFiles.Sort((a, b) => c.Compare(a.Name, b.Name));
            _folders.Sort((a, b) => c.Compare(a.Name, b.Name));
            _files.Sort((a, b) => c.Compare(a.Name + a.SF.FileExt.ToString(), b.Name + b.SF.FileExt.ToString())); // unexpected sorting -> added extension behind name

            foreach (AssemblyTreeFile f in _globalFiles)
            {
                _nodes.Add(f);
                if (f.HasChildren)
                    f.Nodes.Add("FakeNode");
            }

            foreach (AssemblyTreeFolder f in _folders)
            {
                _nodes.Add(f);
                if (f.HasChildren)
                    f.Nodes.Add("FakeNode");
            }

            foreach (AssemblyTreeFile f in _files)
            {
                _nodes.Add(f);
                if (f.HasChildren)
                    f.Nodes.Add("FakeNode");
            }

            anyUpdate = false;
        }
    }

    public class AssemblyTreeFile : TreeNode
    {
        public ScriptFile SF { get; private set; }
        public string Path { get; private set; }

        private List<TreeNode> _nodes;
        private List<AssemblyTreeMember> _members;
        private List<AssemblyTreeMember> _constants;
        private List<AssemblyTreeMember> _functions;
        private List<AssemblyTreeMember> _usings;

        public AssemblyTreeFolder ParentFolder { get; set; }

        public bool HasChildren { get { return _members.Count > 0 || _constants.Count > 0 || _functions.Count > 0 || _usings.Count > 0; } }

        bool anyUpdate = true;

        public AssemblyTreeFile(ScriptFile sf)
        {
            SF = sf;

            if (sf.SecondSI != null)
                Name = sf.SecondSI.Name;
            else
                Name = sf.SFPath;

            #region ToolTipText
            StringBuilder sb = new StringBuilder();
            foreach (Error e in sf.Errors)
                sb.Append(e.FullMessage + Environment.NewLine);

            ToolTipText = sb.ToString().TrimEnd();
            #endregion

            #region Image
            if (sf.Errors.AnyErrors)
                ImageIndex = AssemblyTreeIcons.FileError;
            else if (sf.Errors.AnyWarnings)
                ImageIndex = AssemblyTreeIcons.FileWarning;
            else if (sf.SI != null && sf.SI.IsGlobal)
                ImageIndex = AssemblyTreeIcons.FileGlobal;
            else if (sf.IsExtern)
                ImageIndex = AssemblyTreeIcons.FileExtern;
            else if (sf.FileExt == ScriptFile.Extension.GSX)
                ImageIndex = AssemblyTreeIcons.FileGSX;
            else
                ImageIndex = AssemblyTreeIcons.File;

            SelectedImageIndex = ImageIndex;
            #endregion

            int lastSep = Name.LastIndexOf('\\');
            if (lastSep != -1)
            {
                Path = Name.Substring(0, lastSep);
                Name = Name.Substring(lastSep + 1);
            }

            this.Text = this.Name;

            this.ContextMenuStrip = new FileContextMenuStrip(this, sf);

            _nodes = new List<TreeNode>();
            _members = new List<AssemblyTreeMember>();
            _constants = new List<AssemblyTreeMember>();
            _functions = new List<AssemblyTreeMember>();
            _usings = new List<AssemblyTreeMember>();
        }

        public void Add(AssemblyTreeMember member)
        {
            if (member.Member is ConstInfo)
                _constants.Add(member);
            else if (member.Member is FuncInfo)
                _functions.Add(member);
            else if (member.Member is UsingInfo)
                _usings.Add(member);
            else
                _members.Add(member);

            member.ParentFile = this;

            anyUpdate = true;
        }

        public AssemblyTreeMember Find(IMemberInfo memberInfo)
        {
            if (memberInfo is ConstInfo)
                return _constants.Find(a => ((IMemberInfo)a.Member).Compare(memberInfo));
            else if (memberInfo is FuncInfo)
                return _functions.Find(a => ((IMemberInfo)a.Member).Compare(memberInfo));
            else if (memberInfo is UsingInfo)
                return _usings.Find(a => ((IMemberInfo)a.Member).Compare(memberInfo));
            else
                throw new ArgumentException("memberInfo");
        }

        public AssemblyTreeMember Find(IReferenceInfo refInfo)
        {
            return _members.Find(a => ((IReferenceInfo)a.Member).Compare(refInfo));
        }

        public void Refresh()
        {
            BuildNodes();

            Nodes.Clear();
            Nodes.AddRange(_nodes.ToArray());
        }

        private void BuildNodes()
        {
            if (!anyUpdate)
                return;

            _nodes.Clear();

            NaturalStringComparer c = new NaturalStringComparer();
            _members.Sort((a, b) => c.Compare(a.Name, b.Name));
            _usings.Sort((a, b) => c.Compare(a.Name, b.Name));
            _constants.Sort((a, b) => c.Compare(a.Name, b.Name));
            _functions.Sort((a, b) => c.Compare(a.Name, b.Name));

            foreach (AssemblyTreeMember f in _members)
                _nodes.Add(f);

            foreach (AssemblyTreeMember f in _usings)
                _nodes.Add(f);

            foreach (AssemblyTreeMember f in _constants)
                _nodes.Add(f);

            foreach (AssemblyTreeMember f in _functions)
                _nodes.Add(f);

            anyUpdate = false;
        }
    }

    public class AssemblyTreeMember : TreeNode
    {
        public AssemblyTreeFile ParentFile { get; set; }

        private IMemberInfo memberInfo;
        private IReferenceInfo referenceInfo;
        public object Member 
        {
            get { return memberInfo != null ? (object)memberInfo : (object)referenceInfo; }
        }

        private AssemblyTreeMember()
        { 
            
        }

        public AssemblyTreeMember(IMemberInfo member)
            : this()
        {
            memberInfo = member;

            Name = member.Name;
            Text = Name;

            #region ToolTipText
            if (member is FuncInfo)
                ToolTipText = FileManager.GetCallTipText((FuncInfo)member, ((FuncInfo)member).Parameters.Count);
            else if (member is ConstInfo)
                ToolTipText = FileManager.GetCallTipText((ConstInfo)member);
            else if (member is UsingInfo)
                ToolTipText = FileManager.GetCallTipText((UsingInfo)member);
            else
                ToolTipText = Name;
            #endregion

            #region Image
            if (member is ConstInfo)
            {
                ConstInfo c = (ConstInfo)member;
                if (c.Sealed)
                    ImageIndex = AssemblyTreeIcons.ConstantSealed;
                else if (c.Access == MemberAccess.Private)
                    ImageIndex = AssemblyTreeIcons.ConstantPrivate;
                else if (c.Access == MemberAccess.Public)
                    ImageIndex = AssemblyTreeIcons.ConstantPublic;
                else
                    throw new ArgumentException("Unknown icon");
            }
            else if (member is FuncInfo)
            {
                FuncInfo f = (FuncInfo)member;
                if (f.Access == MemberAccess.Private)
                    ImageIndex = AssemblyTreeIcons.FunctionPrivate;
                else if (f.Access == MemberAccess.Public)
                    ImageIndex = AssemblyTreeIcons.FunctionPublic;
                else
                    throw new ArgumentException("Unknown icon");
            }
            else if (member is UsingInfo)
            {
                UsingInfo u = (UsingInfo)member;
                if (u.Access == MemberAccess.Private)
                    ImageIndex = AssemblyTreeIcons.UsingPrivate;
                else if (u.Access == MemberAccess.Public)
                    ImageIndex = AssemblyTreeIcons.UsingPublic;
                else
                    throw new ArgumentException("Unknown icon");
            }
            else
                throw new ArgumentException("member");

            SelectedImageIndex = ImageIndex;
            #endregion

            this.ContextMenuStrip = new MemberContextMenuStrip(this, member);
        }

        public AssemblyTreeMember(IReferenceInfo reference)
            : this()
        {
            referenceInfo = reference;

            if (reference.Definition != null)
                Name = reference.Definition.Name;
            else
                Name = "Unknown member in " + reference.DefinitionSF.ToString();

            Text = Name;

            #region ToolTipText
            ToolTipText = reference.CodePart;
            #endregion

            #region Image
            ImageIndex = AssemblyTreeIcons.Reference;
            SelectedImageIndex = ImageIndex;
            #endregion

            this.ContextMenuStrip = new ReferenceContextMenuStrip(this, reference);
        }
    }

    #region Context Menus
    class FileContextMenuStrip : ContextMenuStrip
    {
        private AssemblyTreeFile _file;
        private ScriptFile _sf;

        public FileContextMenuStrip(AssemblyTreeFile file, ScriptFile sf)
        {
            _file = file;
            _sf = sf;

            ToolStripMenuItem fileOpen = new ToolStripMenuItem("Open File");
            fileOpen.Click += new EventHandler(fileOpen_Click);

            ToolStripMenuItem fileCopy = new ToolStripMenuItem("Copy");
            fileCopy.Click += new EventHandler(fileCopy_Click);

            this.Items.AddRange(new ToolStripMenuItem[] { fileOpen, fileCopy });
        }

        private void fileOpen_Click(object sender, EventArgs e)
        {
            Main.MainDlg.OpenFile(_file);
        }

        private void fileCopy_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("Copy " + _sf.SFPath);
            Clipboard.SetText(_sf.SFPath);
        }
    }

    class MemberContextMenuStrip : ContextMenuStrip
    {
        private AssemblyTreeMember _member;
        private IMemberInfo _memberInfo;

        public MemberContextMenuStrip(AssemblyTreeMember member, IMemberInfo memberInfo)
        {
            _member = member;
            _memberInfo = memberInfo;

            ToolStripMenuItem memberGoToDefinition = new ToolStripMenuItem("Go To Definition");
            memberGoToDefinition.Click += new EventHandler(memberGoToDefinition_Click);

            ToolStripMenuItem memberFindAllReferences = new ToolStripMenuItem("Find All References");
            memberFindAllReferences.Click += new EventHandler(memberFindAllReferences_Click);

            ToolStripMenuItem memberCopy = new ToolStripMenuItem("Copy");
            memberCopy.Click += new EventHandler(memberCopy_Click);

            this.Items.AddRange(new ToolStripMenuItem[] { memberGoToDefinition, memberFindAllReferences, memberCopy });
        }

        private void memberGoToDefinition_Click(object sender, EventArgs e)
        {
            Main.MainDlg.OpenMember(_member);
        }

        private void memberFindAllReferences_Click(object sender, EventArgs e)
        {
            Main.Manager.FindAllReferences(_memberInfo);
        }

        private void memberCopy_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("Copy " + _memberInfo.SF.SFPath + "::" + _memberInfo.Name);
            string copiedText;
            if (_memberInfo is UsingInfo)
                copiedText = ((UsingInfo)_memberInfo).SFPath;
            else
                copiedText = _memberInfo.SF.SFPath + "::" + _memberInfo.Name;

            Clipboard.SetText(copiedText, TextDataFormat.Text);
        }
    }

    class ReferenceContextMenuStrip : ContextMenuStrip
    {
        private AssemblyTreeMember _member;
        private IReferenceInfo _refInfo;

        public ReferenceContextMenuStrip(AssemblyTreeMember member, IReferenceInfo refInfo)
        {
            _member = member;
            _refInfo = refInfo;

            ToolStripMenuItem refGoToReference = new ToolStripMenuItem("Go To Reference");
            refGoToReference.Click += new EventHandler(refGoToReference_Click);

            ToolStripMenuItem refGoToDefinition = new ToolStripMenuItem("Go To Definition");
            refGoToDefinition.Click += new EventHandler(refGoToDefinition_Click);

            this.Items.AddRange(new ToolStripMenuItem[] { refGoToReference, refGoToDefinition });
        }

        private void refGoToReference_Click(object sender, EventArgs e)
        {
            Main.MainDlg.OpenMember(_member);
        }

        private void refGoToDefinition_Click(object sender, EventArgs e)
        {
            if (_refInfo.Definition == null)
                MessageBox.Show("Unknown member in " + _refInfo.DefinitionSF.ToString());
            else
                FileManager.GoToDefinition(_refInfo.Definition);
        }
    }
    #endregion

    class AssemblyTreeIcons
    {
        static ImageList list = new ImageList();
        public static ImageList ImageList { get { return list; } }

        private int index;
        private AssemblyTreeIcons(string fileName)
        {
            index = list.Images.Count;
            try
            {
                list.Images.Add(Image.FromFile(Path.Combine(Main.PluginSubFolder, fileName)));
            }
            catch
            {
                MessageBox.Show("Could not find image " + fileName);
            }
        }

        public static AssemblyTreeIcons FolderClosed = new AssemblyTreeIcons("folder_closed.png");
        public static AssemblyTreeIcons FolderOpen = new AssemblyTreeIcons("folder_open.png");

        public static AssemblyTreeIcons File = new AssemblyTreeIcons("file.png");
        public static AssemblyTreeIcons FileGSX = new AssemblyTreeIcons("file_gsx.png");
        public static AssemblyTreeIcons FileExtern = new AssemblyTreeIcons("file_extern.png");
        public static AssemblyTreeIcons FileGlobal = new AssemblyTreeIcons("file_global.png");
        public static AssemblyTreeIcons FileError = new AssemblyTreeIcons("file_error.png");
        public static AssemblyTreeIcons FileWarning = new AssemblyTreeIcons("file_warning.png");

        public static AssemblyTreeIcons ConstantPublic = new AssemblyTreeIcons("member_constant.png");
        public static AssemblyTreeIcons ConstantPrivate = new AssemblyTreeIcons("member_constant_private.png");
        public static AssemblyTreeIcons ConstantSealed = new AssemblyTreeIcons("member_constant_sealed.png");
        
        public static AssemblyTreeIcons FunctionPublic = new AssemblyTreeIcons("member_function.png");
        public static AssemblyTreeIcons FunctionPrivate = new AssemblyTreeIcons("member_function_private.png");

        public static AssemblyTreeIcons UsingPublic = new AssemblyTreeIcons("member_using.png");
        public static AssemblyTreeIcons UsingPrivate = new AssemblyTreeIcons("member_using_private.png");

        public static AssemblyTreeIcons Variable = new AssemblyTreeIcons("member_variable.png");

        public static AssemblyTreeIcons Reference = new AssemblyTreeIcons("member_reference.png");

        public static implicit operator int(AssemblyTreeIcons icon)
        {
            return icon.index;
        }
    }
}
