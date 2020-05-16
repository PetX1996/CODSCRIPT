using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CODSCRIPT;

namespace ScriptPreCompiler
{
    class Compiler
    {
        public Compiler(string[] args)
        {
            ParseSettings(args);
        }

        string _workingDir; // -workingDir=""
        string _settingsFile; // -settingsFile=""

        string _raw; // -raw=""

        bool _verbose; // -verbose
        bool _compareDate; // -compareDate

        string _FSGameFolderName; // -FSGameFolderName=""

        ScriptManager _scrManager;
        MyConsoleListener _consoleListener;

        void ParseSettings(string[] args)
        {
            foreach (string arg in args)
            {
                string[] argOptions = arg.Split('=');
                string argName = argOptions[0];

                switch (argName)
                {
                    case "-verbose":
                        _verbose = true;
                        break;
                    case "-compareDate":
                        _compareDate = true;
                        break;
                    case "-workingDir":
                        _workingDir = argOptions[1];
                        break;
                    case "-settingsFile":
                        _settingsFile = argOptions[1];
                        break;
                    case "-raw":
                        _raw = argOptions[1];
                        break;
                    case "-FSGameFolderName":
                        _FSGameFolderName = argOptions[1];
                        break;
                    default:
                        throw new ApplicationException("Unknown input arg '" + arg + "'");
                }
            }

            if (_verbose)
            {
                Console.WriteLine("-verbose " + _verbose.ToString());
                Console.WriteLine("-compareDate " + _compareDate.ToString());
                Console.WriteLine("-workingDir " + _workingDir);
                Console.WriteLine("-settingsFile " + _settingsFile);
                Console.WriteLine("-raw " + _raw);
                Console.WriteLine("-FSGameFolderName " + _FSGameFolderName);
            }
        }

        bool CompileAssembly()
        {
            int curProgress = 0;
            int getFilesPart = 10;
            int readSCPart = 20;
            int checkSCPart = 20;
            int compilePart = 50;

            int errorsCount = 0;
            int warningsCount = 0;
            bool successful = true;

            _scrManager = ScriptManager.Create(_workingDir, _settingsFile, _FSGameFolderName);
            if (_scrManager == null)
                throw new ApplicationException("Could not create ScriptManager...probably bad workingDir");

            _scrManager.FindAssemblySFs();

            _scrManager.ReadAssemblySFs(ReadingState.ScriptInfo);

            _consoleListener = new MyConsoleListener(_verbose);
            _scrManager.Trace.Listeners.Add(_consoleListener);

            RawType raw = (RawType)Enum.Parse(typeof(RawType), _raw);

            List<ScriptFile> files = _scrManager.CompileAssemblySFs_GetFiles(raw, _compareDate);
            ReportProgress(curProgress += getFilesPart, "Getting files for compile finished...");

            if (files.Count == 0)
                return true;

            if (!successful)
                return false;


            successful = _scrManager.CompileAssemblySFs_ReadSC(files, ref errorsCount, ref warningsCount);
            ReportProgress(curProgress += readSCPart, "Reading SC finished with " + errorsCount + " errors and " + warningsCount + " warnings");

            if (!successful)
                return false;


            successful = _scrManager.CompileAssemblySFs_CheckSC(files, ref errorsCount, ref warningsCount);
            ReportProgress(curProgress += checkSCPart, "Checking SC finished with " + errorsCount + " errors and " + warningsCount + " warnings");

            if (!successful)
                return false;


            int compilePartPerFile = compilePart / files.Count;
            foreach (ScriptFile sf in files)
            {
                if (!_scrManager.CompileAssemblySF_Compile(sf))
                    return false;
            }

            _scrManager.CompileAssemblySF_Finish(raw);
            ReportProgress(100, "Compiling finished with " + errorsCount + " errors and " + warningsCount + " warnings");
            return true;
        }

        void ReportProgress(int percentage, string message)
        {
            _scrManager.Trace.TraceEvent(TraceEventType.Information, 0, "============================");
            _scrManager.Trace.TraceEvent(TraceEventType.Information, 0, percentage + " %");
            _scrManager.Trace.TraceEvent(TraceEventType.Information, 0, message);
            _scrManager.Trace.TraceEvent(TraceEventType.Information, 0, "============================");
        }

        public void Compile()
        {
            if (CompileAssembly())
                Console.WriteLine("Successfully compiled.");
            else
                Console.WriteLine("Could not compile.");
        }

        class MyConsoleListener : TraceListener
        {
            bool _verbose;

            public MyConsoleListener(bool verbose)
            {
                this.Name = "Trace";
                this._verbose = verbose;
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

                if (!_verbose && eventType == TraceEventType.Verbose)
                    return;

                string message;
                if (args.Length > 0 && !String.IsNullOrEmpty(args[0].ToString()))
                    message = String.Format(format, args);
                else
                    message = format;

                Console.WriteLine(message);
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
    }
}
