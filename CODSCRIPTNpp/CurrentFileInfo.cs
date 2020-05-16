using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CODSCRIPT;

namespace CODSCRIPTNpp
{
    class CurrentFileEventArgs : EventArgs
    {
        public string FullPath { get; private set; }
        public ScriptFile SF { get; private set; }

        public CurrentFileEventArgs(string fullPath, ScriptFile sf)
        {
            FullPath = fullPath;
            SF = sf;
        }
    }

    class CurrentFileInfo
    {
        private ThreadedSM threadedSM;

        public string FullPath { get; private set; }

        public bool IsLoaded { get; private set; }
        public ScriptFile SF { get; private set; }

        public event EventHandler<CurrentFileEventArgs> OnFileUpdated;
        public event EventHandler<CurrentFileEventArgs> OnFileLoaded;

        public CurrentFileInfo(ThreadedSM threadedSM)
        {
            this.threadedSM = threadedSM;
        }

        public void Update(string fullPath)
        {
            FullPath = fullPath;
            SF = null;
            IsLoaded = false;

            if (OnFileUpdated != null)
                OnFileUpdated(this, new CurrentFileEventArgs(fullPath, null));

            Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Update(string fullPath), thread " + Thread.CurrentThread.Name + Thread.CurrentThread.GetHashCode());
            Main.Trace.Flush();

            threadedSM.LoadSF(fullPath, Callback_OnFileLoaded);
        }

        public void OnFileSaved(List<string> fullPaths)
        {
            threadedSM.LoadSFs(fullPaths, Callback_OnFileLoaded);
        }

        private void Callback_OnFileLoaded(string fullPath, ScriptFile sf)
        {
            Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Callback_OnFileLoaded, thread " + Thread.CurrentThread.Name + Thread.CurrentThread.GetHashCode());
            Main.Trace.Flush();

            if (fullPath.ToLowerInvariant() == FullPath.ToLowerInvariant())
            {
                SF = sf;

                IsLoaded = true;

                if (OnFileLoaded != null)
                    OnFileLoaded(this, new CurrentFileEventArgs(fullPath, SF));
            }
        }
    }
}
