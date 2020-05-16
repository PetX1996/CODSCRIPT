using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Diagnostics;

namespace CODSCRIPT
{
    public enum TargetPlatform
    {
        None,
        Windows,
        Linux,
        LinuxNinja
    };

    public enum TargetConfiguration
    {
        None,
        Debug,
        Release,
        FinalRelease
    };

    public class Settings
    {
        private ScriptManager _scrManager;

        #region Common
        public static string GetFullPathFromRelativeOrFull(string parentPath, string path, bool isFile)
        {
            string fullPath = string.Empty;
            if (!String.IsNullOrEmpty(path))
            {
                if (!Path.IsPathRooted(path)) // Mods\escape
                    fullPath = Path.Combine(parentPath, path);
                else // C:\cod4\Mods\escape
                    fullPath = path;

                if ((isFile && File.Exists(fullPath))
                    || (!isFile && Directory.Exists(fullPath)))
                    return fullPath;
                else
                    throw new DirectoryNotFoundException(fullPath);
            }
            return null;
        }

        public string ReplacePathVariables(string path)
        {
            int dollarI = path.IndexOf('$');
            while (dollarI != -1)
            {
                if (path[dollarI + 1] != '(')
                    break;

                int startI = dollarI + 2;
                if (path.Length <= startI)
                    break;

                int endBracketI = path.IndexOf(')', startI); // $()
                if (endBracketI == -1)
                    break;

                int length = endBracketI - startI;
                string name = path.Substring(startI, length);

                string value = GetPathVariableValue(name);
                if (String.IsNullOrEmpty(value))
                    throw new ApplicationException("Could not find PathVariable $(" + name + ")");

                path = path.Remove(dollarI, (endBracketI + 1) - dollarI);
                path = path.Insert(dollarI, value);

                dollarI = path.IndexOf('$');
            }
            return path;
        }

        private string GetPathVariableValue(string varName)
        {
            switch (varName)
            {
                case "FSGameFolderName":
                    return FSGameFolderName;
                default:
                    return null;
            }
        }
        #endregion

        #region Base Paths
        public string GamePath { get; private set; }
        public string WorkingPath { get; private set; }

        public string SIsFSGameFolder { get; private set; }
        public string SIsPath { get; private set; }

        private string _settingsFile;


        public string FSGameFolderName { get; private set; }


        public Settings(ScriptManager scrManager, string workingPath, string settingsFile, string fsgameFolderName)
        {
            _scrManager = scrManager;

            FSGameFolderName = fsgameFolderName;

            WorkingPath = workingPath;

            FindGamePath();
            FindSettingsFile(settingsFile);

            ReadSettings();

            FindSIPath();
        }

        private void FindGamePath()
        {
            string binDir = Path.GetDirectoryName(WorkingPath);
            string gameDir = Path.GetDirectoryName(binDir);
            string exePath = Path.Combine(gameDir, "iw3mp.exe");
            if (!File.Exists(exePath))
                throw new FileNotFoundException(exePath);

            this.GamePath = gameDir;
        }

        private void FindSettingsFile(string settingsFile)
        {
            _settingsFile = Path.Combine(GamePath, settingsFile);
            if (!File.Exists(_settingsFile))
                throw new FileNotFoundException("Could not found settings file '" + _settingsFile + "'");
        }

        private void FindSIPath()
        {
            string siPath = Path.Combine(WorkingPath, "scriptinfo");
            if (!Directory.Exists(siPath))
                Directory.CreateDirectory(siPath);

            string externDir = Path.Combine(siPath, "Extern");
            if (!Directory.Exists(externDir)) Directory.CreateDirectory(externDir);

            string rawDir = Path.Combine(siPath, "Raw");
            if (!Directory.Exists(rawDir)) Directory.CreateDirectory(rawDir);

            SIsFSGameFolder = ReplacePathVariables(SIsFSGameFolder);
            string fsGameDir = Path.Combine(siPath, SIsFSGameFolder);
            if (!Directory.Exists(fsGameDir)) Directory.CreateDirectory(fsGameDir);

            this.SIsPath = siPath;
        }
        #endregion

