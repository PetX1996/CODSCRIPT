using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CODSCRIPT;
using NppPluginNET;
using System.Windows.Threading;
using System.IO;

namespace CODSCRIPTNpp
{
    public partial class MainDlg : Form
    {
        private FileManager fileManager;

        public MainDlg()
        {
            InitializeComponent();
            BuildSettingsTab();
            BuildCODSCRIPTTab();
        }

        #region Setting Tab
        private void BuildSettingsTab()
        { 
            // target platform
            string[] platforms = Enum.GetNames(typeof(TargetPlatform));
            this.TargetPlatformComboBox.Items.AddRange(platforms);

            // target configuration
            string[] configurations = Enum.GetNames(typeof(TargetConfiguration));
            this.TargetConfigurationComboBox.Items.AddRange(configurations);
        }

        bool settingsUpdated = true;
        private void UpdateSettingsTab()
        {
            if (!settingsUpdated)
                return;

            this.TargetPlatformComboBox.SelectedItem = this.fileManager.ScriptManager.Settings.TargetPlatform.ToString();

            this.TargetConfigurationComboBox.SelectedItem = this.fileManager.ScriptManager.Settings.TargetConfiguration.ToString();

            this.VersionStrTextBox.Text = this.fileManager.ScriptManager.Settings.GetOrigVersionStr();
            this.VersionIntNumericUpDown.Value = this.fileManager.ScriptManager.Settings.GetOrigVersionInt();

            this.IsVersionIntFromDateCheckBox.Checked = this.fileManager.ScriptManager.Settings.IsVersionIntFromDate;

            settingsUpdated = false;
        }

        private void TargetPlatformComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.fileManager == null)
                return;

