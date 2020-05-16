namespace CODSCRIPTNpp
{
    partial class MainDlg
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.searchTextBox = new System.Windows.Forms.TextBox();
            this.assemblyTreeView = new CODSCRIPTNpp.AssemblyTreeView();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.ErrorPage = new System.Windows.Forms.TabPage();
            this.checkBoxCurrentOnly = new System.Windows.Forms.CheckBox();
            this.checkBoxWarnings = new System.Windows.Forms.CheckBox();
            this.checkBoxErrors = new System.Windows.Forms.CheckBox();
            this.errorsGridView = new System.Windows.Forms.DataGridView();
            this.Description = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SettingsTabPage = new System.Windows.Forms.TabPage();
            this.IsVersionIntFromDateCheckBox = new System.Windows.Forms.CheckBox();
            this.VersionIntNumericUpDown = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.VersionStrTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.TargetConfigurationComboBox = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.TargetPlatformComboBox = new System.Windows.Forms.ComboBox();
            this.CODSCRIPTTabPage = new System.Windows.Forms.TabPage();
            this.SaveErrorCheckBox = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.WorkingDirComboBox = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.SettingsFileComboBox = new System.Windows.Forms.ComboBox();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.ErrorPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorsGridView)).BeginInit();
            this.SettingsTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.VersionIntNumericUpDown)).BeginInit();
            this.CODSCRIPTTabPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.splitContainer1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.splitContainer1.Location = new System.Drawing.Point(-1, -1);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.searchTextBox);
            this.splitContainer1.Panel1.Controls.Add(this.assemblyTreeView);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tabControl1);
            this.splitContainer1.Size = new System.Drawing.Size(286, 450);
            this.splitContainer1.SplitterDistance = 294;
            this.splitContainer1.TabIndex = 1;
            // 
            // searchTextBox
            // 
            this.searchTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.searchTextBox.Location = new System.Drawing.Point(1, 271);
            this.searchTextBox.Name = "searchTextBox";
            this.searchTextBox.Size = new System.Drawing.Size(282, 20);
            this.searchTextBox.TabIndex = 1;
            this.searchTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.searchTextBox_KeyPress);
            // 
            // assemblyTreeView
            // 
            this.assemblyTreeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.assemblyTreeView.CurrentNode = null;
            this.assemblyTreeView.ImageIndex = 0;
            this.assemblyTreeView.Location = new System.Drawing.Point(-1, -1);
            this.assemblyTreeView.Name = "assemblyTreeView";
            this.assemblyTreeView.SelectedImageIndex = 0;
            this.assemblyTreeView.ShowNodeToolTips = true;
            this.assemblyTreeView.Size = new System.Drawing.Size(286, 271);
            this.assemblyTreeView.TabIndex = 0;
            this.assemblyTreeView.Type = CODSCRIPTNpp.AssemblyTreeType.None;
            this.assemblyTreeView.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.assemblyTreeView_NodeMouseDoubleClick);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.ErrorPage);
            this.tabControl1.Controls.Add(this.SettingsTabPage);
            this.tabControl1.Controls.Add(this.CODSCRIPTTabPage);
            this.tabControl1.Location = new System.Drawing.Point(-3, -1);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(290, 152);
            this.tabControl1.TabIndex = 0;
            // 
            // ErrorPage
            // 
            this.ErrorPage.Controls.Add(this.checkBoxCurrentOnly);
            this.ErrorPage.Controls.Add(this.checkBoxWarnings);
            this.ErrorPage.Controls.Add(this.checkBoxErrors);
            this.ErrorPage.Controls.Add(this.errorsGridView);
            this.ErrorPage.Location = new System.Drawing.Point(4, 22);
            this.ErrorPage.Name = "ErrorPage";
            this.ErrorPage.Padding = new System.Windows.Forms.Padding(3);
            this.ErrorPage.Size = new System.Drawing.Size(282, 126);
            this.ErrorPage.TabIndex = 0;
            this.ErrorPage.Text = "Errors";
            this.ErrorPage.UseVisualStyleBackColor = true;
            // 
            // checkBoxCurrentOnly
            // 
            this.checkBoxCurrentOnly.AutoSize = true;
            this.checkBoxCurrentOnly.Checked = true;
            this.checkBoxCurrentOnly.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxCurrentOnly.Location = new System.Drawing.Point(136, 0);
            this.checkBoxCurrentOnly.Name = "checkBoxCurrentOnly";
            this.checkBoxCurrentOnly.Size = new System.Drawing.Size(98, 17);
            this.checkBoxCurrentOnly.TabIndex = 5;
            this.checkBoxCurrentOnly.Text = "Current file only";
            this.checkBoxCurrentOnly.UseVisualStyleBackColor = true;
            this.checkBoxCurrentOnly.CheckedChanged += new System.EventHandler(this.checkBoxErrors_CheckedChanged);
            // 
            // checkBoxWarnings
            // 
            this.checkBoxWarnings.AutoSize = true;
            this.checkBoxWarnings.Checked = true;
            this.checkBoxWarnings.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxWarnings.Location = new System.Drawing.Point(59, 0);
            this.checkBoxWarnings.Name = "checkBoxWarnings";
            this.checkBoxWarnings.Size = new System.Drawing.Size(71, 17);
            this.checkBoxWarnings.TabIndex = 4;
            this.checkBoxWarnings.Text = "Warnings";
            this.checkBoxWarnings.UseVisualStyleBackColor = true;
            this.checkBoxWarnings.CheckedChanged += new System.EventHandler(this.checkBoxErrors_CheckedChanged);
            // 
            // checkBoxErrors
            // 
            this.checkBoxErrors.AutoSize = true;
            this.checkBoxErrors.Checked = true;
            this.checkBoxErrors.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxErrors.Location = new System.Drawing.Point(0, 0);
            this.checkBoxErrors.Name = "checkBoxErrors";
            this.checkBoxErrors.Size = new System.Drawing.Size(53, 17);
            this.checkBoxErrors.TabIndex = 3;
            this.checkBoxErrors.Text = "Errors";
            this.checkBoxErrors.UseVisualStyleBackColor = true;
            this.checkBoxErrors.CheckedChanged += new System.EventHandler(this.checkBoxErrors_CheckedChanged);
            // 
            // errorsGridView
            // 
            this.errorsGridView.AllowUserToAddRows = false;
            this.errorsGridView.AllowUserToDeleteRows = false;
            this.errorsGridView.AllowUserToResizeRows = false;
            this.errorsGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.errorsGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.errorsGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Description});
            this.errorsGridView.Location = new System.Drawing.Point(0, 18);
            this.errorsGridView.MultiSelect = false;
            this.errorsGridView.Name = "errorsGridView";
            this.errorsGridView.ReadOnly = true;
            this.errorsGridView.RowHeadersVisible = false;
            this.errorsGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.errorsGridView.Size = new System.Drawing.Size(282, 108);
            this.errorsGridView.TabIndex = 2;
            this.errorsGridView.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.errorsGridView_CellMouseDoubleClick);
            // 
            // Description
            // 
            this.Description.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Description.HeaderText = "Description";
            this.Description.Name = "Description";
            this.Description.ReadOnly = true;
            // 
            // SettingsTabPage
            // 
            this.SettingsTabPage.Controls.Add(this.IsVersionIntFromDateCheckBox);
            this.SettingsTabPage.Controls.Add(this.VersionIntNumericUpDown);
            this.SettingsTabPage.Controls.Add(this.label4);
            this.SettingsTabPage.Controls.Add(this.VersionStrTextBox);
            this.SettingsTabPage.Controls.Add(this.label3);
            this.SettingsTabPage.Controls.Add(this.TargetConfigurationComboBox);
            this.SettingsTabPage.Controls.Add(this.label2);
            this.SettingsTabPage.Controls.Add(this.label1);
            this.SettingsTabPage.Controls.Add(this.TargetPlatformComboBox);
            this.SettingsTabPage.Location = new System.Drawing.Point(4, 22);
            this.SettingsTabPage.Name = "SettingsTabPage";
            this.SettingsTabPage.Padding = new System.Windows.Forms.Padding(3);
            this.SettingsTabPage.Size = new System.Drawing.Size(282, 126);
            this.SettingsTabPage.TabIndex = 1;
            this.SettingsTabPage.Text = "Settings";
            this.SettingsTabPage.UseVisualStyleBackColor = true;
            // 
            // IsVersionIntFromDateCheckBox
            // 
            this.IsVersionIntFromDateCheckBox.AutoSize = true;
            this.IsVersionIntFromDateCheckBox.Location = new System.Drawing.Point(9, 54);
            this.IsVersionIntFromDateCheckBox.Name = "IsVersionIntFromDateCheckBox";
            this.IsVersionIntFromDateCheckBox.Size = new System.Drawing.Size(127, 17);
            this.IsVersionIntFromDateCheckBox.TabIndex = 8;
            this.IsVersionIntFromDateCheckBox.Text = "IsVersionIntFromDate";
            this.IsVersionIntFromDateCheckBox.UseVisualStyleBackColor = true;
            this.IsVersionIntFromDateCheckBox.Click += new System.EventHandler(this.IsVersionIntFromDateCheckBox_Click);
            // 
            // VersionIntNumericUpDown
            // 
            this.VersionIntNumericUpDown.Location = new System.Drawing.Point(112, 72);
            this.VersionIntNumericUpDown.Maximum = new decimal(new int[] {
            2147483647,
            0,
            0,
            0});
            this.VersionIntNumericUpDown.Name = "VersionIntNumericUpDown";
            this.VersionIntNumericUpDown.Size = new System.Drawing.Size(167, 20);
            this.VersionIntNumericUpDown.TabIndex = 7;
            this.VersionIntNumericUpDown.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.VersionIntNumericUpDown_KeyPress);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(7, 74);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(54, 13);
            this.label4.TabIndex = 6;
            this.label4.Text = "VersionInt";
            // 
            // VersionStrTextBox
            // 
            this.VersionStrTextBox.Location = new System.Drawing.Point(112, 92);
            this.VersionStrTextBox.Name = "VersionStrTextBox";
            this.VersionStrTextBox.Size = new System.Drawing.Size(167, 20);
            this.VersionStrTextBox.TabIndex = 5;
            this.VersionStrTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.VersionStrTextBox_KeyPress);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 95);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(55, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "VersionStr";
            // 
            // TargetConfigurationComboBox
            // 
            this.TargetConfigurationComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.TargetConfigurationComboBox.FormattingEnabled = true;
            this.TargetConfigurationComboBox.Location = new System.Drawing.Point(112, 25);
            this.TargetConfigurationComboBox.Name = "TargetConfigurationComboBox";
            this.TargetConfigurationComboBox.Size = new System.Drawing.Size(167, 21);
            this.TargetConfigurationComboBox.TabIndex = 3;
            this.TargetConfigurationComboBox.SelectedIndexChanged += new System.EventHandler(this.TargetConfigurationComboBox_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 28);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "TargetConfiguration";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "TargetPlatform";
            // 
            // TargetPlatformComboBox
            // 
            this.TargetPlatformComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.TargetPlatformComboBox.FormattingEnabled = true;
            this.TargetPlatformComboBox.Location = new System.Drawing.Point(112, 3);
            this.TargetPlatformComboBox.Name = "TargetPlatformComboBox";
            this.TargetPlatformComboBox.Size = new System.Drawing.Size(167, 21);
            this.TargetPlatformComboBox.TabIndex = 0;
            this.TargetPlatformComboBox.SelectedIndexChanged += new System.EventHandler(this.TargetPlatformComboBox_SelectedIndexChanged);
            // 
            // CODSCRIPTTabPage
            // 
            this.CODSCRIPTTabPage.Controls.Add(this.label6);
            this.CODSCRIPTTabPage.Controls.Add(this.SettingsFileComboBox);
            this.CODSCRIPTTabPage.Controls.Add(this.SaveErrorCheckBox);
            this.CODSCRIPTTabPage.Controls.Add(this.label5);
            this.CODSCRIPTTabPage.Controls.Add(this.WorkingDirComboBox);
            this.CODSCRIPTTabPage.Location = new System.Drawing.Point(4, 22);
            this.CODSCRIPTTabPage.Name = "CODSCRIPTTabPage";
            this.CODSCRIPTTabPage.Size = new System.Drawing.Size(282, 126);
            this.CODSCRIPTTabPage.TabIndex = 2;
            this.CODSCRIPTTabPage.Text = "CODSCRIPT";
            this.CODSCRIPTTabPage.UseVisualStyleBackColor = true;
            // 
            // SaveErrorCheckBox
            // 
            this.SaveErrorCheckBox.AutoSize = true;
            this.SaveErrorCheckBox.Location = new System.Drawing.Point(6, 59);
            this.SaveErrorCheckBox.Name = "SaveErrorCheckBox";
            this.SaveErrorCheckBox.Size = new System.Drawing.Size(143, 17);
            this.SaveErrorCheckBox.TabIndex = 2;
            this.SaveErrorCheckBox.Text = "Error message after save";
            this.SaveErrorCheckBox.UseVisualStyleBackColor = true;
            this.SaveErrorCheckBox.Click += new System.EventHandler(this.SaveErrorCheckBox_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 6);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(60, 13);
            this.label5.TabIndex = 1;
            this.label5.Text = "WorkingDir";
            // 
            // WorkingDirComboBox
            // 
            this.WorkingDirComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.WorkingDirComboBox.FormattingEnabled = true;
            this.WorkingDirComboBox.Location = new System.Drawing.Point(69, 3);
            this.WorkingDirComboBox.Name = "WorkingDirComboBox";
            this.WorkingDirComboBox.Size = new System.Drawing.Size(210, 21);
            this.WorkingDirComboBox.TabIndex = 0;
            this.WorkingDirComboBox.SelectionChangeCommitted += new System.EventHandler(this.WorkingDirComboBox_SelectionChangeCommitted);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(4, 33);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(61, 13);
            this.label6.TabIndex = 4;
            this.label6.Text = "SettingsFile";
            // 
            // SettingsFileComboBox
            // 
            this.SettingsFileComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.SettingsFileComboBox.FormattingEnabled = true;
            this.SettingsFileComboBox.Location = new System.Drawing.Point(70, 30);
            this.SettingsFileComboBox.Name = "SettingsFileComboBox";
            this.SettingsFileComboBox.Size = new System.Drawing.Size(210, 21);
            this.SettingsFileComboBox.TabIndex = 3;
            this.SettingsFileComboBox.SelectionChangeCommitted += new System.EventHandler(this.SettingsFileComboBox_SelectionChangeCommitted);
            // 
            // MainDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 447);
            this.Controls.Add(this.splitContainer1);
            this.Name = "MainDlg";
            this.Text = "MainDlg";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.ErrorPage.ResumeLayout(false);
            this.ErrorPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.errorsGridView)).EndInit();
            this.SettingsTabPage.ResumeLayout(false);
            this.SettingsTabPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.VersionIntNumericUpDown)).EndInit();
            this.CODSCRIPTTabPage.ResumeLayout(false);
            this.CODSCRIPTTabPage.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private AssemblyTreeView assemblyTreeView;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage ErrorPage;
        private System.Windows.Forms.TabPage SettingsTabPage;
        private System.Windows.Forms.DataGridView errorsGridView;
        private System.Windows.Forms.CheckBox checkBoxWarnings;
        private System.Windows.Forms.CheckBox checkBoxErrors;
        private System.Windows.Forms.CheckBox checkBoxCurrentOnly;
        private System.Windows.Forms.TextBox searchTextBox;
        private System.Windows.Forms.DataGridViewTextBoxColumn Description;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox TargetPlatformComboBox;
        private System.Windows.Forms.ComboBox TargetConfigurationComboBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox VersionStrTextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown VersionIntNumericUpDown;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.CheckBox IsVersionIntFromDateCheckBox;
        private System.Windows.Forms.TabPage CODSCRIPTTabPage;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox WorkingDirComboBox;
        private System.Windows.Forms.CheckBox SaveErrorCheckBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox SettingsFileComboBox;


    }
}