        #region Settings
        private void AfterUpdatedSettings()
        {
            _settingsHash = _targetPlatform.ToString() + _targetConfiguration.ToString() + _isVersionIntFromDate.ToString() + _versionInt + _versionStr;

            XmlDocument doc = new XmlDocument();
            doc.Load(_settingsFile);

            SetSettingInXML(doc, "targetPlatform", _targetPlatform.ToString());
            SetSettingInXML(doc, "targetConfiguration", _targetConfiguration.ToString());
            SetSettingInXML(doc, "isVersionIntFromDate", _isVersionIntFromDate.ToString());
            SetSettingInXML(doc, "versionInt", _versionInt.ToString());
            SetSettingInXML(doc, "versionStr", _versionStr);

            doc.Save(_settingsFile);
        }

        private void SetSettingInXML(XmlDocument doc, string elementName, string elementValue)
        {
            XmlNode element = doc.DocumentElement.SelectSingleNode("/settings/" + elementName);
            if (element == null)
            {
                element = doc.CreateElement(elementName);
                doc.DocumentElement.AppendChild(element);
            }
            element.InnerText = elementValue;
        }

        private string _settingsHash;
        public string SettingsHash { get { return _settingsHash; } }

        private TargetPlatform _targetPlatform;
        public TargetPlatform TargetPlatform { get { return _targetPlatform; } set { _targetPlatform = value; AfterUpdatedSettings(); } }

        private TargetConfiguration _targetConfiguration;
        public TargetConfiguration TargetConfiguration { get { return _targetConfiguration; } set { _targetConfiguration = value; AfterUpdatedSettings(); } }

        #region Version
        private bool _isVersionIntFromDate;
        public bool IsVersionIntFromDate { get { return _isVersionIntFromDate; } set { _isVersionIntFromDate = value; AfterUpdatedSettings(); } }

        private string _versionStr;
        public string GetOrigVersionStr() { return _versionStr; }
        public string VersionStr 
        { 
            get 
            {
                string newVersionStr = _versionStr;
                int startI = newVersionStr.IndexOf('{');
                while (startI != -1)
                {
                    int endI = newVersionStr.IndexOf('}', startI);
                    string content = newVersionStr.Substring(startI + 1, endI - startI - 1);
                    if (content == "VersionInt")
                        content = VersionInt.ToString();
                    else if (content.Contains("DateTime:"))
                        content = DateTime.Now.ToString(content.Substring("DateTime:".Length));
                    else
                        throw new ArgumentException("Unknown string for replace '" + content + "'");

                    newVersionStr = newVersionStr.Remove(startI, endI - startI + 1);
                    newVersionStr = newVersionStr.Insert(startI, content);

                    startI = newVersionStr.IndexOf('{');
                }

                return newVersionStr; 
            }
            set 
            { 
                _versionStr = value; 
                AfterUpdatedSettings(); 
            }
        }

        private int _versionInt;
        public int GetOrigVersionInt() { return _versionInt; }
        public int VersionInt 
        { 
            get 
            {
                if (_isVersionIntFromDate)
                {
                    string str = DateTime.Now.ToString("yyMMdd");
                    return Int32.Parse(str);
                }
                else
                    return _versionInt; 
            } 
            set 
            { 
                _versionInt = value; 
                AfterUpdatedSettings(); 
            } 
        }
        #endregion
        #endregion

        private void ReadSettings()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(_settingsFile);

