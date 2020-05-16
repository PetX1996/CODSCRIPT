using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

namespace CODSCRIPT
{
    public enum RawType
    {
        None,
        Extern,
        Raw,
        FSGame
    };

    public class ScriptManager
    {
        object lockerSF = new object();
        object lockerGlobalSFs = new object();
        object lockerGlobalMembers = new object();

        private Settings _settings;
        public Settings Settings { get { return _settings; } }

        private List<ScriptFile> _files;
        private List<ScriptFile> _globalFiles;

        private List<IMemberInfo> _globalMembers;

        #region Constants
        public static Encoding FilesEncoding = Encoding.GetEncoding(1250);
        public static string[] GlobalVariables = new string[] { "level", "game" };
        #endregion

        public TraceSource Trace { get; private set; }

        private void InitTraceListener(string folderPath)
        {
            Trace = new TraceSource("CODSCRIPT");

            string filePath = string.Empty;
            for (int i = 0; ; i++)
            {
                filePath = Path.Combine(folderPath, "verboseInfo_" + i + ".log");
                FileInfo f = new FileInfo(filePath);
                if (f.Exists && !f.IsLocked())
                {
                    f.Delete();
                    break;
                }
                else if (!f.Exists)
                    break;
            }

            DelimitedListTraceListener listener = new DelimitedListTraceListener(filePath);
            Trace.Listeners.Add(listener);

            Trace.Switch.Level = SourceLevels.Verbose;
        }

        private ScriptManager()
        { }

        /// <summary>
        /// Vytvorí novú inštanciu ScriptManageru pre správu zostavenia.
        /// </summary>
        /// <param name="workingDir">Cesta k hre + bin\CODSCRIPT</param>
        /// <returns>Vráti null ak je workingDir neplatný.</returns>
        public static ScriptManager Create(string workingDir, string settingsFile, string fsgameFolderName)
        {
            if (String.IsNullOrEmpty(workingDir) || !Directory.Exists(workingDir)
                || !File.Exists(Path.Combine(workingDir, "CODSCRIPT.dll")))
                return null;
            
            ScriptManager manager = new ScriptManager();
            manager.InitTraceListener(workingDir);

            manager._settings = new Settings(manager, workingDir, settingsFile, fsgameFolderName);
            
            manager._files = new List<ScriptFile>();

            manager.ReadExternSFs();
            
            return manager;
        }

        #region SFManager
        public ScriptFile GetSFFromFullPath(string fullPath)
        {
            RawType rawType = RawType.None;
            string sfPath = string.Empty;

            if (String.IsNullOrEmpty(fullPath))
                return null;

            string ext = Path.GetExtension(fullPath);
            if (String.IsNullOrEmpty(ext))
                return null;

            ext = ext.Substring(1).ToUpperInvariant(); // '.gsc' to 'GSC'

            string[] extensions = Enum.GetNames(typeof(ScriptFile.Extension));
            if (!extensions.Contains(ext))
                return null;

            string dir = Path.GetDirectoryName(fullPath);
            string fileName = Path.GetFileNameWithoutExtension(fullPath);

            if (_settings.SourceFSGame != null
                && fullPath.Length > _settings.SourceFSGame.Length
                && fullPath.Substring(0, _settings.SourceFSGame.Length).ToUpperInvariant() == _settings.SourceFSGame.ToUpperInvariant())
            {
                string relativeDir = dir.Substring(_settings.SourceFSGame.Length + 1);
                if (_settings.SourceFSGameFolders.Contains(relativeDir))
                {
                    rawType = RawType.FSGame;
                    sfPath = Path.Combine(relativeDir, fileName);
                }
            }
            else if (_settings.SourceRaw != null
                && fullPath.Length > _settings.SourceRaw.Length
                && fullPath.Substring(0, _settings.SourceRaw.Length).ToUpperInvariant() == _settings.SourceRaw.ToUpperInvariant())
            {
                string relativeDir = dir.Substring(_settings.SourceRaw.Length + 1);
                if (_settings.SourceRawFolders.Contains(relativeDir))
                {
                    rawType = RawType.Raw;
                    sfPath = Path.Combine(relativeDir, fileName);
                }
            }

            if (!String.IsNullOrEmpty(sfPath))
            {
                ScriptFile.Extension extension = (ScriptFile.Extension)Enum.Parse(typeof(ScriptFile.Extension), ext);
                ScriptFile sf = GetSF(sfPath, extension);
                if (sf.RawType == rawType)
                    return sf;
            }

            return null;
        }