            string selected = TargetPlatformComboBox.SelectedItem.ToString();
            this.fileManager.ScriptManager.Settings.TargetPlatform = (TargetPlatform)Enum.Parse(typeof(TargetPlatform), selected);
        }

        private void TargetConfigurationComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.fileManager == null)
                return;

            string selected = TargetConfigurationComboBox.SelectedItem.ToString();
            this.fileManager.ScriptManager.Settings.TargetConfiguration = (TargetConfiguration)Enum.Parse(typeof(TargetConfiguration), selected);
        }

        private void VersionStrTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (this.fileManager == null || e.KeyChar != 13)
                return;

            this.fileManager.ScriptManager.Settings.VersionStr = this.VersionStrTextBox.Text;
        }

        private void VersionIntNumericUpDown_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (this.fileManager == null || e.KeyChar != 13)
                return;

            this.fileManager.ScriptManager.Settings.VersionInt = (int)this.VersionIntNumericUpDown.Value;
        }

        private void IsVersionIntFromDateCheckBox_Click(object sender, EventArgs e)
        {
            if (this.fileManager == null)
                return;

            this.fileManager.ScriptManager.Settings.IsVersionIntFromDate = this.IsVersionIntFromDateCheckBox.Checked;
        }
        #endregion

        #region CODSCRIPT TABPAGE
        private const string workingDirComboBoxNew = "* NEW *";
        private const string settingsDirComboBoxNew = "* NEW *";
        private void BuildCODSCRIPTTab()
        {
            if (Main.WorkingDirList != null)
                this.WorkingDirComboBox.Items.AddRange(Main.WorkingDirList.ToArray());

            this.WorkingDirComboBox.SelectedItem = Main.workingDir;
            this.WorkingDirComboBox.Items.Add(workingDirComboBoxNew);


            if (Main.SettingsFileList != null)
                this.SettingsFileComboBox.Items.AddRange(Main.SettingsFileList.ToArray());

            this.SettingsFileComboBox.SelectedItem = Main.SettingsFile;
            this.SettingsFileComboBox.Items.Add(settingsDirComboBoxNew);


            this.SaveErrorCheckBox.Checked = Main.Save_ShowErrorMsgBox;
        }

        private void WorkingDirComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            string cur = this.WorkingDirComboBox.SelectedItem.ToString();
            if (cur == workingDirComboBoxNew)
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                dialog.SelectedPath = Main.workingDir;
                DialogResult result = dialog.ShowDialog();
                if ((result == System.Windows.Forms.DialogResult.OK
                    || result == System.Windows.Forms.DialogResult.Yes)
                    && Directory.Exists(dialog.SelectedPath)
                    && File.Exists(Path.Combine(dialog.SelectedPath, "iw3mp.exe")))
                {
                    Main.WorkingDirList.Add(dialog.SelectedPath);
                    this.WorkingDirComboBox.SelectedItem = dialog.SelectedPath;
                }
                else
                {
                    this.WorkingDirComboBox.SelectedItem = Main.workingDir;
                    MessageBox.Show("Could not add new workingDir");
                    return;
                }
            }

            Main.workingDir = this.WorkingDirComboBox.SelectedItem.ToString();
            Main.UpdateSettingsInFile();
            Environment.Exit(0);
        }

        private void SettingsFileComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            string cur = this.SettingsFileComboBox.SelectedItem.ToString();
            if (cur == settingsDirComboBoxNew)
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.FileName = Main.SettingsFile;
                dialog.Filter = "XML|*.xml";
                DialogResult result = dialog.ShowDialog();
                if ((result == System.Windows.Forms.DialogResult.OK
                    || result == System.Windows.Forms.DialogResult.Yes)
                    && File.Exists(dialog.FileName))
                {
                    Main.SettingsFileList.Add(dialog.FileName);
                    this.SettingsFileComboBox.SelectedItem = dialog.FileName;
                }
                else
                {
                    this.SettingsFileComboBox.SelectedItem = Main.SettingsFile;
                    MessageBox.Show("Could not add new settingsFile");
                    return;
                }
            }

            Main.SettingsFile = this.SettingsFileComboBox.SelectedItem.ToString();
            Main.UpdateSettingsInFile();
            Environment.Exit(0);
        }

        private void SaveErrorCheckBox_Click(object sender, EventArgs e)
        {
            Main.Save_ShowErrorMsgBox = this.SaveErrorCheckBox.Checked;
        }
        #endregion

        internal void UpdateInfo(FileManager fileManager)
        {
            this.fileManager = fileManager;
            UpdateSettingsTab();

            UpdateErrorList();
            UpdateAssemblyTree(false);
        }

        #region AssemblyTree
        const string referencesText = "Found references list.";

        private void UpdateAssemblyTree(bool changedControlText)
        {
            if (this.fileManager == null || this.fileManager.CurrentFile == null)
                return;
            
            if (searchTextBox.Text == referencesText)
                assemblyTreeView.Type = AssemblyTreeType.References;
            else if (!String.IsNullOrEmpty(searchTextBox.Text))
                assemblyTreeView.Type = AssemblyTreeType.Search;
            else
                assemblyTreeView.Type = AssemblyTreeType.Classic;

            if (assemblyTreeView.Type == AssemblyTreeType.Search)
                UpdateSearchResults(changedControlText);
            else if (assemblyTreeView.Type == AssemblyTreeType.Classic)
                UpdateClassicTree();
        }

        public void UpdateAssemblyTree(List<IReferenceInfo> references, IMemberInfo definition)
        {
            this.assemblyTreeView.Type = AssemblyTreeType.References;
            this.searchTextBox.Text = referencesText;

            this.assemblyTreeView.ClearNodes();

            #region Add definition to tree
            AssemblyTreeFile file = this.assemblyTreeView.FindFile(definition.SF);
            if (file == null)
            {
                file = new AssemblyTreeFile(definition.SF);
                this.assemblyTreeView.Add(file);              
            }
            file.Add(new AssemblyTreeMember(definition));
            #endregion

            #region Add references to tree
            foreach (IReferenceInfo refI in references)
            {
                file = this.assemblyTreeView.FindFile(refI.SF);
                if (file == null)
                {
                    file = new AssemblyTreeFile(refI.SF);
                    this.assemblyTreeView.Add(file);
                }

                file.Add(new AssemblyTreeMember(refI));
            }
            #endregion

            this.assemblyTreeView.CurrentNode = null;
            this.assemblyTreeView.RefreshNodes();
        }

        private void UpdateSearchResults(bool changedControlText)
        {
            string searchText = searchTextBox.Text.ToUpper();

            if (!changedControlText && this.assemblyTreeView.Type == assemblyTreeView.LastType)
                return;

            this.assemblyTreeView.ClearNodes();

            foreach (ScriptFile sf in this.fileManager.ScriptManager.GetAllSFs())
            {
                AssemblyTreeFile file = new AssemblyTreeFile(sf);
                bool isEmpty = true;

                if (sf.SecondSI != null)
                {
                    foreach (UsingInfo u in sf.SecondSI.Usings)
                    {
                        if (u.Name.ToUpper().Contains(searchText))
                        {
                            file.Add(new AssemblyTreeMember(u));
                            isEmpty = false;
                        }
                    }

                    foreach (ConstInfo c in sf.SecondSI.Constants)
                    {
                        if (c.Name.ToUpper().Contains(searchText))
                        {
                            file.Add(new AssemblyTreeMember(c));
                            isEmpty = false;
                        }
                    }

                    foreach (FuncInfo f in sf.SecondSI.Functions)
                    {
                        if (f.Name.ToUpper().Contains(searchText))
                        {
                            file.Add(new AssemblyTreeMember(f));
                            isEmpty = false;
                        }
                    }
                }

                if (!isEmpty)
                    this.assemblyTreeView.Add(file);
            }

            this.assemblyTreeView.CurrentNode = null;
            this.assemblyTreeView.RefreshNodes();
        }

        private void UpdateClassicTree()
        {
            try
            {
                // update complete tree
                if (assemblyTreeView.LastType != AssemblyTreeType.Classic)
                {
                    this.assemblyTreeView.ClearNodes();
                    AssemblyTreeFile curFile = null;

                    foreach (ScriptFile sf in this.fileManager.ScriptManager.GetAllSFs())
                    {
                        AssemblyTreeFile file = new AssemblyTreeFile(sf);

                        if (sf == this.fileManager.CurrentFile)
                            curFile = file;

                        if (sf.SecondSI != null)
                        {
                            foreach (UsingInfo u in sf.SecondSI.Usings)
                                file.Add(new AssemblyTreeMember(u));

                            foreach (ConstInfo c in sf.SecondSI.Constants)
                                file.Add(new AssemblyTreeMember(c));

                            foreach (FuncInfo f in sf.SecondSI.Functions)
                                file.Add(new AssemblyTreeMember(f));
                        }

                        this.assemblyTreeView.Add(file);
                    }

                    this.assemblyTreeView.CurrentNode = curFile;
                    this.assemblyTreeView.RefreshNodes();
                }
                else // update only changed sfs
                {
                    // TODO: update informácií v strome po aktualizácii

                    //this.assemblyTreeView.ClearNodes();
                    AssemblyTreeFile curFile = null;
                    List<ScriptFile> updatedFiles = this.fileManager.ThreadedSM.CurrentUpdatedFiles.Clear();

                    foreach (ScriptFile sf in updatedFiles)
                    {
                        Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "TREE - Updating file " + sf.ToString());

                        AssemblyTreeFile file = this.assemblyTreeView.FindFile(sf);
                        if (file != null)
                        {
                            AssemblyTreeFolder folder = file.ParentFolder;
                            folder.Remove(file);
                            file = new AssemblyTreeFile(sf);
                            folder.Add(file);
                        }
                        else
                        {
                            file = new AssemblyTreeFile(sf);
                            this.assemblyTreeView.Add(file);
                        }

                        if (sf == this.fileManager.CurrentFile)
                            curFile = file;

                        if (sf.SecondSI != null)
                        {
                            foreach (UsingInfo u in sf.SecondSI.Usings)
                                file.Add(new AssemblyTreeMember(u));

                            foreach (ConstInfo c in sf.SecondSI.Constants)
                                file.Add(new AssemblyTreeMember(c));

                            foreach (FuncInfo f in sf.SecondSI.Functions)
                                file.Add(new AssemblyTreeMember(f));
                        }
                    }

                    this.assemblyTreeView.CurrentNode = curFile;
                    this.assemblyTreeView.RefreshNodes();
                }
            }
            catch (Exception e)
            {
                Main.NotifyError(e);
            }
        }

        private void searchTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            try
            {
                if (e.KeyChar == 13)
                    UpdateAssemblyTree(true);
            }
            catch (Exception ex)
            {
                Main.NotifyError(ex);
            }
        }

        private void assemblyTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            try
            {
                if (e.Node is AssemblyTreeFile)
                {
                    OpenFile((AssemblyTreeFile)e.Node);
                }
                else if (e.Node is AssemblyTreeMember)
                {
                    OpenMember((AssemblyTreeMember)e.Node);
                
                }
            }
            catch (Exception ex)
            {
                Main.NotifyError(ex);
            }
        }

        public void OpenFile(AssemblyTreeFile fileNode)
        {
            if (!String.IsNullOrEmpty(fileNode.SF.SFFullPath))
            {
                Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DOOPEN, 0, fileNode.SF.SFFullPath);
                Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GRABFOCUS, 0, 0);
            }
            else
                MessageBox.Show("Cannot open file '" + fileNode.SF.SFPath + "'");
        }

        public void OpenMember(AssemblyTreeMember member)
        {
            if (member.Member is IMemberInfo)
            {
                IMemberInfo memberInfo = (IMemberInfo)member.Member;
                FileManager.GoToDefinition(memberInfo);

                // TODO: remove threading!

                if (memberInfo.SF == this.fileManager.CurrentFile)
                {
                    this.assemblyTreeView.CurrentNode = member;
                    this.assemblyTreeView.RefreshSelectedNode(false);
                }
            }
            else if (member.Member is IReferenceInfo)
            {
                IReferenceInfo referenceInfo = (IReferenceInfo)member.Member;
                FileManager.GoToPosition(referenceInfo.SF, referenceInfo.CharIndex, referenceInfo.CharLength, referenceInfo);

                if (referenceInfo.SF == this.fileManager.CurrentFile)
                {
                    this.assemblyTreeView.CurrentNode = member;
                    this.assemblyTreeView.RefreshSelectedNode(false);
                }
            }
        }

        public void SelectMemberInTree(IMemberInfo member)
        {
            AssemblyTreeFile file = this.assemblyTreeView.FindFile(member.SF);
            if (file != null)
            {
                AssemblyTreeMember m = file.Find(member);

                this.assemblyTreeView.CurrentNode = m;
                this.assemblyTreeView.RefreshSelectedNode(false);
            }
        }

        public void SelectMemberInTree(IReferenceInfo refInfo)
        {
            AssemblyTreeFile file = this.assemblyTreeView.FindFile(refInfo.SF);
            if (file != null)
            {
                AssemblyTreeMember m = file.Find(refInfo);

                this.assemblyTreeView.CurrentNode = m;
                this.assemblyTreeView.RefreshSelectedNode(false);
            }
        }
        #endregion

        #region ERROR LIST
        bool lastCurrentOnly = true;
        private void UpdateErrorList()
        {
            if (this.fileManager == null || this.fileManager.CurrentFile == null)
                return;

            #region Get Error List
            bool allowErrors = this.checkBoxErrors.Checked;
            bool allowWarnings = this.checkBoxWarnings.Checked;
            bool currentOnly = this.checkBoxCurrentOnly.Checked;

            if (!currentOnly && !lastCurrentOnly)
                return;

            lastCurrentOnly = currentOnly;

            ErrorCollection sourceErrors = null;
            ErrorCollection outputErrors = null;
            if (currentOnly)
                sourceErrors = fileManager.CurrentFile.Errors;
            else
            {
                sourceErrors = new ErrorCollection();
                foreach (ScriptFile sf in this.fileManager.ScriptManager.GetAllSFs())
                    sourceErrors.AddRange(sf.Errors);
            }

            if (allowErrors && allowWarnings)
                outputErrors = sourceErrors;
            else if (allowErrors)
                outputErrors = sourceErrors.GetOnlyErrors();
            else
                outputErrors = sourceErrors.GetOnlyWarnings();
            #endregion

            #region Parse Error List to GridView
            this.errorsGridView.Rows.Clear();

            foreach (Error e in outputErrors)
            {
                DataGridViewRow row = new DataGridViewRow();
                row.CreateCells(this.errorsGridView, e.Message);
                row.Cells[0].ToolTipText = e.FullMessage;
                row.Tag = e;

                this.errorsGridView.Rows.Add(row);
            }
            #endregion
        }

        private void checkBoxErrors_CheckedChanged(object sender, EventArgs e)
        {
            UpdateErrorList();
        }

        private void errorsGridView_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            DataGridViewRow row = this.errorsGridView.Rows[e.RowIndex];
            Error error = (Error)row.Tag;
            //MessageBox.Show(error.FullMessage);
            FileManager.GoToPosition(error.ErrorInfo.SF, error.ErrorInfo.CurCharIndex, error.ErrorInfo.CurCharLength);
        }
        #endregion
    }
}
