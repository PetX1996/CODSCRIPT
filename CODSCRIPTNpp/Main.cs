using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using NppPluginNET;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using CODSCRIPT;
using System.Collections.Generic;
using System.Windows.Threading;

namespace CODSCRIPTNpp
{
    class Main
    {
        #region External DLLs
        static Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromPluginSubFolder);
        }
        static Assembly LoadFromPluginSubFolder(object sender, ResolveEventArgs args)
        {
            FindPluginSubFolder();
            string assemblyPath = Path.Combine(PluginSubFolder, new AssemblyName(args.Name).Name + ".dll");
            if (File.Exists(assemblyPath))
                return Assembly.LoadFrom(assemblyPath);
            else
                return null;
        }
        #endregion

        #region " Fields "
        internal const string PluginName = "CODSCRIPTNpp";
        static string iniFilePath = null;
        static bool someSetting = false;
        public static MainDlg MainDlg = null;
        static int idMyDlg = -1;
        static Bitmap tbBmp;
        static Bitmap tbBmp_tbTab = Properties.Resources.star_bmp;
        static Icon tbIcon = null;

        public static string PluginSubFolder = null;
        static bool isReady = false;

        public static FileManager Manager = null;

        internal static CompileForm CompileForm = null;

        private static int dWellTime = 800; // ms

        public static string xpmFuncPublic;
        public static string xpmFuncPrivate;
        public static string xpmConstPublic;
        public static string xpmConstPrivate;
        public static string xpmConstSealed;
        public static string xpmUsingPublic;
        public static string xpmUsingPrivate;
        public static string xpmLocalVar;
        #endregion

        #region " StartUp/CleanUp "
        internal static void CommandMenuInit()
        {
            CompileForm = new CompileForm();

            FindPluginSubFolder();

            ReadSettings();

            tbBmp = new Bitmap(Path.Combine(PluginSubFolder, "AssemblyTree.png"));

            NppNotification.Ready += new NppNotificationEvent(OnReady);
            NppNotification.BufferActivated += new NppNotificationEvent(OnFileSwitch);
            NppNotification.FileSaved += new NppNotificationEvent(OnFileSaved);

            SciNotification.CharAdded += new SciNotificationEvent(OnCharAdded);

            Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_SETMOUSEDWELLTIME, dWellTime, 0);
            //MessageBox.Show("Time: " + (int)Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GETMOUSEDWELLTIME, 0, 0));
            SciNotification.DWellStart += new SciNotificationEvent(OnDWellStart);
            SciNotification.DWellEnd += new SciNotificationEvent(OnDWellEnd);
            SciNotification.Modified += new SciNotificationEvent(SciNotification_Modified);

            /*StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            iniFilePath = sbIniFilePath.ToString();
            if (!Directory.Exists(iniFilePath)) Directory.CreateDirectory(iniFilePath);
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");
            someSetting = (Win32.GetPrivateProfileInt("SomeSection", "SomeKey", 0, iniFilePath) != 0);
            */
            PluginBase.SetCommand(0, "Show Assembly Tree", myDockableDialog, new ShortcutKey(false, false, false, Keys.None));
            PluginBase.SetCommand(1, "Go To Definition", GoToDefinition);
            PluginBase.SetCommand(2, "Find All References", FindAllReferences);
            PluginBase.SetCommand(3, "Compile", Compile);
            PluginBase.SetCommand(4, "About", myMenuFunction); idMyDlg = 0;
        }

        internal static void SetToolBarIcon()
        {
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[idMyDlg]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }

        internal static void PluginCleanUp()
        {
            // MessageBox.Show("END!");

            //Win32.WritePrivateProfileString("SomeSection", "SomeKey", someSetting ? "1" : "0", iniFilePath);
        }
        #endregion

        #region Settings
        public static string workingDir;
        public static List<string> WorkingDirList = null;

        public static string SettingsFile;
        public static List<string> SettingsFileList = null;

        public static string FSGameFolderName;

        public static bool Save_ShowErrorMsgBox;

        public static bool Compile_StartImmediately;
        public static bool Compile_CloseAfterCompile;
        public static string Compile_Raw;
        public static bool Compile_CompareDate;

        private static void FindPluginSubFolder()
        {
            if (!String.IsNullOrEmpty(PluginSubFolder))
                return;

            string pluginPath = typeof(Main).Assembly.Location;
            string pluginName = Path.GetFileNameWithoutExtension(pluginPath);
            PluginSubFolder = Path.Combine(Path.GetDirectoryName(pluginPath), pluginName);

            if (String.IsNullOrEmpty(PluginSubFolder) || !Directory.Exists(PluginSubFolder))
                MessageBox.Show("Could not find subFolder 'CODSCRIPTNpp'");

            InitTraceListener();
        }

        public static TraceSource Trace { get; private set; }

        private static void InitTraceListener()
        {
            if (Trace != null)
                return;

            Trace = new TraceSource("CODSCRIPTNpp");

            string filePath = Path.Combine(PluginSubFolder, "verboseInfo.log");
            File.Delete(filePath);

            DelimitedListTraceListener listener = new DelimitedListTraceListener(filePath);
            Trace.Listeners.Add(listener);

            Trace.Switch.Level = SourceLevels.Verbose;
        }

        private static void ReadSettings()
        {
            string path = Path.Combine(PluginSubFolder, "settings.dat");

            if (!File.Exists(path))
            {
                MessageBox.Show("Could not find 'settings.dat'");
                return;
            }

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    string line;
                    while (sr.Peek() != -1)
                    {
                        line = sr.ReadLine();
                        int assignI = line.IndexOf('=');
                        if (assignI == -1)
                            continue;

                        string name = line.Substring(0, assignI);
                        string value = line.Substring(assignI + 1);
                        SettingsFromFile(name, value);
                    }
                }
            }
        }

        private static void SettingsFromFile(string name, string value)
        {
            switch (name)
            {
                case "workingDir":
                    workingDir = value;
                    break;
                case "workingDirList":
                    if (WorkingDirList == null)
                        WorkingDirList = new List<string>();

                    WorkingDirList.Add(value);
                    break;
                case "settingsFile":
                    SettingsFile = value;
                    break;
                case "settingsFileList":
                    if (SettingsFileList == null)
                        SettingsFileList = new List<string>();

                    SettingsFileList.Add(value);
                    break;
                case "FSGameFolderName":
                    FSGameFolderName = value;
                    break;
                case "save_ShowErrorMsgBox":
                    Save_ShowErrorMsgBox = Boolean.Parse(value);
                    break;
                case "compile_StartImmediately":
                    Compile_StartImmediately = Boolean.Parse(value);
                    break;
                case "compile_CloseAfterCompile":
                    Compile_CloseAfterCompile = Boolean.Parse(value);
                    break;
                case "compile_Raw":
                    Compile_Raw = value;
                    break;
                case "compile_CompareDate":
                    Compile_CompareDate = Boolean.Parse(value);
                    break;
                default:
                    throw new ArgumentException("Unknown element '" + name + "'");
            }
        }

        public static void UpdateSettingsInFile()
        {
            string path = Path.Combine(PluginSubFolder, "settings.dat");
            StringBuilder sb = new StringBuilder();


            sb.Append("workingDir=" + workingDir);
            sb.Append(Environment.NewLine);

            foreach (string wDir in WorkingDirList)
            {
                sb.Append("workingDirList=" + wDir);
                sb.Append(Environment.NewLine);
            }


            sb.Append("settingsFile=" + SettingsFile);
            sb.Append(Environment.NewLine);

            foreach (string sFile in SettingsFileList)
            {
                sb.Append("settingsFileList=" + sFile);
                sb.Append(Environment.NewLine);
            }

            sb.Append("FSGameFolderName=" + FSGameFolderName);
            sb.Append(Environment.NewLine);

            sb.Append("save_ShowErrorMsgBox=" + Save_ShowErrorMsgBox.ToString());
            sb.Append(Environment.NewLine);

            sb.Append("compile_StartImmediately=" + Compile_StartImmediately.ToString());
            sb.Append(Environment.NewLine);

            sb.Append("compile_CloseAfterCompile=" + Compile_CloseAfterCompile.ToString());
            sb.Append(Environment.NewLine);

            sb.Append("compile_Raw=" + Compile_Raw);
            sb.Append(Environment.NewLine);

            sb.Append("compile_CompareDate=" + Compile_CompareDate.ToString());
            sb.Append(Environment.NewLine);

            File.WriteAllText(path, sb.ToString());
            //MessageBox.Show("update..");
        }
        #endregion

        #region " Menu functions "
        internal static void myMenuFunction()
        {
            try
            {
                MessageBox.Show("Created by PetX", "About");
            }
            catch (Exception e)
            {
                Main.NotifyError(e);
            }
        }
        internal static void myDockableDialog()
        {
            try
            {
                if (MainDlg == null)
                {
                    MainDlg = new MainDlg();

                    using (Bitmap newBmp = new Bitmap(16, 16))
                    {
                        Graphics g = Graphics.FromImage(newBmp);
                        ColorMap[] colorMap = new ColorMap[1];
                        colorMap[0] = new ColorMap();
                        colorMap[0].OldColor = Color.Fuchsia;
                        colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
                        ImageAttributes attr = new ImageAttributes();
                        attr.SetRemapTable(colorMap);
                        g.DrawImage(tbBmp_tbTab, new Rectangle(0, 0, 16, 16), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
                        tbIcon = Icon.FromHandle(newBmp.GetHicon());
                    }

                    NppTbData _nppTbData = new NppTbData();
                    _nppTbData.hClient = MainDlg.Handle;
                    _nppTbData.pszName = "Assembly";
                    _nppTbData.dlgID = idMyDlg;
                    _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_RIGHT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
                    _nppTbData.hIconTab = (uint)tbIcon.Handle;
                    _nppTbData.pszModuleName = PluginName;
                    IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
                    Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);

                    Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
                }
                else
                {
                    Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DMMSHOW, 0, MainDlg.Handle);
                    Thread.Sleep(50);
                    MainDlg.Refresh();
                }

                UpdateSFInfo(false);
            }
            catch (Exception e)
            {
                Main.NotifyError(e);
            }
        }

        internal static void GoToDefinition()
        {
            try
            {
                if (Manager != null)
                    Manager.GoToDefinition();
            }
            catch (Exception e)
            {
                Main.NotifyError(e);
            }
        }

        internal static void FindAllReferences()
        {
            try
            {
                if (Manager != null)
                    Manager.FindAllReferences();
            }
            catch (Exception e)
            {
                Main.NotifyError(e);
            }
        }

        internal static void Compile()
        {
            try
            {
                if (Manager == null || Manager.ScriptManager == null)
                    return;

                CompileForm.ShowDialog();
                UpdateSettingsInFile();
            }
            catch (Exception e)
            {
                Main.NotifyError(e);
            }
        }
        #endregion

        #region Update
        internal static void OnReady(SCNotification notify)
        {
            //MessageBox.Show("READY!");
            isReady = true;

            UpdateSFInfo(false);
        }

        internal static void OnFileSwitch(SCNotification notify)
        {
            if (!isReady)
                return;

            UpdateSFInfo(false);
            UpdateSettingsInFile();
        }

        #region File Saving
        private static EventWaitHandle savingQueueWaitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
        private static List<uint> savingQueueBufferIds = new List<uint>();

        internal static void OnFileSaved(SCNotification notify)
        {
            Trace.TraceEvent(TraceEventType.Verbose, 0, "Saving bufferID " + notify.nmhdr.idFrom);
            savingQueueBufferIds.Add(notify.nmhdr.idFrom);

            savingQueueWaitHandle.Set(); // kills all waiting threads...
            Thread.Sleep(5); // wtf?! really?
            savingQueueWaitHandle.Reset();

            /*
            //MessageBox.Show("Saving " + notify.nmhdr.idFrom);
            Main.Trace.TraceEvent(TraceEventType.Verbose, 0, "Saving " + notify.nmhdr.idFrom);

            int id = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETCURRENTBUFFERID, 0, 0);
            Main.Trace.TraceEvent(TraceEventType.Verbose, 0, "Cur " + id);

            StringBuilder sb = new StringBuilder(NppPluginNET.Win32.MAX_PATH);
            int successful = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETFULLPATHFROMBUFFERID, id, sb);

            MessageBox.Show("Saving cur " + id + ";" + successful + ";" + sb.ToString());
            */

            //UpdateSFInfo(true);

            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            Thread t = new Thread(WaitAfterSaved);
            t.Start(dispatcher);
        }

        private static void WaitAfterSaved(object data)
        {
            try
            {
                Trace.TraceEvent(TraceEventType.Verbose, 0, "Starting saving thread " + Thread.CurrentThread.GetHashCode());

                // npp saving files too long -> file access exception 
                // wait until npp finishes saving...
                if (savingQueueWaitHandle.WaitOne(50))
                {
                    Trace.TraceEvent(TraceEventType.Verbose, 0, "Killing saving thread " + Thread.CurrentThread.GetHashCode());
                    return;
                }

                Trace.TraceEvent(TraceEventType.Verbose, 0, "Saving files thread " + Thread.CurrentThread.GetHashCode());

                Dispatcher d = data as Dispatcher;
                Action a = DoAfterSaved;
                d.Invoke(a);
            }
            catch (Exception e)
            {
                Main.NotifyError(e);
            }
        }

        private static void DoAfterSaved()
        {
            //Trace.TraceEvent(TraceEventType.Verbose, 0, "Saving files...");

            List<string> fullPaths = new List<string>(savingQueueBufferIds.Count);
            foreach (uint id in savingQueueBufferIds)
            {
                StringBuilder sb = new StringBuilder(NppPluginNET.Win32.MAX_PATH);
                int successful = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETFULLPATHFROMBUFFERID, (int)id, sb);
                if (successful != -1)
                {
                    fullPaths.Add(sb.ToString());
                    Trace.TraceEvent(TraceEventType.Verbose, 0, "Prepare for saving SF " + sb.ToString());
                }
            }

            fullPaths.TrimExcess();
            UpdateSFsInfo(fullPaths);

            savingQueueBufferIds.Clear();
        }
        #endregion

        internal static void OnCharAdded(SCNotification notify)
        {
            if (Manager != null)
                Manager.OnCharAdded(notify.ch);
        }

        internal static void OnDWellStart(SCNotification notify)
        {
            if (Manager != null)
                Manager.OnDWellStart(notify.position);
        }

        internal static void OnDWellEnd(SCNotification notify)
        {
            if (Manager != null)
                Manager.OnDWellEnd(notify.position);
        }

        internal static void SciNotification_Modified(SCNotification notify)
        {
            if (Manager != null)
                Manager.OnModified(notify);
        }
        #endregion

        internal static void UpdateSFInfo(bool showErrors)
        {
            if (Manager == null)
                Manager = FileManager.Create(workingDir, SettingsFile, FSGameFolderName);

            if (Manager == null)
                return;

            StringBuilder path = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETFULLCURRENTPATH, 0, path);

            Manager.OnFileSwitch(path.ToString(), (MainDlg != null && MainDlg.Visible), showErrors);
        }

        internal static void UpdateSFsInfo(List<string> fullPaths)
        {
            if (Manager == null)
                Manager = FileManager.Create(workingDir, SettingsFile, FSGameFolderName);

            if (Manager == null)
                return;

            Manager.OnFileSaved(fullPaths);
        }

        /*internal static void OnOpenScriptFile(ScriptFile sf)
        {
            MessageBox.Show("SF " + sf.SFPath);
        }

        internal static void OnError(Error error)
        { 
            
        }*/

        public static void NotifyError(Exception e)
        {
            string error = string.Empty;
            error += e.Message + Environment.NewLine + e.StackTrace;
            if (e.InnerException != null)
                error += Environment.NewLine + e.InnerException + Environment.NewLine + e.InnerException.StackTrace;

            MessageBox.Show(error, e.GetType().ToString());
        }
    }
}