        public ScriptFile GetSF(string filePath, ScriptFile.Extension ext)
        {
            PrintDebug("GetSF(string filePath, ScriptFile.Extension ext)", true);
            lock (lockerSF)
            {
                ScriptFile file = _files.Find(a => a.SFPath.EqualCode(filePath) && a.FileExt == ext);
                if (file == null)
                    file = CreateSF(filePath, ext, false);

                PrintDebug("GetSF(string filePath, ScriptFile.Extension ext)", false);
                return file;
            }
        }

        private void PrintDebug(string funcName, bool start)
        {
            /*string startStr = "Start";
            if (!start)
                startStr = "End";

            Console.WriteLine(funcName +  "; Thread: " + Thread.CurrentThread.Name + Thread.CurrentThread.ManagedThreadId + "; " + startStr);*/
        }

        public ScriptFile GetSF(string filePath)
        {
            PrintDebug("GetSF(string filePath)", true);
            lock (lockerSF)
            {
                ScriptFile file;

                if (_settings.TargetPlatform == TargetPlatform.LinuxNinja)
                {
                    file = _files.Find(a => a.SFPath.EqualCode(filePath) && a.FileExt == ScriptFile.Extension.GSX);
                    if (file == null)
                        file = _files.Find(a => a.SFPath.EqualCode(filePath) && a.FileExt == ScriptFile.Extension.GSC);

                    if (file == null)
                        file = CreateSF(filePath, false);
                }
                else
                {
                    file = _files.Find(a => a.SFPath.EqualCode(filePath) && a.FileExt == ScriptFile.Extension.GSC);
                    if (file == null)
                        file = CreateSF(filePath, ScriptFile.Extension.GSC, false);                   
                }

                PrintDebug("GetSF(string filePath)", false);
                return file;
            }
        }

        private ScriptFile CreateSF(string filePath, ScriptFile.Extension ext, bool isExtern)
        {
            ScriptFile script = ScriptFile.Create(this, filePath, ext, isExtern);
            if (script != null)
                _files.Add(script);

            return script;
        }

        private ScriptFile CreateSF(string filePath, bool isExtern)
        {
            ScriptFile file;
            if (_settings.TargetPlatform == TargetPlatform.LinuxNinja)
            {
                file = CreateSF(filePath, ScriptFile.Extension.GSX, isExtern);
                if (file == null)
                    file = CreateSF(filePath, ScriptFile.Extension.GSC, isExtern);
            }
            else
                file = CreateSF(filePath, ScriptFile.Extension.GSC, isExtern);

            return file;
        }

        private ScriptFile CreateSF(string filePath)
        {
            return CreateSF(filePath, false);
        }

        internal List<ScriptFile> GetGlobalSFs()
        {
            PrintDebug("GetGlobalSFs()", true);
            lock (lockerGlobalSFs)
            {
                if (this._globalFiles == null)
                {
                    this._globalFiles = new List<ScriptFile>();
                    foreach (ScriptFile SF in this._files)
                    {
                        if (SF.SI != null && SF.SI.IsGlobal)
                            this._globalFiles.Add(SF);
                    }
                }

                PrintDebug("GetGlobalSFs()", false);
                return this._globalFiles;
            }
        }

        public List<ScriptFile> GetAllSFs()
        {
            return _files;
        }
        #endregion