            foreach (XmlNode curNode in doc.DocumentElement.ChildNodes)
            {
                XmlElement curElem = curNode as XmlElement;
                if (curElem == null)
                    continue;

                switch (curElem.Name)
                {
                    case "scriptInfoDir":
                        SIsFSGameFolder = curElem.InnerText;
                        break;
                    case "sourceRaw":
                        SourceRaw = curElem.GetAttribute("path");
                        if (!String.IsNullOrEmpty(SourceRaw))
                        {
                            _sourceRawFoldersSettings = curElem;
                            BuildSourceRawFolders(SourceRaw, _sourceRawFolders, curElem);
                        }
                        break;
                    case "sourceFSGame":
                        SourceFSGame = curElem.GetAttribute("path");
                        if (!String.IsNullOrEmpty(SourceFSGame))
                        {
                            _sourceFSGameFoldersSettings = curElem;
                            BuildSourceRawFolders(SourceFSGame, _sourceFSGameFolders, curElem);
                        }
                        break;
                    case "outputRaw":
                        _outputRaws.Add(OutputSetting.ParseFromXML(curElem, this));
                        break;
                    case "outputFSGame":
                        _outputFSGames.Add(OutputSetting.ParseFromXML(curElem, this));
                        break;
                    case "targetPlatform":
                        _targetPlatform = (TargetPlatform)Enum.Parse(typeof(TargetPlatform), curElem.InnerText);
                        break;
                    case "targetConfiguration":
                        _targetConfiguration = (TargetConfiguration)Enum.Parse(typeof(TargetConfiguration), curElem.InnerText);
                        break;
                    case "isVersionIntFromDate":
                        _isVersionIntFromDate = Boolean.Parse(curElem.InnerText);
                        break;
                    case "versionInt":
                        _versionInt = String.IsNullOrEmpty(curElem.InnerText) ? 0 : Int32.Parse(curElem.InnerText);
                        break;
                    case "versionStr":
                        _versionStr = curElem.InnerText;
                        break;
                    default:
                        throw new XmlException("Unknown settings node '" + curElem.Name + "'");
                }
            }

