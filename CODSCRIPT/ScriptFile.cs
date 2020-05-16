using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace CODSCRIPT
{
    public enum ReadingState
    {
        UnRead,
        ScriptInfo,
        ScriptCode,
        CheckCode,
        Error
    };

    public enum CompilingState
    {
        UnCompile,
        PrepareCompile,
        CompileMembers,
        CompileCode,
        CompileOutput
    };

    /// <summary>
    /// Súbor so scriptom, obsahuje základné informácie.
    /// </summary>
    public class ScriptFile
    {
        public enum Extension
        {
            GSC,
            GSX
        };

        private ScriptManager manager;
        public ScriptManager Manager { get { return manager; } }

        public bool IsExtern { get { return isExtern; } }
        private bool isExtern;
        private RawType _raw;
        public RawType RawType { get { return _raw; } }
        public string SFPath { get; private set; }
        public string SFFullPath { get { return _scFullPath; } }

        private string _siFullPath;
        private string _scFullPath;

        private Extension _extension;
        public Extension FileExt { get { return _extension; } }

        public ScriptInfo SI { get; private set; }
        public ScriptInfo SecondSI { get; private set; }

        public ScriptCode SC { get; private set; }

        private string _scSettingsHash;

        public ReadingState ReadingState { get; private set; }

        public ErrorCollection Errors { get; private set; }

        public CompilingState CompilingState { get; private set; }
        public ScriptFile OriginalOverwriteFile { get; private set; }
        private bool _isOverwritten;

        public bool Compile_IsExcluded { get; set; }

        private ScriptFile()
        { }

        #region Copy for overwrite using...
        public ScriptFile CreateCopyForOverwrite(ScriptFile refSF)
        {
            if (SC == null || isExtern)
                return null;

            ScriptFile sf = new ScriptFile();
            sf.manager = manager;
            sf.isExtern = isExtern;
            sf._raw = _raw;

            sf._siFullPath = _siFullPath;

            string fileName = Path.GetFileName(SFPath) + "_" + refSF.SFPath.GetHashCode();
            sf.SFPath = Path.Combine(Path.GetDirectoryName(SFPath), fileName);
            sf._scFullPath = _scFullPath.Replace(SFPath, sf.SFPath);

            sf._extension = _extension;
            sf.SI = SI.CreateCopy(sf);
            sf.SC = SC.CreateCopyForOverwrite(sf);
            sf._scSettingsHash = _scSettingsHash;
            sf.ReadingState = ReadingState;
            sf.CompilingState = CODSCRIPT.CompilingState.UnCompile;
            sf.Errors = Errors;
            sf._isOverwritten = true;
            sf.OriginalOverwriteFile = this;
            return sf;
        }
        #endregion

        #region Creating File(File System)
        public static ScriptFile Create(ScriptManager manager, string SFPath, Extension ext, bool isExtern)
        {
            ScriptFile sf = new ScriptFile();
            sf.manager = manager;
            sf.isExtern = isExtern;
            sf.SFPath = SFPath;
            sf._extension = ext;

            sf.Errors = new ErrorCollection();

            manager.Trace.TraceEvent(TraceEventType.Verbose, 0, "Creating SF '{0}.{1}'", SFPath, ext.ToString());
            //throw new ArgumentException("manager");

            if (isExtern)
            {
                sf._raw = RawType.Extern;
                sf._siFullPath = GetSIFullPath(SFPath, RawType.Extern, ext, manager);
                sf._scFullPath = null;

                if (!File.Exists(sf._siFullPath))
                    return null;
            }
            else
            {
                string SCFSGameFullPath = GetSCFullPath(SFPath, RawType.FSGame, ext, sf.manager);
                string SCRawFullPath = GetSCFullPath(SFPath, RawType.Raw, ext, sf.manager);

                sf._raw = GetBetterRawFile(SCFSGameFullPath, SCRawFullPath);
                if (sf._raw == RawType.FSGame)
                    sf._scFullPath = SCFSGameFullPath;
                else if (sf._raw == RawType.Raw)
                    sf._scFullPath = SCRawFullPath;
                else
                    return null;

                sf._siFullPath = GetSIFullPath(SFPath, sf._raw, ext, sf.manager);
            }

            return sf;
        }

        private static string GetSIFullPath(string SFPath, RawType raw, ScriptFile.Extension ext, ScriptManager manager)
        {
            string rawPath = raw.ToString();
            if (raw == RawType.FSGame)
                rawPath = manager.Settings.SIsFSGameFolder;

            return Path.Combine(Path.Combine(manager.Settings.SIsPath, rawPath), SFPath + "." + ext.ToString() + ".xml");
        }

        private static string GetSCFullPath(string SFPath, RawType raw, Extension ext, ScriptManager manager)
        {
            if (raw == RawType.Extern)
                throw new ArgumentException("raw");

            string relativePathOnly = Path.GetDirectoryName(SFPath);
            string rawStr = null;
            if (raw == RawType.FSGame && !String.IsNullOrEmpty(manager.Settings.SourceFSGame))
            {
                if (manager.Settings.SourceFSGameFolders.Contains(relativePathOnly))
                    rawStr = manager.Settings.SourceFSGame;
            }
            else if (raw == RawType.Raw && !String.IsNullOrEmpty(manager.Settings.SourceRaw))
            {
                if (manager.Settings.SourceRawFolders.Contains(relativePathOnly))
                    rawStr = manager.Settings.SourceRaw;
            }

            if (!String.IsNullOrEmpty(rawStr))
            {
                string fullPath;
                if (ext == Extension.GSX)
                {
                    fullPath = Path.Combine(rawStr, SFPath + "." + Extension.GSX.ToString());
                    if (File.Exists(fullPath))
                        return fullPath;
                }
                else
                {
                    fullPath = Path.Combine(rawStr, SFPath + "." + Extension.GSC.ToString());
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }

            return null;
        }

        private static RawType GetBetterRawFile(string fsGameFullPath, string rawFullPath)
        {
            if (!String.IsNullOrEmpty(fsGameFullPath))
                return RawType.FSGame;
            else if (!String.IsNullOrEmpty(rawFullPath))
                return RawType.Raw;
            else
                return RawType.None;
        }
        #endregion

        #region Reading ScriptInfo/ScriptCode
        public bool IsSourceFileUpdated()
        {
            if (!File.Exists(_scFullPath))
                return false;

            if (SC != null && SC.CreateTime < File.GetLastWriteTime(_scFullPath))
                return true;

            if (_scSettingsHash != manager.Settings.SettingsHash) // updated platform, ...
                return true;

            return false;
        }

        /// <summary>
        /// Reads base info(functions, constants, etc.)
        /// </summary>
        public void ReadSI()
        {
            manager.Trace.TraceEvent(TraceEventType.Verbose, 0, "Trying to read SI, SF: '{0}'", SFPath);

            // read ScriptCode
            if (SI == null
                && !File.Exists(_siFullPath))
            {
                ReadSC();

                //if (SI != null)
                //SI.WriteInfoToFile(SIFullPath);

                return;
            }

            // XML exists and it is newer
            if (SI == null
                && File.Exists(_siFullPath)
                && (!File.Exists(_scFullPath) || File.GetLastWriteTime(_scFullPath) < File.GetLastWriteTime(_siFullPath)))
            {
                ResetSI();

                bool successfully;
                SI = ScriptInfo.ReadFromFile(this, this._siFullPath, out successfully);
                SecondSI = SI;
                if (successfully)
                    ReadingState = ReadingState.ScriptInfo;
                else
                {
                    ReadingState = ReadingState.Error;
                    ResetSI();
                }

                manager.Trace.TraceEvent(TraceEventType.Verbose, 0, "SI have been successfully read from XML, SF: '{0}'", SFPath);
                CompilingState = CODSCRIPT.CompilingState.UnCompile;

                return;
            }

            // SourceCode has been updated...
            if (File.Exists(_siFullPath)
                && File.Exists(_scFullPath)
                && (SC == null || IsSourceFileUpdated()))
            {
                ReadSC();

                //if (SI != null)
                //SI.WriteInfoToFile(SIFullPath);

                return;
            }
            // else žiadna zmena -> použi načítané dáta
        }

        /// <summary>
        /// Reads the code. Checks syntax.
        /// </summary>
        public void ReadSC()
        {
            manager.Trace.TraceEvent(TraceEventType.Verbose, 0, "Trying to read SC, SF: '{0}'", SFPath);

            if (String.IsNullOrEmpty(_scFullPath))
                throw new InvalidOperationException("SCFullPath is undefined!");

            if (SC == null || IsSourceFileUpdated())
            {
                DateTime createTime = DateTime.Now;

                ResetSI();
                Errors.Clear();
                CompilingState = CODSCRIPT.CompilingState.UnCompile;

                SI = ScriptInfo.Create(this, this.SFPath, false, createTime);
                SC = ScriptCode.Create(this, _scFullPath, createTime);

                if (SC.ReadCode())
                {
                    manager.Trace.TraceEvent(TraceEventType.Verbose, 0, "SC have been successfully read, SF '{0}'", SFPath);

                    _scSettingsHash = manager.Settings.SettingsHash;

                    ReadingState = ReadingState.ScriptCode;
                    SI.WriteInfoToFile(_siFullPath);

                    SecondSI = SI;
                }
                else
                {
                    manager.Trace.TraceEvent(TraceEventType.Verbose, 0, "SC have been unsuccessfully read, SF '{0}'", SFPath);

                    ReadingState = ReadingState.Error;
                    SI.WriteInfoToFile(_siFullPath);

                    ResetSI();
                    SC = null;
                }

                Console.WriteLine("Memory: " + GC.GetTotalMemory(false));
                if (GC.GetTotalMemory(false) > 300000000)
                {
                    Console.WriteLine("Memory before collect: " + GC.GetTotalMemory(false));
                    GC.Collect();
                    Console.WriteLine("Memory after collect: " + GC.GetTotalMemory(false));
                }
            }// else žiadna zmena -> použi uložené dáta 
        }

        public void ResetSI()
        {
            SI = null;
        }

        /// <summary>
        /// Checks the code semantics.
        /// </summary>
        public void CheckSC()
        {
            if (SC == null)
                throw new InvalidOperationException("SC");

            if (ReadingState >= ReadingState.CheckCode)
                return;

            SC.CheckCode();

            SI.WriteInfoToFile(_siFullPath);
            ReadingState = ReadingState.CheckCode;
            CompilingState = CODSCRIPT.CompilingState.UnCompile;
        }
        #endregion

        #region Compiling ScriptCode
        public bool PrepareCompileSC()
        {
            //if (SC == null || ReadingState < ReadingState.CheckCode)
                //throw new InvalidOperationException("SC");

            if (CompilingState >= CompilingState.PrepareCompile)
                return true;

            manager.Trace.TraceEvent(TraceEventType.Verbose, 0, "PrepareCompile " + this.ToString());

            // read SC if has not read
            if (ReadingState < ReadingState.ScriptCode)
                ReadSC();
            if (ReadingState < ReadingState.CheckCode)
                CheckSC();

            if (ReadingState == ReadingState.Error || Errors.AnyErrors)
            {
                manager.Trace.TraceEvent(TraceEventType.Error, 0, "Could not compile " + this.ToString());
                return false;
            }

            // compile...
            if (!SC.PrepareCompile())
            {
                manager.Trace.TraceEvent(TraceEventType.Verbose, 0, "PrepareCompile " + this.ToString() + " was unsuccessfully");
                return false;
            }

            CompilingState = CODSCRIPT.CompilingState.PrepareCompile;
            manager.Trace.TraceEvent(TraceEventType.Verbose, 0, "PrepareCompile " + this.ToString() + " was successfully");
            return true;
        }

        public bool CompileMembersSC()
        {
            if (CompilingState >= CompilingState.CompileMembers)
                return true;

            manager.Trace.TraceEvent(TraceEventType.Verbose, 0, "CompileMembers " + this.ToString());

            if (!SC.CompileMembers())
            {
                manager.Trace.TraceEvent(TraceEventType.Verbose, 0, "CompileMembers " + this.ToString() + " was unsuccessfully");
                return false;
            }

            if (!_isOverwritten)
            {
                SI.IsCompiled = true;
                SI.WriteInfoToFile(_siFullPath); // constant Values has been modified...
            }

            CompilingState = CODSCRIPT.CompilingState.CompileMembers;
            manager.Trace.TraceEvent(TraceEventType.Verbose, 0, "CompileMembers " + this.ToString() + " was successfully");
            return true;
        }

        public void CompileCodeSC()
        {
            if (CompilingState >= CompilingState.CompileCode)
                return;

            manager.Trace.TraceEvent(TraceEventType.Verbose, 0, "CompileCode " + this.ToString());

            SC.CompileCode();

            CompilingState = CODSCRIPT.CompilingState.CompileCode;
        }

        public string CompileOutputSC()
        {
            return CompileOutputSC(false);
        }

        public string CompileOutputSC(bool onlyCopy)
        {
            manager.Trace.TraceEvent(TraceEventType.Verbose, 0, "CompileOutput " + this.ToString());

            OutputSetting mainOutput = manager.Settings.GetPrimaryOutput(_raw);

            string output;
            if (onlyCopy)
                output = File.ReadAllText(SFFullPath, ScriptManager.FilesEncoding);
            else
                output = SC.CompileOutput(mainOutput);

            mainOutput.WriteSF(this, output);

            foreach (OutputSetting outputSetting in manager.Settings.GetSecondaryOutputs(_raw))
            {
                if (!onlyCopy)
                    output = SC.CompileOutput(outputSetting);

                outputSetting.WriteSF(this, output);
            }

            CompilingState = CODSCRIPT.CompilingState.CompileOutput;

            return output;
        }
        #endregion

        #region SF tools
        public int GetMemberStartPos(IMemberInfo member, ref int length)
        {
            if (SC == null)
                return -1;

            return SC.GetMemberStartPos(member, ref length);
        }
        #endregion

        public override string ToString()
        {
            return "SF '" + SFPath + "." + _extension.ToString() + "'";
        }
    }
}