        private void ReadExternSFs()
        {
            string path = Path.Combine(_settings.SIsPath, "Extern");
            string[] files = Directory.GetFiles(path, "*.xml", SearchOption.AllDirectories);

            string fileNameWithoutXML;
            string relativePath;
            string extStr;
            string fileName;
            string fullPath;
            foreach (string filePath in files)
            {
                // blabla\Extern\maps\_utility.gsc.xml
                fileNameWithoutXML = Path.GetFileNameWithoutExtension(filePath);
                extStr = Path.GetExtension(fileNameWithoutXML); // .gsc or .gsx
                if (!String.IsNullOrEmpty(extStr))
                {
                    extStr = extStr.Substring(1).ToUpperInvariant();
                    ScriptFile.Extension ext = (ScriptFile.Extension)Enum.Parse(typeof(ScriptFile.Extension), extStr);

                    fileName = Path.GetFileNameWithoutExtension(fileNameWithoutXML);
                    fullPath = Path.Combine(Path.GetDirectoryName(filePath), fileName);
                    relativePath = fullPath.Remove(0, path.Length + 1); // delete path and separator
                    ScriptFile sf = CreateSF(relativePath, ext, true);
                    sf.ReadSI();
                }
            }
        }

        #region Files Finding & Reading
        public void FindAssemblySFs()
        {
            if (!String.IsNullOrEmpty(_settings.SourceFSGame))
                FindAssemblySFsInRaw(_settings.SourceFSGame, _settings.SourceFSGameFolders);
            if (!String.IsNullOrEmpty(_settings.SourceRaw))
                FindAssemblySFsInRaw(_settings.SourceRaw, _settings.SourceRawFolders);
        }

        private void FindAssemblySFsInRaw(string rawFullPath, List<string> folders, ref List<ScriptFile> files)
        {
            foreach (string curFolder in folders)
            {
                string fullFolderPath = Path.Combine(rawFullPath, curFolder);

                List<string> sfFiles = new List<string>(Directory.GetFiles(fullFolderPath, "*.gsx", SearchOption.TopDirectoryOnly));
                sfFiles.AddRange(Directory.GetFiles(fullFolderPath, "*.gsc", SearchOption.TopDirectoryOnly));
                foreach (string sfFile in sfFiles)
                {
                    string file = Path.GetFileNameWithoutExtension(sfFile);
                    string sfPath = Path.Combine(curFolder, file);

                    ScriptFile.Extension ext = ScriptFile.Extension.GSX;
                    if (Path.GetExtension(sfFile).ToUpperInvariant() == ".GSC")
                        ext = ScriptFile.Extension.GSC;

                    ScriptFile sf = GetSF(sfPath, ext);
                    if (sf != null)
                        files.Add(sf);
                }
            }
        }

        private void FindAssemblySFsInRaw(string rawFullPath, List<string> folders)
        {
            List<ScriptFile> files = new List<ScriptFile>();
            FindAssemblySFsInRaw(rawFullPath, folders, ref files);
        }

        /// <summary>
        /// Updates all loaded SFs. Adds news and deletes non-valid files.
        /// </summary>
        public void UpdateAssemblySFs()
        {
            List<ScriptFile> lastFiles = new List<ScriptFile>(_files);
            Settings.UpdateSourceRawFolders();

            List<ScriptFile> newFiles = new List<ScriptFile>();

            if (!String.IsNullOrEmpty(_settings.SourceFSGame))
                FindAssemblySFsInRaw(_settings.SourceFSGame, _settings.SourceFSGameFolders, ref newFiles);
            if (!String.IsNullOrEmpty(_settings.SourceRaw))
                FindAssemblySFsInRaw(_settings.SourceRaw, _settings.SourceRawFolders, ref newFiles);

            // if SF is in RAW and FSGAME...then are 2 refs in list...
            newFiles = new List<ScriptFile>(newFiles.Distinct());

            //foreach (ScriptFile sf in lastFiles)
                //Trace.TraceEvent(TraceEventType.Verbose, 0, sf.ToString());

            //foreach (ScriptFile sf in newFiles)
                //Trace.TraceEvent(TraceEventType.Verbose, 0, sf.ToString());
            
            //Trace.Flush();

            List<ScriptFile> deletedFiles = new List<ScriptFile>(lastFiles.Except(newFiles));
            deletedFiles.RemoveAll(a => a.IsExtern);

            _files.Clear();
            _files.AddRange(lastFiles.Except(deletedFiles));
        }

        public void ReadAssemblySFs(ReadingState state)
        {
            ReadAssemblySFs(state, GetAllSFs(), false);
        }

        public void ReadAssemblySFs(ReadingState state, List<ScriptFile> filesForReading)
        {
            ReadAssemblySFs(state, filesForReading, false);
        }

