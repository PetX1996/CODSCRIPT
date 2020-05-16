using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using CODSCRIPT;

namespace CODSCRIPTNpp
{
    public partial class CompileForm : Form
    {
        public CompileForm()
        {
            InitializeComponent();
        }

        private bool _anyError;
        private TextBoxBaseTraceListener _listener;
        private DateTime _startTime;

        private void CompileForm_Load(object sender, EventArgs e)
        {
            if (Main.Manager != null && Main.Manager.ScriptManager != null)
            {
                ScriptManager scrManager = Main.Manager.ScriptManager;

                // initialize
                this.ProgressBar.Value = 0;
                this.ConsoleTextBox.ResetText();
                this.TimeTextBox.Clear();

                this.StartImmediatelyCheckBox.Checked = Main.Compile_StartImmediately;
                this.CloseAfterCompleteCheckBox.Checked = Main.Compile_CloseAfterCompile;
                this.RawComboBox.SelectedItem = Main.Compile_Raw;
                this.CompareDateCheckBox.Checked = Main.Compile_CompareDate;

                if (_listener == null)
                    _listener = new TextBoxBaseTraceListener(this.ConsoleTextBox);

                // compile
                scrManager.Trace.Listeners.Add(_listener);

                this.Text = "Compiling - Ready";

                if (this.StartImmediatelyCheckBox.Checked)
                {
                    this.Text = "Compiling - Working";
                    _startTime = DateTime.Now;

                    this.ControlBox = true;

                    this.BackgroundWorker.RunWorkerAsync();
                }
            }
        }

        private void CompileForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Main.Manager != null && Main.Manager.ScriptManager != null)
            {
                ScriptManager scrManager = Main.Manager.ScriptManager;
                scrManager.Trace.Listeners.Remove(_listener);
            }
        }

        #region Settings
        private void UpdateSettings()
        {
            Main.Compile_StartImmediately = this.StartImmediatelyCheckBox.Checked;
            Main.Compile_CloseAfterCompile = this.CloseAfterCompleteCheckBox.Checked;
            Main.Compile_Raw = this.RawComboBox.SelectedItem.ToString();
            Main.Compile_CompareDate = this.CompareDateCheckBox.Checked;
        }

        private void StartImmediatelyCheckBox_Click(object sender, EventArgs e)
        {
            UpdateSettings();
        }

        private void CloseAfterCompleteCheckBox_Click(object sender, EventArgs e)
        {
            UpdateSettings();
        }

        private void RawComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateSettings();
        }

        private void CompareDateCheckBox_Click(object sender, EventArgs e)
        {
            UpdateSettings();
        }
        #endregion

        private void StartStopButton_Click(object sender, EventArgs e)
        {
            if (this.BackgroundWorker.IsBusy)
            {
                this.Text = "Compiling - Stopping";

                this.ControlBox = true;

                this.BackgroundWorker.CancelAsync();
            }
            else
            {
                this.ProgressBar.Value = 0;
                this.ConsoleTextBox.ResetText();
                this.TimeTextBox.Clear();

                this.Text = "Compiling - Working";
                _startTime = DateTime.Now;

                this.ControlBox = false;

                this.BackgroundWorker.RunWorkerAsync();
            }
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int curProgress = 0;
            int getFilesPart = 10;
            int readSCPart = 20;
            int checkSCPart = 20;
            int compilePart = 50;

            int errorsCount = 0;
            int warningsCount = 0;
            bool successful = true;

            _anyError = true;

            try
            {
                BackgroundWorker bgWorker = (BackgroundWorker)sender;
                ScriptManager scrManager = Main.Manager.ScriptManager;
                RawType raw = (RawType)Enum.Parse(typeof(RawType), this.RawComboBox.SelectedItem.ToString());

                List<ScriptFile> files = scrManager.CompileAssemblySFs_GetFiles(raw, this.CompareDateCheckBox.Checked);
                bgWorker.ReportProgress(curProgress += getFilesPart);

                if (!successful || bgWorker.CancellationPending)
                    return;

                successful = scrManager.CompileAssemblySFs_ReadSC(files, ref errorsCount, ref warningsCount);
                scrManager.Trace.TraceEvent(TraceEventType.Information, 0, "Reading SC finished with " + errorsCount + " errors and " + warningsCount + " warnings");
                bgWorker.ReportProgress(curProgress += readSCPart);

                if (!successful || bgWorker.CancellationPending)
                    return;

                successful = scrManager.CompileAssemblySFs_CheckSC(files, ref errorsCount, ref warningsCount);
                scrManager.Trace.TraceEvent(TraceEventType.Information, 0, "Checking SC finished with " + errorsCount + " errors and " + warningsCount + " warnings");
                bgWorker.ReportProgress(curProgress += checkSCPart);

                if (!successful || bgWorker.CancellationPending)
                    return;

                if (files.Count > 0)
                {
                    int compilePartPerFile = compilePart / files.Count;
                    foreach (ScriptFile sf in files)
                    {
                        if (!scrManager.CompileAssemblySF_Compile(sf) || bgWorker.CancellationPending)
                            return;

                        bgWorker.ReportProgress(curProgress += compilePartPerFile);
                    }
                }

                scrManager.CompileAssemblySF_Finish(raw);
                scrManager.Trace.TraceEvent(TraceEventType.Information, 0, "Compiling finished with " + errorsCount + " errors and " + warningsCount + " warnings");
                bgWorker.ReportProgress(100);

                _anyError = false;
            }
            catch (Exception ex)
            {
                Main.NotifyError(ex);
            }
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.ProgressBar.Value = e.ProgressPercentage;
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_anyError)
                this.Text = "Compiling - Error";
            else
                this.Text = "Compiling - Ready";

            TimeSpan t = DateTime.Now - _startTime;
            this.TimeTextBox.Text = (int)t.TotalSeconds + "." + t.Milliseconds;

            this.ControlBox = true;

            if (!_anyError && this.CloseAfterCompleteCheckBox.Checked)
                this.Close();
        }
    }

    // http://stackoverflow.com/questions/1389264/trace-listener-to-write-to-a-text-box-wpf-application
    public class TextBoxBaseTraceListener : TraceListener
    {
        private RichTextBox _output;

        public TextBoxBaseTraceListener(RichTextBox output)
        {
            this.Name = "Trace";
            this._output = output;
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id)
        {
            TraceEvent(eventCache, source, eventType, id, string.Empty);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
        {
            TraceEvent(eventCache, source, eventType, id, message, string.Empty);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
        {
            if (String.IsNullOrEmpty(format))
                return;

            string message;
            if (args.Length > 0 && !String.IsNullOrEmpty(args[0].ToString()))
                message = String.Format(format, args);
            else
                message = format;

            Color color = Color.Black;
            if (eventType == TraceEventType.Error)
                color = Color.Red;
            else if (eventType == TraceEventType.Warning)
                color = Color.Orange;
            else if (eventType == TraceEventType.Information)
                color = Color.Blue;

            WriteLine(message, color);
        }

        public void Write(string message, Color color)
        {
            Action append = delegate()
            {
                _output.AppendText(message, color);
            };

            if (_output.InvokeRequired)
                _output.BeginInvoke(append);
            else
                append();            
        }

        public void WriteLine(string message, Color color)
        {
            Write(message + Environment.NewLine, color);
        }

        public override void Write(string message)
        {
            throw new NotImplementedException();
        }

        public override void WriteLine(string message)
        {
            throw new NotImplementedException();
        }
    }

    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;

            box.ScrollToCaret();
        }
    }
}
