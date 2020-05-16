using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using CODSCRIPT;

namespace CODSCRIPTNpp
{
    class ThreadSafeList<T>
    {
        List<T> list;
        ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        public ThreadSafeList()
        {
            list = new List<T>();
        }

        public void Add(T e)
        {
            locker.EnterWriteLock();
            try
            {
                list.Add(e);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        public void Remove(T e)
        {
            locker.EnterWriteLock();
            try
            {
                list.Remove(e);
            }
            finally
            {
                locker.ExitWriteLock();
            }            
        }

        public bool Contains(T e)
        {
            locker.EnterReadLock();
            try
            {
                return list.Contains(e);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        public List<T> Clear()
        {
            locker.EnterWriteLock();
            try
            {
                List<T> retList = list;
                list = new List<T>();
                return retList;
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }
    }

    class ThreadedSM
    {
        System.Windows.Threading.Dispatcher _dispatcher;
        Thread lastSFThread = null;

        // list for exclude double reading...
        ThreadSafeList<string> currentSFFiles = new ThreadSafeList<string>();

        // list for updating AssemblyTree
        ThreadSafeList<ScriptFile> _currentUpdatedFiles = new ThreadSafeList<ScriptFile>();
        public ThreadSafeList<ScriptFile> CurrentUpdatedFiles { get { return _currentUpdatedFiles; } }

        private ScriptManager scriptManager;
        public ScriptManager ScriptManager { get { return _isSMLoaded ? scriptManager : null; } }

        private bool _isSMLoaded;
        public bool IsSMLoaded { get { return _isSMLoaded; } }

        public ThreadedSM(ScriptManager sm)
        {
            scriptManager = sm;
            _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        }

        #region ScriptManager Loading
        public void LoadSM()
        {
            _isSMLoaded = false;
            Thread t = new Thread(DoLoadSM);
            t.IsBackground = true;
            t.Name = "Loading SM thread";
            t.Start();
        }

        private void DoLoadSM()
        {
            try
            {
                scriptManager.FindAssemblySFs();
                scriptManager.ReadAssemblySFs(ReadingState.ScriptInfo);
            }
            catch (ThreadAbortException)
            { }
            catch (Exception e)
            {
                Main.NotifyError(e);
            }

            _isSMLoaded = true;
        }
        #endregion

        #region ScriptFile Loading
        public void LoadSF(string fullPath, Action<string, ScriptFile> callback)
        {
            if (currentSFFiles.Contains(fullPath.ToLowerInvariant()))
                return;

            currentSFFiles.Add(fullPath.ToLowerInvariant());

            LoadSFData data = new LoadSFData(fullPath, callback);
            Thread t = new Thread(DoLoadSF);
            t.IsBackground = true;
            t.Name = "Loading SF '" + fullPath + "' thread";
            t.Start(data);

            lastSFThread = t;

            Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "LoadSF, thread " + Thread.CurrentThread.Name + Thread.CurrentThread.GetHashCode());
            Main.Trace.Flush();
        }

        private void DoLoadSF(object data)
        { 
            LoadSFData loadSFData = (LoadSFData)data;
            while (!_isSMLoaded)
                Thread.Sleep(50);

            Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "DoLoadSF(object data) 0, thread " + Thread.CurrentThread.Name + Thread.CurrentThread.GetHashCode());
            Main.Trace.Flush();

            ScriptFile sf = null;

            try
            {
                sf = scriptManager.GetSFFromFullPath(loadSFData.FullPath);
                if (sf != null && (sf.SC == null || sf.ReadingState < ReadingState.CheckCode || sf.IsSourceFileUpdated()))
                {
                    sf.ReadSC();

                    if (sf.SC != null)
                        sf.CheckSC();
                }
            }
            catch (ThreadAbortException)
            { }
            catch (Exception e)
            {
                Main.NotifyError(e);
            }

            Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "DoLoadSF(object data) 1, thread " + Thread.CurrentThread.Name + Thread.CurrentThread.GetHashCode());
            Main.Trace.Flush();

            currentSFFiles.Remove(loadSFData.FullPath.ToLowerInvariant());

            if (sf != null)
                _currentUpdatedFiles.Add(sf);

            //if (lastSFThread == Thread.CurrentThread) // fire event only if this is last SF reading...
                _dispatcher.Invoke(loadSFData.Callback, loadSFData.FullPath, sf);
            //loadSFData.Callback.Invoke(loadSFData.FullPath, sf); // volá funkciu v tomto threade, volanie treba presunúť do hlavného threadu!
        }

        private struct LoadSFData
        {
            public string FullPath;
            public Action<string, ScriptFile> Callback;

            public LoadSFData(string fullPath, Action<string, ScriptFile> callback)
            {
                FullPath = fullPath;
                Callback = callback;
            }
        }

        /// <summary>
        /// Callback will be called for each SF...
        /// </summary>
        /// <param name="fullPaths"></param>
        /// <param name="callback"></param>
        public void LoadSFs(List<string> fullPaths, Action<string, ScriptFile> callback)
        {
            List<ScriptFile> filesForReading = new List<ScriptFile>(fullPaths.Count);
            foreach (string fullPath in fullPaths)
            {
                if (currentSFFiles.Contains(fullPath.ToLowerInvariant()))
                    continue;

                ScriptFile sf = scriptManager.GetSFFromFullPath(fullPath);
                if (sf != null && (sf.SC == null || sf.ReadingState < ReadingState.CheckCode || sf.IsSourceFileUpdated()))
                {
                    filesForReading.Add(sf);
                    currentSFFiles.Add(fullPath.ToLowerInvariant());
                    //Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "PrePreparing for saving SF " + fullPath);
                }
            }

            filesForReading.TrimExcess();

            //foreach (ScriptFile sf in filesForReading)
                //Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "PrePrePreparing for saving SF " + sf.SFFullPath);

            LoadSFsData data = new LoadSFsData(filesForReading, callback);
            Thread t = new Thread(DoLoadSFs);
            t.IsBackground = true;
            t.Name = "Loading SFs thread";
            t.Start(data);
        }

        private void DoLoadSFs(object data)
        {
            LoadSFsData loadSFsData = (LoadSFsData)data;
            while (!_isSMLoaded)
                Thread.Sleep(50);

            Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "DoLoadSFs(object data) 0, thread " + Thread.CurrentThread.Name + Thread.CurrentThread.GetHashCode());
            Main.Trace.Flush();

            //foreach (ScriptFile sf in loadSFsData.SFs)
                //Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "PrePrePrePreparing for saving SF " + sf.SFFullPath);

            try
            {
                scriptManager.ReadAssemblySFs(ReadingState.ScriptCode, loadSFsData.SFs);

                //foreach (ScriptFile sf in loadSFsData.SFs)
                    //Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "PrePrePrePrePreparing for saving SF " + sf.SFFullPath);

                scriptManager.ReadAssemblySFs(ReadingState.CheckCode, loadSFsData.SFs);

                //foreach (ScriptFile sf in loadSFsData.SFs)
                    //Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "PrePrePrePrePrePreparing for saving SF " + sf.SFFullPath);
            }
            catch (ThreadAbortException)
            { }
            catch (Exception e)
            {
                Main.NotifyError(e);
            }

            Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "DoLoadSFs(object data) 1, thread " + Thread.CurrentThread.Name + Thread.CurrentThread.GetHashCode());
            Main.Trace.Flush();

            foreach (ScriptFile sf in loadSFsData.SFs)
            {
                Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "After saving SF " + sf.SFFullPath);

                if (currentSFFiles.Contains(sf.SFFullPath.ToLowerInvariant()))
                    currentSFFiles.Remove(sf.SFFullPath.ToLowerInvariant());

                _currentUpdatedFiles.Add(sf);
            }

            foreach (ScriptFile sf in loadSFsData.SFs)
                _dispatcher.Invoke(loadSFsData.Callback, sf.SFFullPath, sf); // have to be on the end
        }

        private struct LoadSFsData
        {
            public List<ScriptFile> SFs;
            public Action<string, ScriptFile> Callback;

            public LoadSFsData(List<ScriptFile> sfs, Action<string, ScriptFile> callback)
            {
                SFs = sfs;
                Callback = callback;
            }
        }
        #endregion
    }
}