        public void ReadAssemblySFs(ReadingState state, List<ScriptFile> filesForReading, bool compileIgnoreExcluded)
        {
            _waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            _activeThreads = 0;

            int currentIndex = 0;
            while (true)
            {
                for (; currentIndex < filesForReading.Count; currentIndex++)
                {
                    lock (_activeThreadsLocker)
                    {
                        if (_activeThreads > _maxActiveThreads)
                            break;
                    }

                    ScriptFile curSF = filesForReading[currentIndex];
                    if (compileIgnoreExcluded && curSF.Compile_IsExcluded)
                        continue;

                    switch (state)
                    {
                        case ReadingState.ScriptInfo:
                            ReadSFInThread(curSF.ReadSI);
                            break;
                        case ReadingState.ScriptCode:
                            if (!curSF.IsExtern)
                                ReadSFInThread(curSF.ReadSC);
                            break;
                        case ReadingState.CheckCode:
                            if (curSF.SC != null)
                                ReadSFInThread(curSF.CheckSC);
                            break;
                        default:
                            throw new ArgumentException("state");
                    }
                }

                if (_activeThreads > 0) // because this...
                    _waitHandle.WaitOne(60000); // it can freeze here...
                else
                    break;
            }
        }

        private int _activeThreads;
        private object _activeThreadsLocker = new object();
        private const int _maxActiveThreads = 10;
        private EventWaitHandle _waitHandle;

        private void ReadSFInThread(Action method)
        {
            lock (_activeThreadsLocker)
            {
                _activeThreads++;
            }

            Thread t = new Thread(DoReadSFInThread);
            t.Start(method);
        }

        private void DoReadSFInThread(object data)
        {
            try
            {
                Action method = data as Action;
                method.Invoke();

                lock (_activeThreadsLocker)
                {
                    _activeThreads--;
                }

                _waitHandle.Set();
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message + Environment.NewLine + e.StackTrace, e.ToString());
            }
        }
        #endregion

        #region Files Compiling
        public void CompileAssemblySFs(RawType raw, bool compareDate)
        {
            List<ScriptFile> processFiles = CompileAssemblySFs_GetFiles(raw, compareDate);

            int errorsCount = 0;
            int warningsCount = 0;

            CompileAssemblySFs_ReadSC(processFiles, ref errorsCount, ref warningsCount);
            CompileAssemblySFs_CheckSC(processFiles, ref errorsCount, ref warningsCount);

            foreach (ScriptFile sf in processFiles)
                CompileAssemblySF_Compile(sf);

            CompileAssemblySF_Finish(raw);
        }

        public List<ScriptFile> CompileAssemblySFs_GetFiles(RawType raw, bool compareDate)
        {
            UpdateAssemblySFs();

            OutputSetting primarySetting = Settings.GetPrimaryOutput(raw);

            bool includeAll = String.IsNullOrEmpty(primarySetting.IncludeFiles);
            bool excludeNone = String.IsNullOrEmpty(primarySetting.ExcludeFiles);

            List<ScriptFile> sourceFiles = new List<ScriptFile>(GetAllSFs().Count);
            foreach (ScriptFile sf in GetAllSFs())
            {
                if (!sf.IsExtern && sf.RawType == raw)
                {
                    if (includeAll
                        || (!includeAll && Regex.IsMatch(sf.SFPath, primarySetting.IncludeFiles)))
                    {
                        if (!excludeNone && Regex.IsMatch(sf.SFPath, primarySetting.ExcludeFiles))
                            sf.Compile_IsExcluded = true;
                        else
                            sf.Compile_IsExcluded = false;
                    }
                    else
                    {
                        sf.Compile_IsExcluded = true;
                    }

                    sourceFiles.Add(sf);
                }
            }

            // select files for compile...
            List<ScriptFile> processFiles = new List<ScriptFile>(sourceFiles.Count);
            foreach (ScriptFile sf in sourceFiles)
            {
                string sourceFile = sf.SFFullPath;
                string outputFile = Path.Combine(primarySetting.LocalPath, sf.SFPath + "." + sf.FileExt.ToString());

                if (compareDate) // compare date
                {
                    DateTime sourceTime = File.GetLastWriteTime(sourceFile);
                    DateTime outputTime = File.Exists(outputFile) ? File.GetLastWriteTime(outputFile) : DateTime.MinValue;
                    if (sourceTime >= outputTime)
                        processFiles.Add(sf);
                }
                else
                    processFiles.Add(sf);
            }

            Trace.TraceEvent(TraceEventType.Information, 0, "Files for compile: " + processFiles.Count);
            foreach (ScriptFile sf in processFiles)
                Trace.TraceEvent(TraceEventType.Verbose, 0, sf.ToString());

            Trace.TraceEvent(TraceEventType.Information, 0, "============================");

            return processFiles;
        }

