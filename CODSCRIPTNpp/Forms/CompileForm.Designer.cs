namespace CODSCRIPTNpp
{
    partial class CompileForm
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
            this.CloseAfterCompleteCheckBox = new System.Windows.Forms.CheckBox();
            this.TimeTextBox = new System.Windows.Forms.TextBox();
            this.StartImmediatelyCheckBox = new System.Windows.Forms.CheckBox();
            this.RawComboBox = new System.Windows.Forms.ComboBox();
            this.StartStopButton = new System.Windows.Forms.Button();
            this.BackgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.ProgressBar = new System.Windows.Forms.ProgressBar();
            this.CompareDateCheckBox = new System.Windows.Forms.CheckBox();
            this.ConsoleTextBox = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // CloseAfterCompleteCheckBox
            // 
            this.CloseAfterCompleteCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.CloseAfterCompleteCheckBox.AutoSize = true;
            this.CloseAfterCompleteCheckBox.Location = new System.Drawing.Point(114, 184);
            this.CloseAfterCompleteCheckBox.Name = "CloseAfterCompleteCheckBox";
            this.CloseAfterCompleteCheckBox.Size = new System.Drawing.Size(122, 17);
            this.CloseAfterCompleteCheckBox.TabIndex = 1;
            this.CloseAfterCompleteCheckBox.Text = "Close after complete";
            this.CloseAfterCompleteCheckBox.UseVisualStyleBackColor = true;
            this.CloseAfterCompleteCheckBox.Click += new System.EventHandler(this.CloseAfterCompleteCheckBox_Click);
            // 
            // TimeTextBox
            // 
            this.TimeTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.TimeTextBox.Location = new System.Drawing.Point(623, 181);
            this.TimeTextBox.Name = "TimeTextBox";
            this.TimeTextBox.ReadOnly = true;
            this.TimeTextBox.Size = new System.Drawing.Size(80, 20);
            this.TimeTextBox.TabIndex = 2;
            // 
            // StartImmediatelyCheckBox
            // 
            this.StartImmediatelyCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.StartImmediatelyCheckBox.AutoSize = true;
            this.StartImmediatelyCheckBox.Location = new System.Drawing.Point(3, 183);
            this.StartImmediatelyCheckBox.Name = "StartImmediatelyCheckBox";
            this.StartImmediatelyCheckBox.Size = new System.Drawing.Size(105, 17);
            this.StartImmediatelyCheckBox.TabIndex = 3;
            this.StartImmediatelyCheckBox.Text = "Start immediately";
            this.StartImmediatelyCheckBox.UseVisualStyleBackColor = true;
            this.StartImmediatelyCheckBox.Click += new System.EventHandler(this.StartImmediatelyCheckBox_Click);
            // 
            // RawComboBox
            // 
            this.RawComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.RawComboBox.DisplayMember = "Raw";
            this.RawComboBox.FormattingEnabled = true;
            this.RawComboBox.Items.AddRange(new object[] {
            "Raw",
            "FSGame"});
            this.RawComboBox.Location = new System.Drawing.Point(341, 181);
            this.RawComboBox.Name = "RawComboBox";
            this.RawComboBox.Size = new System.Drawing.Size(113, 21);
            this.RawComboBox.TabIndex = 4;
            this.RawComboBox.ValueMember = "Raw";
            this.RawComboBox.SelectedIndexChanged += new System.EventHandler(this.RawComboBox_SelectedIndexChanged);
            // 
            // StartStopButton
            // 
            this.StartStopButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.StartStopButton.Location = new System.Drawing.Point(460, 180);
            this.StartStopButton.Name = "StartStopButton";
            this.StartStopButton.Size = new System.Drawing.Size(157, 23);
            this.StartStopButton.TabIndex = 5;
            this.StartStopButton.Text = "Start/Stop";
            this.StartStopButton.UseVisualStyleBackColor = true;
            this.StartStopButton.Click += new System.EventHandler(this.StartStopButton_Click);
            // 
            // BackgroundWorker
            // 
            this.BackgroundWorker.WorkerReportsProgress = true;
            this.BackgroundWorker.WorkerSupportsCancellation = true;
            this.BackgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundWorker_DoWork);
            this.BackgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.BackgroundWorker_ProgressChanged);
            this.BackgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BackgroundWorker_RunWorkerCompleted);
            // 
            // ProgressBar
            // 
            this.ProgressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ProgressBar.Location = new System.Drawing.Point(3, 167);
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new System.Drawing.Size(700, 10);
            this.ProgressBar.TabIndex = 6;
            // 
            // CompareDateCheckBox
            // 
            this.CompareDateCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.CompareDateCheckBox.AutoSize = true;
            this.CompareDateCheckBox.Location = new System.Drawing.Point(243, 183);
            this.CompareDateCheckBox.Name = "CompareDateCheckBox";
            this.CompareDateCheckBox.Size = new System.Drawing.Size(92, 17);
            this.CompareDateCheckBox.TabIndex = 7;
            this.CompareDateCheckBox.Text = "Compare date";
            this.CompareDateCheckBox.UseVisualStyleBackColor = true;
            this.CompareDateCheckBox.Click += new System.EventHandler(this.CompareDateCheckBox_Click);
            // 
            // ConsoleTextBox
            // 
            this.ConsoleTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.ConsoleTextBox.DetectUrls = false;
            this.ConsoleTextBox.Location = new System.Drawing.Point(3, 3);
            this.ConsoleTextBox.Name = "ConsoleTextBox";
            this.ConsoleTextBox.ReadOnly = true;
            this.ConsoleTextBox.Size = new System.Drawing.Size(700, 158);
            this.ConsoleTextBox.TabIndex = 8;
            this.ConsoleTextBox.Text = "";
            this.ConsoleTextBox.WordWrap = false;
            // 
            // CompileForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(705, 203);
            this.Controls.Add(this.ConsoleTextBox);
            this.Controls.Add(this.CompareDateCheckBox);
            this.Controls.Add(this.ProgressBar);
            this.Controls.Add(this.StartStopButton);
            this.Controls.Add(this.RawComboBox);
            this.Controls.Add(this.StartImmediatelyCheckBox);
            this.Controls.Add(this.TimeTextBox);
            this.Controls.Add(this.CloseAfterCompleteCheckBox);
            this.Name = "CompileForm";
            this.Text = "CompileForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CompileForm_FormClosing);
            this.Load += new System.EventHandler(this.CompileForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox CloseAfterCompleteCheckBox;
        private System.Windows.Forms.TextBox TimeTextBox;
        private System.Windows.Forms.CheckBox StartImmediatelyCheckBox;
        private System.Windows.Forms.ComboBox RawComboBox;
        private System.Windows.Forms.Button StartStopButton;
        private System.ComponentModel.BackgroundWorker BackgroundWorker;
        private System.Windows.Forms.ProgressBar ProgressBar;
        private System.Windows.Forms.CheckBox CompareDateCheckBox;
        private System.Windows.Forms.RichTextBox ConsoleTextBox;
    }
}