            _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "Settings");
            _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "=========================");
            _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "WorkingPath: " + WorkingPath);
            _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "SIs' Path: " + SIsPath);
            _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "SIs' FSGame folder: " + SIsFSGameFolder);
            _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "GamePath: " + GamePath);

            _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "SourceFSGame Path: " + SourceFSGame);

            foreach (string curFolder in _sourceFSGameFolders)
                _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "SourceFSGame: " + curFolder);

            _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "SourceRaw Path: " + SourceRaw);

            foreach (string curFolder in _sourceRawFolders)
                _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "SourceRaw: " + curFolder);

            _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "TargetPlatform: " + TargetPlatform.ToString());
            _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "TargetConfiguration: " + TargetConfiguration.ToString());
            _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "IsVersionIntFromDate: " + IsVersionIntFromDate.ToString());
            _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "VersionInt: " + VersionInt.ToString());
            _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "VersionStr: " + VersionStr);

            _scrManager.Trace.TraceEvent(TraceEventType.Verbose, 0, "=========================");

            _scrManager.Trace.Flush();
        }

        #region Source RAW
        List<string> _sourceRawFolders;
        public List<string> SourceRawFolders { get { return _sourceRawFolders; } }

        string _sourceRaw;
        public string SourceRaw
        {
            get { return _sourceRaw; }
            private set
            {
                value = ReplacePathVariables(value);
                _sourceRaw = GetFullPathFromRelativeOrFull(GamePath, value, false);
                _sourceRawFolders = new List<string>();
            }
        }

        List<string> _sourceFSGameFolders;
        public List<string> SourceFSGameFolders { get { return _sourceFSGameFolders; } }

        string _sourceFSGame;
        public string SourceFSGame
        {
            get { return _sourceFSGame; }
            private set
            {
                value = ReplacePathVariables(value);
                _sourceFSGame = GetFullPathFromRelativeOrFull(GamePath, value, false);
                _sourceFSGameFolders = new List<string>();
            }
        }

        #region SourceRawFolders
        private XmlElement _sourceRawFoldersSettings;
        private XmlElement _sourceFSGameFoldersSettings;

        public void UpdateSourceRawFolders()
        {
            _sourceRawFolders.Clear();
            _sourceFSGameFolders.Clear();

            if (_sourceRawFoldersSettings != null)
                BuildSourceRawFolders(SourceRaw, _sourceRawFolders, _sourceRawFoldersSettings);

            if (_sourceFSGameFoldersSettings != null)
                BuildSourceRawFolders(SourceFSGame, _sourceFSGameFolders, _sourceFSGameFoldersSettings);
        }

        private void BuildSourceRawFolders(string rawFullPath, List<string> folders, XmlElement elem)
        {
            string incFilesStr = elem.GetAttribute("includeFiles");
            bool incFiles = !String.IsNullOrEmpty(incFilesStr) ? Boolean.Parse(incFilesStr) : false;

            string incFoldersStr = elem.GetAttribute("includeSubFolders");
            bool incFolders = !String.IsNullOrEmpty(incFoldersStr) ? Boolean.Parse(incFoldersStr) : false;

            if (incFiles)
                folders.Add("");

            if (incFolders)
            {
                string[] curFolders = Directory.GetDirectories(rawFullPath, "*", SearchOption.AllDirectories);
                folders.AddRange(RemoveRawPathFromSubFolders(rawFullPath, curFolders));
            }
            else
            {
                foreach (XmlNode n in elem.ChildNodes)
                {
                    if (n is XmlElement)
                        BuildSourceRawSubFolders(rawFullPath, "", folders, (XmlElement)n);
                }
            }
        }

        private void BuildSourceRawSubFolders(string rawFullPath, string parentPath, List<string> folders, XmlElement curElem)
        {
            string parentFullPath = Path.Combine(rawFullPath, parentPath);

            string path = curElem.GetAttribute("folder");
            if (String.IsNullOrEmpty(path) || !Directory.Exists(Path.Combine(parentFullPath, path)))
                return;

            string incFoldersStr = curElem.GetAttribute("includeSubFolders");
            bool incFolders = !String.IsNullOrEmpty(incFoldersStr) ? Boolean.Parse(incFoldersStr) : false;

            if (curElem.Name == "includeFiles")
                folders.Add(Path.Combine(parentPath, path));
            else if (curElem.Name != "excludeFiles")
                throw new XmlException("Invalid subFolder element.");

            if (incFolders)
            {
                string[] curFolders = Directory.GetDirectories(Path.Combine(parentFullPath, path), "*", SearchOption.AllDirectories);
                folders.AddRange(RemoveRawPathFromSubFolders(rawFullPath, curFolders));
            }
            else
            {
                foreach (XmlNode n in curElem.ChildNodes)
                {
                    if (n is XmlElement)
                        BuildSourceRawSubFolders(rawFullPath, Path.Combine(parentPath, path), folders, (XmlElement)n);
                }
            }
        }

        private string[] RemoveRawPathFromSubFolders(string rawPath, string[] subFolders)
        {
            string[] outFolders = new string[subFolders.Length];
            for (int i = 0; i < outFolders.Length; i++)
            {
                outFolders[i] = subFolders[i].Substring(rawPath.Length + 1); // delete 'rawPath\'
            }
            return outFolders;
        }
        #endregion

        #endregion

        #region Output RAW
        private List<OutputSetting> _outputRaws = new List<OutputSetting>();
        private List<OutputSetting> _outputFSGames = new List<OutputSetting>();

        private List<OutputSetting> GetOutputListForRaw(RawType raw)
        {
            if (raw == RawType.FSGame)
                return _outputFSGames;
            else if (raw == RawType.Raw)
                return _outputRaws;
            else
                throw new ArgumentException("raw");            
        }

        public OutputSetting GetPrimaryOutput(RawType raw)
        {
            List<OutputSetting> outputs = GetOutputListForRaw(raw);

            // search in outputs, which has defined TargetPlatform and TargetConfiguration
            foreach (OutputSetting cur in outputs)
            {
                if (cur.TargetPlatform == TargetPlatform
                    && cur.TargetConfiguration == TargetConfiguration)
                    return cur;
            }

            // search in outputs, which has defined TargetPlatform
            foreach (OutputSetting cur in outputs)
            {
                if (cur.TargetPlatform == TargetPlatform
                    && cur.TargetConfiguration == TargetConfiguration.None)
                    return cur;
            }

            // search in outputs, which has defined TargetConfiguration
            foreach (OutputSetting cur in outputs)
            {
                if (cur.TargetPlatform == TargetPlatform.None
                    && cur.TargetConfiguration == TargetConfiguration)
                    return cur;
            }

            // get first output without any conditions
            foreach (OutputSetting cur in outputs)
            {
                if (cur.TargetPlatform == TargetPlatform.None
                    && cur.TargetConfiguration == TargetConfiguration.None)
                    return cur;
            }

            return null;
        }

        public List<OutputSetting> GetSecondaryOutputs(RawType raw)
        {
            List<OutputSetting> outputs = GetOutputListForRaw(raw);
            List<OutputSetting> finalList = new List<OutputSetting>(outputs.Count);

            foreach (OutputSetting cur in outputs)
            {
                if (cur.UseAlways)
                    finalList.Add(cur);
            }

            return finalList;
        }
        #endregion
    }

    public class OutputSetting
    {
        public bool UseAlways { get; private set; }

        public TargetPlatform TargetPlatform { get; private set; }
        public TargetConfiguration TargetConfiguration { get; private set; }

        public string IncludeFiles { get; private set; }
        public string ExcludeFiles { get; private set; }

        public string LocalPath { get; private set; }

        public string ExecutePath { get; private set; }

        public bool DeleteDefs { get; private set; }
        public bool DeleteComments { get; private set; }
        public bool DeleteWhite { get; private set; }
        public int LineLengthMin { get; private set; }
        public int LineLengthMax { get; private set; }
        public bool RandomCase { get; private set; }

        private OutputSetting()
        { }

        public static OutputSetting ParseFromXML(XmlElement elem, Settings mainSettings)
        {
            OutputSetting settings = new OutputSetting();

            string useAlways = elem.GetAttribute("useAlways");
            settings.UseAlways = String.IsNullOrEmpty(useAlways) ? false : Boolean.Parse(useAlways);

            string targetPlatform = elem.GetAttribute("targetPlatform");
            if (!String.IsNullOrEmpty(targetPlatform))
                settings.TargetPlatform = (TargetPlatform)Enum.Parse(typeof(TargetPlatform), targetPlatform);

            string targetConfiguration = elem.GetAttribute("targetConfiguration");
            if (!String.IsNullOrEmpty(targetConfiguration))
                settings.TargetConfiguration = (TargetConfiguration)Enum.Parse(typeof(TargetConfiguration), targetConfiguration);

            foreach (XmlNode n in elem)
            {
                XmlElement curElem = n as XmlElement;
                if (curElem == null)
                    continue;

                if (curElem.Name == "includeFiles")
                {
                    settings.IncludeFiles = curElem.InnerText;
                }
                else if (curElem.Name == "excludeFiles")
                {
                    settings.ExcludeFiles = curElem.InnerText;
                }
                if (curElem.Name == "localPath")
                {
                    string path = mainSettings.ReplacePathVariables(curElem.InnerText);
                    settings.LocalPath = Settings.GetFullPathFromRelativeOrFull(mainSettings.GamePath, path, false);
                }
                else if (curElem.Name == "execute")
                {
                    string path = mainSettings.ReplacePathVariables(curElem.InnerText);
                    settings.ExecutePath = Settings.GetFullPathFromRelativeOrFull(mainSettings.GamePath, path, true);
                }
                else if (curElem.Name == "settings")
                {
                    settings.DeleteDefs = Boolean.Parse(curElem.GetAttribute("deleteDefs"));
                    settings.DeleteComments = Boolean.Parse(curElem.GetAttribute("deleteComments"));
                    settings.DeleteWhite = Boolean.Parse(curElem.GetAttribute("deleteWhite"));
                    settings.LineLengthMin = Int32.Parse(curElem.GetAttribute("lineLengthMin"));
                    settings.LineLengthMax = Int32.Parse(curElem.GetAttribute("lineLengthMax"));
                    settings.RandomCase = Boolean.Parse(curElem.GetAttribute("randomCase"));
                }
            }
            return settings;
        }

        public void WriteSF(ScriptFile sf, string content)
        {
            string relativePath = (sf.SFPath + "." + sf.FileExt.ToString()).ToLowerInvariant();
            if (!String.IsNullOrEmpty(LocalPath))
                WriteLocal(relativePath, content);
        }

        private void WriteLocal(string relativePath, string content)
        {
            string fullPath = Path.Combine(LocalPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllText(fullPath, content, ScriptManager.FilesEncoding);
        }

        public void FinishCompile()
        {
            if (!String.IsNullOrEmpty(ExecutePath))
            {
                Process p = new Process();
                p.StartInfo.FileName = ExecutePath;
                p.StartInfo.WorkingDirectory = Path.GetDirectoryName(ExecutePath);
                p.Start();
            }
        }
    }
}