        public bool CompileAssemblySFs_ReadSC(List<ScriptFile> processFiles, ref int errorsCount, ref int warningsCount)
        {
            ReadAssemblySFs(ReadingState.ScriptCode, processFiles, true);

            if (CompileAssemblySFs_CheckErrors(processFiles, ref errorsCount, ref warningsCount))
            {
                Trace.TraceEvent(TraceEventType.Error, 0, "Could not read SFs' SC");
                return false;
            }
            return true;
        }

        public bool CompileAssemblySFs_CheckSC(List<ScriptFile> processFiles, ref int errorsCount, ref int warningsCount)
        {
            ReadAssemblySFs(ReadingState.CheckCode, processFiles, true);

            if (CompileAssemblySFs_CheckErrors(processFiles, ref errorsCount, ref warningsCount))
            {
                Trace.TraceEvent(TraceEventType.Error, 0, "Could not check SFs' SC");
                return false;
            }
            return true;
        }

        private bool CompileAssemblySFs_CheckErrors(List<ScriptFile> processFiles, ref int errorsCount, ref int warningsCount)
        {
            bool anyError = false;
            foreach (ScriptFile sf in processFiles)
            {
                if (sf.Compile_IsExcluded)
                    continue;

                foreach (Error e in sf.Errors)
                {
                    if (e is WarningError)
                    {
                        Trace.TraceEvent(TraceEventType.Warning, 0, e.FullMessage);
                        warningsCount++;
                    }
                    else
                    {
                        Trace.TraceEvent(TraceEventType.Error, 0, e.FullMessage);
                        errorsCount++;
                    }
                }

                if (sf.Errors.AnyErrors)
                    anyError = true;
            }
            return anyError;
        }

        public bool CompileAssemblySF_Compile(ScriptFile sf)
        {
            if (sf.Compile_IsExcluded)
            {
                sf.CompileOutputSC(true);
            }
            else
            {
                if (!sf.PrepareCompileSC())
                {
                    Trace.TraceEvent(TraceEventType.Error, 0, "Could not compile SFs'");
                    return false;
                }

                if (!sf.CompileMembersSC())
                {
                    Trace.TraceEvent(TraceEventType.Error, 0, "Could not compile SFs'");
                    return false;
                }

                sf.CompileCodeSC();
                sf.CompileOutputSC();
            }

            return true;
        }

        public void CompileAssemblySF_Finish(RawType raw)
        {
            OutputSetting setting = Settings.GetPrimaryOutput(raw);
            setting.FinishCompile();
        }
        #endregion

        #region SF tools
        /// <summary>
        /// Vráti zoznam globálnych členov.
        /// </summary>
        /// <returns></returns>
        public List<IMemberInfo> GetGlobalMembers()
        {
            PrintDebug("GetGlobalMembers()", true);
            lock (lockerGlobalMembers)
            {
                if (_globalMembers == null)
                {
                    _globalMembers = new List<IMemberInfo>();
                    foreach (ScriptFile sf in GetGlobalSFs())
                    {
                        // hmm...
                        // http://blogs.msdn.com/b/ericlippert/archive/2007/10/17/covariance-and-contravariance-in-c-part-two-array-covariance.aspx
                        foreach (ConstInfo curConts in sf.SI.Constants)
                            _globalMembers.Add(curConts);
                        foreach (FuncInfo curFunc in sf.SI.Functions)
                            _globalMembers.Add(curFunc);
                    }

                    PrintDebug("GetGlobalMembers()", false);
                    return _globalMembers;
                }
                else
                {
                    PrintDebug("GetGlobalMembers()", false);
                    return _globalMembers;
                }
            }
        }
        #endregion
    }
}
