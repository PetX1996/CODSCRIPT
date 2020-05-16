using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CODSCRIPT;
using NppPluginNET;
using System.Drawing;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;

namespace CODSCRIPTNpp
{
    // http://www.scintilla.org/ScintillaDoc.html
    internal class FileManager
    {
        private static FileManager fileManager;

        private ThreadedSM threadedSM;
        public ThreadedSM ThreadedSM { get { return threadedSM; } }

        private CurrentFileInfo _currentFileInfo;
        public CurrentFileInfo CurrentFileInfo { get { return _currentFileInfo; } }

        public ScriptManager ScriptManager { get { return threadedSM.ScriptManager; } }

        public ScriptFile CurrentFile { get { return _currentFileInfo.SF; } }

        private FileManager()
        {}

        public static FileManager Create(string workingDir, string settingsFile, string fsgameFolderName)
        {
            Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Creating file manager, thread " + Thread.CurrentThread.Name);
            Main.Trace.Flush();

            //MessageBox.Show("Creating ScriptManager");
            FileManager fManager = new FileManager();

            ScriptManager scrManager = ScriptManager.Create(workingDir, settingsFile, fsgameFolderName);
            if (scrManager == null)
            {
                MessageBox.Show("Bad game path. Please, set working dir to <gamePath>\\bin\\CODSCRIPT");
                return null;
            }

            fManager.threadedSM = new ThreadedSM(scrManager);
            fManager.threadedSM.LoadSM();

            fManager._currentFileInfo = new CurrentFileInfo(fManager.threadedSM);
            fManager._currentFileInfo.OnFileLoaded += new EventHandler<CurrentFileEventArgs>(fManager.UpdateCurrentFile);

            fileManager = fManager;
            return fManager;
        }

        bool showErrorMessage = false;
        public void OnFileSwitch(string fullPath, bool updateTree, bool showErrors)
        {
            showErrorMessage = showErrors;

            Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Switching file, thread " + Thread.CurrentThread.Name + Thread.CurrentThread.GetHashCode());
            Main.Trace.Flush();
            _currentFileInfo.Update(fullPath);
        }

        public void OnFileSaved(List<string> fullPaths)
        {
            _currentFileInfo.OnFileSaved(fullPaths);
        }

        public void UpdateCurrentFile(object sender, CurrentFileEventArgs args)
        {
            Main.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Updating info, thread " + Thread.CurrentThread.Name + Thread.CurrentThread.GetHashCode());
            Main.Trace.Flush();

            if (args.SF == null)
                return;

            CheckErrors();

            //MessageBox.Show("Readed..");

            if (Main.MainDlg != null && Main.MainDlg.Visible)
                Main.MainDlg.UpdateInfo(this);


            if (targetGoToPos != null && targetGoToPos.SF == args.SF)
                targetGoToPos.Go();

            targetGoToPos = null;

            UpdateNppInfoList();

            Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GRABFOCUS, 0, 0);

            if (showErrorMessage && Main.Save_ShowErrorMsgBox)
            {
                if (args.SF.Errors.AnyErrors)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (Error e in args.SF.Errors.GetOnlyErrors())
                        sb.Append(e.FullMessage + Environment.NewLine);

                    MessageBox.Show(sb.ToString(), "Error list");
                }
            }
        }

        public void OnCharAdded(int newChar)
        {
            char newCh = (char)newChar;

            if (CurrentFile == null)
                return;

            #region XMLDoc
            if (newChar == '/' && CheckXMLDoc())
            {
                PlaceXMLDoc();
                return;
            }
            #endregion

            #region OptionalParams
            if (newChar == ']')
                PlaceOptionalParams();
            #endregion

            #region CallTip
            if (newCh == '(')
            {
                int curPos = (int)Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GETCURRENTPOS, 0, 0);
                
                string memberName;
                string memberPath;
                if (GetMemberNameByIndex(curPos - 1, out memberName, out memberPath))
                {
                    if (!String.IsNullOrEmpty(memberName))
                    {
                        IMemberInfo member = null;
                        if (memberPath == null) // funcName
                        {
                            if (CurrentFile.SecondSI != null)
                            {
                                List<IMemberInfo> members = CurrentFile.SecondSI.GetAvailableMembers();
                                member = members.Find(a => a.Name.EqualCode(memberName));
                            }
                        }
                        else if (memberPath == string.Empty) // ::funcName
                        {
                            if (CurrentFile.SecondSI != null)
                            {
                                List<IMemberInfo> members = CurrentFile.SecondSI.GetIncludesMembers(true);
                                members.AddRange(CurrentFile.SecondSI.GetLocalMembers(false));
                                member = members.Find(a => a.Name.EqualCode(memberName));
                            }
                        }
                        else
                        {
                            // check using
                            if (CurrentFile.SecondSI != null)
                            {
                                UsingInfo tryUsing = CurrentFile.SecondSI.FindUsing(memberPath);
                                if (tryUsing != null)
                                    memberPath = tryUsing.SFPath;
                            }

                            ScriptFile sf = ScriptManager.GetSF(memberPath);
                            if (sf != null && sf.SecondSI != null)
                                member = sf.SecondSI.FindLocalFunc(memberName);
                        }

                        if (member != null && member is FuncInfo)
                        {
                            int pos = (int)Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GETCURRENTPOS, 0, 0);
                            ShowCallTip((FuncInfo)member, null, pos, null, 0);

                            //ShowCallTip(member);
                            //MessageBox.Show("CallTip: " + member.ToString());
                        }
                    }
                }
            }
            else if (newCh == ',')
            {
                fileManager.ScriptManager.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Comma pre-start");
                fileManager.ScriptManager.Trace.Flush();

                int paramI;
                string memberName;
                string memberPath;
                if (GetFuncNameAndParamBehindComma(out memberName, out memberPath, out paramI))
                {
                    if (!String.IsNullOrEmpty(memberName))
                    {
                        IMemberInfo member = null;
                        if (memberPath == null) // funcName
                        {
                            if (CurrentFile.SecondSI != null)
                            {
                                List<IMemberInfo> members = CurrentFile.SecondSI.GetAvailableMembers();
                                member = members.Find(a => a.Name.EqualCode(memberName));
                            }
                        }
                        else if (memberPath == string.Empty) // ::funcName
                        {
                            if (CurrentFile.SecondSI != null)
                            {
                                List<IMemberInfo> members = CurrentFile.SecondSI.GetIncludesMembers(true);
                                members.AddRange(CurrentFile.SecondSI.GetLocalMembers(false));
                                member = members.Find(a => a.Name.EqualCode(memberName));
                            }
                        }
                        else
                        {
                            // check using
                            if (CurrentFile.SecondSI != null)
                            {
                                UsingInfo tryUsing = CurrentFile.SecondSI.FindUsing(memberPath);
                                if (tryUsing != null)
                                    memberPath = tryUsing.SFPath;
                            }

                            ScriptFile sf = ScriptManager.GetSF(memberPath);
                            if (sf != null && sf.SecondSI != null)
                                member = sf.SecondSI.FindLocalFunc(memberName);
                        }

                        if (member != null && member is FuncInfo)
                        {
                            fileManager.ScriptManager.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Comma start");
                            fileManager.ScriptManager.Trace.Flush();

                            int pos = (int)Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GETCURRENTPOS, 0, 0);
                            ShowCallTip((FuncInfo)member, null, pos, null, paramI);

                            fileManager.ScriptManager.Trace.TraceEvent(System.Diagnostics.TraceEventType.Verbose, 0, "Comma end");
                            fileManager.ScriptManager.Trace.Flush();
                        }
                    }
                }
            }
            else if (newCh == ')')
            {
                if ((int)Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_CALLTIPACTIVE, 0, 0) == 1)
                    Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_CALLTIPCANCEL, 0, 0);
            }
            #endregion
            
            #region AutoCompletion
            if (IsValidName(newChar) || newCh == ':')
            {
                IntPtr scintilla = PluginBase.GetCurrentScintilla();

                int curPos = (int)Win32.SendMessage(scintilla, SciMsg.SCI_GETCURRENTPOS, 0, 0);

                string memberName; 
                string memberPath;
                if (GetMemberNameByIndex(curPos, out memberName, out memberPath))
                {
                    RegisterAutoCImages();

                    Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSETIGNORECASE, 1, 0);
                    Win32.SendMessage(scintilla, SciMsg.SCI_AUTOCSHOW, memberName.Length, BuildAutoCList(memberName, memberPath));
                    //MessageBox.Show(BuildAutoCList(name));
                }
            }
            #endregion
        }

        #region NppElementInfo
        // ScriptFile.Errors.ErrorInfo.NppInfo
        // ScriptFile.SI.References.NppInfo
        // ScriptFile.SI.References[FuncRefInfo].Arguments.NppInfo
        // ScriptFile.SI.Usings.NppInfo
        // ScriptFile.SI.Constants.NppInfo
        // ScriptFile.SI.Functions.NppInfo
        // ScriptFile.SI.Functions.LocalVars.NppInfo

        private List<NppElementInfo> _nppInfoList;
        private void UpdateNppInfoList()
        {
            _nppInfoList = new List<NppElementInfo>();

            foreach (Error e in CurrentFile.Errors)
                _nppInfoList.Add(e.ErrorInfo.NppInfo);

            foreach (IReferenceInfo r in CurrentFile.SecondSI.References)
            {
                _nppInfoList.Add(r.NppInfo);
                if (r is FuncRefInfo && ((FuncRefInfo)r).Arguments != null)
                    foreach (FuncRefArgInfo a in ((FuncRefInfo)r).Arguments)
                        _nppInfoList.Add(a.NppInfo);
            }

            foreach (UsingInfo u in CurrentFile.SecondSI.Usings)
                _nppInfoList.Add(u.NppInfo);

            foreach (ConstInfo c in CurrentFile.SecondSI.Constants)
                _nppInfoList.Add(c.NppInfo);

            foreach (FuncInfo f in CurrentFile.SecondSI.Functions)
            {
                _nppInfoList.Add(f.NppInfo);
                if (f.LocalVars != null)
                    foreach (LocalVarInfo l in f.LocalVars)
                        _nppInfoList.Add(l.NppInfo);
            }
        }

        public void OnModified(SCNotification notify)
        {
            if (CurrentFile == null || NppNotification.IsFileBeforeLoad || _nppInfoList == null)
                return;

            bool inserted = false;
            if ((notify.modificationType & (uint)SciMsg.SC_MOD_BEFOREINSERT) > 0)
                inserted = true;
            else if ((notify.modificationType & (uint)SciMsg.SC_MOD_BEFOREDELETE) > 0)
                inserted = false;
            else
                return;

            //MessageBox.Show(inserted + ";" + notify.position + ";" + notify.length);

            foreach (NppElementInfo e in _nppInfoList)
            {
                if (inserted)
                    e.Inserted(notify.position, notify.length);
                else
                    e.Deleted(notify.position, notify.length);
            }
        }
        #endregion

        #region Optional Params
        private void PlaceOptionalParams()
        {
            if (CurrentFile == null)
                return;

            IntPtr scintilla = PluginBase.GetCurrentScintilla();
            int endIndex = (int)Win32.SendMessage(scintilla, SciMsg.SCI_GETCURRENTPOS, 0, 0) - 1;

            int bufferSize = 512;
            int startPos = endIndex - bufferSize;
            startPos = startPos < 0 ? 0 : startPos;
            bufferSize = endIndex - startPos;

            string buffer = PluginBase.GetTextRange(startPos, bufferSize);
            if (CheckOptionalParam(buffer))
                SetNextParamsAsOptional(endIndex + 1);
        }

        // DO THE MAGIC!!!
        private bool CheckOptionalParam(string buffer)
        {
            bool endSpace = true;
            bool word = true;
            bool startSpace = true;
            //bool startBracket = true;
            bool preSpace = true;

            for (int i = buffer.Length - 1; i >= 0; i--)
            {
                int c = buffer[i];

                /*MessageBox.Show(String.Format("c = {0}\n endSpace = {1}\n word = {2}\n startSpace = {3}\n preSpace = {4}", 
                    (char)c, 
                    endSpace.ToString(), 
                    word.ToString(), 
                    startSpace.ToString(), 
                    preSpace.ToString()));
                */
                if (endSpace)
                    if (Char.IsWhiteSpace((char)c))
                        continue;
                    else if (Char.IsLetterOrDigit((char)c))
                        endSpace = false;
                    else
                        return false;
                else if (word)
                    if (Char.IsLetterOrDigit((char)c))
                        continue;
                    else if (Char.IsWhiteSpace((char)c))
                        word = false;
                    else if (c == '[')
                    {
                        word = false;
                        startSpace = false;
                    }
                    else
                        return false;
                else if (startSpace)
                    if (Char.IsWhiteSpace((char)c))
                        continue;
                    else if (c == '[')
                        startSpace = false;
                    else
                        return false;
                else if (preSpace)
                    if (Char.IsWhiteSpace((char)c))
                        continue;
                    else if (c == ',' || c == '(')
                        return true;
                    else
                        return false;
                else
                    return false;
            }
            return false;
        }

        // and miracles occur..
        private void SetNextParamsAsOptional(int startPos)
        {
            IntPtr scintilla = PluginBase.GetCurrentScintilla();
            int docLength = (int)Win32.SendMessage(scintilla, SciMsg.SCI_GETLENGTH, 0, 0);
            int bufferSize = 512;
            int endIndex = startPos + bufferSize;
            endIndex = endIndex >= docLength ? docLength - 1 : endIndex;
            bufferSize = endIndex - startPos;
            string buffer = PluginBase.GetTextRange(startPos, bufferSize);

            StringBuilder finalStr = new StringBuilder();

            // " , param"
            // " , [ param ]"
            bool postSpace = true;
            //bool postComma = true;
            bool preSpace = true;
            //bool preBracket = true;
            bool preBracketSpace = true;
            bool word = true;
            //bool postBracketSpace = true;
            //bool postBracket = true;

            int i;
            for (i = 0; i < buffer.Length; i++)
            {
                int c = buffer[i];
                finalStr.Append((char)c);

                if (postSpace)
                    if (Char.IsWhiteSpace((char)c))
                        continue;
                    else if (c == ',')
                    {
                        postSpace = false;
                        //postComma = false;
                    }
                    else if (c == ')')
                        break;
                    else
                        break;
                else if (preSpace)
                    if (Char.IsWhiteSpace((char)c))
                        continue;
                    else if (c == '[')
                    {
                        preSpace = false;
                        //preBracket = false;
                    }
                    else if (Char.IsLetterOrDigit((char)c))
                    {
                        preSpace = false;
                        //preBracket = false;
                        preBracketSpace = false;
                        finalStr.Insert(finalStr.Length - 1, '[');
                    }
                    else
                        break;
                else if (preBracketSpace)
                    if (Char.IsWhiteSpace((char)c))
                        continue;
                    else if (Char.IsLetterOrDigit((char)c))
                        preBracketSpace = false;
                    else
                        break;
                else if (word)
                    if (Char.IsLetterOrDigit((char)c))
                        continue;
                    else if (Char.IsWhiteSpace((char)c))
                        continue;
                    else if (c == ']' || c == ',' || c == ')')
                    {
                        if (c == ',' || c == ')')
                        {
                            finalStr.Remove(finalStr.Length - 1, 1);
                            finalStr.Append(']');
                            i--; // process again ',' and ')'
                        }

                        // reset settings and repeat...
                        postSpace = true;
                        preSpace = true;
                        preBracketSpace = true;
                        word = true;
                    }
                    else
                        break;
                else
                    break;
            }

            Win32.SendMessage(scintilla, SciMsg.SCI_SETSEL, startPos, startPos + i + 1);
            Win32.SendMessage(scintilla, SciMsg.SCI_REPLACESEL, 0, finalStr.ToString());
            //Win32.SendMessage(scintilla, SciMsg.SCI_SETSEL, pos + selPos, pos + selPos);
        }
        #endregion

        #region XMLDoc
        private bool CheckXMLDoc()
        {
            IntPtr scintilla = PluginBase.GetCurrentScintilla();
            int endIndex = (int)Win32.SendMessage(scintilla, SciMsg.SCI_GETCURRENTPOS, 0, 0) - 1;

            int bufferSize = 3;
            int startPos = endIndex - bufferSize;
            startPos = startPos < 0 ? 0 : startPos;
            bufferSize = endIndex - startPos;

            string buffer = PluginBase.GetTextRange(startPos, bufferSize);
            return (bufferSize == 3 && Char.IsWhiteSpace(buffer[0]) && buffer[1] == '/' && buffer[2] == '/');
        }

        private void PlaceXMLDoc()
        {
            //MessageBox.Show("XML");

            int pos = (int)Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GETCURRENTPOS, 0, 0) - 3;
            if (CurrentFile != null && CurrentFile.SC != null)
            {
                IMemberInfo member = CurrentFile.SC.GetMemberForXMLDoc(pos);
                if (member != null)
                {
                    //MessageBox.Show("member: " + member.ToString());
                    int selPos;
                    string text = GetXMLDoc(member, out selPos);
                    ReplaceXMLDoc(pos, text, selPos);
                }
            }
        }

        private void ReplaceXMLDoc(int pos, string xmlDoc, int selPos)
        { 
            IntPtr scintilla = PluginBase.GetCurrentScintilla();
            Win32.SendMessage(scintilla, SciMsg.SCI_SETSEL, pos, pos + 3);
            Win32.SendMessage(scintilla, SciMsg.SCI_REPLACESEL, 0, xmlDoc);
            Win32.SendMessage(scintilla, SciMsg.SCI_SETSEL, pos + selPos, pos + selPos);
        }

        private string GetXMLDoc(IMemberInfo member, out int selPos)
        {
            if (member is ConstInfo)
                return GetXMLDoc((ConstInfo)member, out selPos);
            else if (member is FuncInfo)
                return GetXMLDoc((FuncInfo)member, out selPos);
            else
                throw new ArgumentException("member");
        }

        private string GetXMLDoc(ConstInfo member, out int selPos)
        {
            StringBuilder sb = new StringBuilder();
            //sb.Append("/// <summary>\n");
            sb.Append("/// ");
            selPos = sb.Length;
            //sb.Append("\n");
            //sb.Append("/// </summary>");
            return sb.ToString();
        }

        private string GetXMLDoc(FuncInfo member, out int selPos)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("/// <summary>\n");
            sb.Append("/// ");
            selPos = sb.Length;
            sb.Append("\n");
            sb.Append("/// </summary>\n");
            sb.Append("/// <returns></returns>\n");
            foreach (FuncParamInfo p in member.Parameters)
                sb.Append("/// <param name=\"" + p.Name + "\"></param>\n");

            return sb.ToString().TrimEnd();
        }
        #endregion

        #region DWELL - mouse hover
        public void OnDWellStart(int pos)
        {
            //MessageBox.Show("Dwell: " + pos);
            if (pos >= 0 && CurrentFile != null)
            {
                Error error = GetErrorByPos(pos);
                IReferenceInfo info = GetReferenceByPos(pos);
                if (info != null && error != null)
                    ShowCallTip(info, error, pos);
                else if (info != null)
                    ShowCallTip(info, null, pos);
                else if (error != null)
                    ShowCallTip(error, pos);
            }
        }

        public void OnDWellEnd(int pos)
        {
            IntPtr scintilla = PluginBase.GetCurrentScintilla();
            if ((int)Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPACTIVE, 0, 0) > 0)
            {
                Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPCANCEL, 0, 0);

                //MessageBox.Show("end");
            }
        }

        private Error GetErrorByPos(int pos)
        {
            foreach (Error e in CurrentFile.Errors)
            {
                if (pos >= e.ErrorInfo.NppInfo.CharIndex
                    && pos < (e.ErrorInfo.NppInfo.CharIndex + e.ErrorInfo.NppInfo.CharLength))
                    return e;
            }
            return null;
        }

        private IReferenceInfo GetReferenceByPos(int pos)
        {
            if (CurrentFile.SecondSI == null)
                return null;

            List<IReferenceInfo> foundRefs = new List<IReferenceInfo>();
            foreach (IReferenceInfo r in CurrentFile.SecondSI.References)
            {
                if (pos >= r.NppInfo.CharIndex
                    && pos < (r.NppInfo.CharIndex + r.NppInfo.CharLength))
                {
                    foundRefs.Add(r);
                }
            }

            if (foundRefs.Count == 0)
                return null;

            // find minimal length
            IReferenceInfo minR = foundRefs[0];
            //StringBuilder sb = new StringBuilder();
            foreach (IReferenceInfo r in foundRefs)
            {
                //sb.Append(r.ToString() + ";" + r.CharIndex + ";" + r.CharLength + Environment.NewLine);

                if (r.NppInfo.CharLength < minR.NppInfo.CharLength)
                    minR = r;
            }

            //MessageBox.Show(sb.ToString());
            return minR;
        }

        private IMemberInfo GetDefinitionByPos(int pos)
        {
            if (CurrentFile.SecondSI == null)
                return null;

            return CurrentFile.SecondSI.GetMemberDefinitionAtPos(pos);
        }
        #endregion

        #region CallTip showing
        private void ShowCallTip(IReferenceInfo reference, Error error, int pos)
        {
            IntPtr scintilla = PluginBase.GetCurrentScintilla();

            if (reference.Definition == null)
                return;

            if (reference is ConstRefInfo)
            {
                if (error != null)
                    Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPSHOW, pos,
                        GetCallTipText((ConstInfo)reference.Definition) + Environment.NewLine + Environment.NewLine + GetCallTipText(error));
                else
                    Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPSHOW, pos, GetCallTipText((ConstInfo)reference.Definition));
            }
            else if (reference is UsingRefInfo)
            {
                if (error != null)
                    Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPSHOW, pos,
                        GetCallTipText((UsingInfo)reference.Definition) + Environment.NewLine + Environment.NewLine + GetCallTipText(error));
                else
                    Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPSHOW, pos, GetCallTipText((UsingInfo)reference.Definition));
            }
            else if (reference is FuncRefInfo)
            {
                FuncRefInfo funcRef = (FuncRefInfo)reference;
                int? hltParam = null;
                for (int i = 0; funcRef.Arguments != null && i < funcRef.Arguments.Count; i++)
                {
                    FuncRefArgInfo argInfo = funcRef.Arguments[i];
                    if (pos >= argInfo.NppInfo.CharIndex && pos < (argInfo.NppInfo.CharIndex + argInfo.NppInfo.CharLength))
                        hltParam = i;
                }

                FuncInfo definition = (FuncInfo)funcRef.Definition;
                bool isAllArguments = (funcRef.Arguments == null
                    || (definition.OptParamStartIndex != null && funcRef.Arguments.Count < definition.OptParamStartIndex)
                    || (definition.OptParamStartIndex == null && funcRef.Arguments.Count < definition.Parameters.Count));

                //MessageBox.Show(funcRef.MemberInfo.ToString());
                ShowCallTip((FuncInfo)funcRef.Definition, error, pos, isAllArguments ? null : (int?)funcRef.Arguments.Count, hltParam);
            }
            else
                throw new ArgumentException("reference");
        }

        private void ShowCallTip(FuncInfo func, Error error, int pos, int? paramsCount, int? hltParam)
        {
            int? hltStart, hltEnd;
            string callTip = GetCallTipText(func, paramsCount, hltParam, out hltStart, out hltEnd);

            if (error != null)
                callTip += Environment.NewLine + Environment.NewLine + GetCallTipText(error);

            ShowCallTip(callTip, pos, hltStart, hltEnd);
        }

        private void ShowCallTip(Error error, int pos)
        {
            ShowCallTip(GetCallTipText(error), pos, null, null);
        }

        private void ShowCallTip(string message, int pos, int? hltStart, int? hltEnd)
        {
            IntPtr scintilla = PluginBase.GetCurrentScintilla();
            //MessageBox.Show(callTip);

            Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPSHOW, pos, message);

            // TODO: pri zafarbení sa niekedy nezobrazí...WTF?!
            //Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPSETFOREHLT, PluginBase.GetColorCode(Color.Black), 0);
            if (hltStart != null && hltEnd != null)
                Win32.SendMessage(scintilla, SciMsg.SCI_CALLTIPSETHLT, (int)hltStart, (int)hltEnd);            
        }
        #endregion

        #region CallTip Text
        public static string GetCallTipText(ConstInfo constInfo)
        {
            StringBuilder str = new StringBuilder();
            str.Append(constInfo.Name + "\n");
            str.Append(constInfo.Summary);
            str.Append("Value: " + constInfo.OriginalValue);
            return str.ToString().Trim();
        }

        public static string GetCallTipText(UsingInfo usingInfo)
        {
            StringBuilder str = new StringBuilder();
            str.Append(usingInfo.SFPath);
            return str.ToString().Trim();
        }

        public static string GetCallTipText(FuncInfo func, int? paramsCount)
        {
            int? hltStart, hltEnd;
            return GetCallTipText(func, paramsCount, null, out hltStart, out hltEnd);
        }

        public static string GetCallTipText(FuncInfo func, int? paramsCount, int? hltParam, out int? hltStart, out int? hltEnd)
        {
            hltStart = null;
            hltEnd = null;

            if (hltParam != null && hltParam >= func.Parameters.Count)
                hltParam = null;

            if (paramsCount != null && paramsCount > func.Parameters.Count)
                paramsCount = null;

            int paramsTotalCount = func.Parameters.Count;
            
            if (paramsCount == null)
                paramsCount = func.OptParamStartIndex != null ? (int)func.OptParamStartIndex : paramsTotalCount;

            if (hltParam != null && paramsCount < hltParam + 1) // hltParam is optional param
                paramsCount = (int)hltParam + 1;

            StringBuilder sb = new StringBuilder();

            sb.Append(func.GetHead(paramsCount, hltParam, out hltStart, out hltEnd));
            sb.Append("\n");

            if (!String.IsNullOrEmpty(func.Summary))
                sb.Append(func.Summary + "\n");

            sb.Append("\n");

            if (!String.IsNullOrEmpty(func.Returns))
                sb.Append("Returns: " + func.Returns + "\n");
            if (!String.IsNullOrEmpty(func.Self))
                sb.Append("Self: " + func.Self + "\n");
            if (!String.IsNullOrEmpty(func.Example))
                sb.Append("Example: " + func.Example + "\n");

            sb.Append("\n");

            if (hltParam != null)
            {
                FuncParamInfo param = func.Parameters[(int)hltParam];
                string summary = func.Parameters[(int)hltParam].GetSummary();
                if (!String.IsNullOrEmpty(summary))
                    sb.Append(summary + "\n");
            }
            else
            {
                string summary;
                for (int i = 0; i < paramsCount; i++)
                {
                    summary = func.Parameters[i].GetSummary();
                    if (!String.IsNullOrEmpty(summary))
                        sb.Append(summary + "\n");
                }
            }

            return sb.ToString().TrimEnd();
        }

        public static string GetCallTipText(Error error)
        {
            StringBuilder sb = new StringBuilder();
            if (error is SyntaxError || error is SemanticError)
                sb.Append("Error:\n");
            else
                sb.Append("Warning:\n");
            sb.Append(error.Message);
            return sb.ToString();
        }
        #endregion

        #region Getting Func Name
        // primitívne vyhľadávanie....netokenizuje!!!!
        // možný konflikt so stringami, komentármi a iným bordelom...
        private static bool GetMemberNameByIndex(int endIndex, out string memberName, out string memberPath)
        {
            IntPtr scintilla = PluginBase.GetCurrentScintilla();

            // cancels when inserting to existing name...
            int nextCharI = endIndex;
            int docLength = (int)Win32.SendMessage(scintilla, SciMsg.SCI_GETLENGTH, 0, 0);
            if (nextCharI < docLength)
            {
                string nextChar = PluginBase.GetTextRange(nextCharI, 1);
                if (IsValidName(nextChar[0]))
                {
                    memberName = null;
                    memberPath = null;
                    return false;
                }
            }

            int bufferSize = 512;
            int startPos = endIndex - bufferSize;
            startPos = startPos < 0 ? 0 : startPos;
            bufferSize = endIndex - startPos;

            string buffer = PluginBase.GetTextRange(startPos, bufferSize);
            //string buffer = "p";
            //MessageBox.Show(buffer);

            return GetMemberName(buffer, out memberName, out memberPath);
        }

        private bool GetFuncNameAndParamBehindComma(out string memberName, out string memberPath, out int paramIndex)
        {
            IntPtr scintilla = PluginBase.GetCurrentScintilla();
            int curPos = (int)Win32.SendMessage(scintilla, SciMsg.SCI_GETCURRENTPOS, 0, 0) - 1;
            int bufferSize = 512;
            int startPos = curPos - bufferSize;
            startPos = startPos < 0 ? 0 : startPos;
            bufferSize = curPos - startPos;

            string buffer = PluginBase.GetTextRange(startPos, bufferSize);
           
            int commasCount;
            int openBracket = GetOpenBracketIndex(buffer, out commasCount);

            paramIndex = commasCount + 1;
            if (openBracket != -1)
                return GetMemberName(buffer.Substring(0, openBracket), out memberName, out memberPath);

            memberName = null;
            memberPath = null;
            return false;
        }

        private static bool GetMemberName(string str, out string memberName, out string memberPath)
        {
            bool isValidName = true;
            bool isStarted = false;
            int nameStartPos = -1;
            int nameEndPos = -1;

            if (str.Length > 1 && str[str.Length - 1] == ':' && str[str.Length - 2] == ':')
            {
                memberName = string.Empty;
                memberPath = GetMemberPath(str, str.Length);
                return (memberPath != null);
            }
            else
            {
                for (int i = str.Length - 1; i >= 0; i--)
                {
                    int cur = str[i];
                    if (!isStarted)
                    {
                        if (Char.IsWhiteSpace((char)cur))
                            continue;

                        if (IsValidName(cur))
                        {
                            isStarted = true;
                            nameEndPos = i + 1;
                            continue;
                        }
                        else
                        {
                            isValidName = false;
                            break;
                        }
                    }
                    else
                    {
                        if (IsValidName(cur))
                            continue;
                        else
                        {
                            nameStartPos = i + 1;
                            break;
                        }
                    }
                }

                if (isValidName && nameStartPos != -1 && nameEndPos != -1)
                {
                    string name = str.Substring(nameStartPos, nameEndPos - nameStartPos);
                    if (name.Length > 0 && !(name[0] > '0' && name[0] < '9'))
                    {
                        memberName = name;
                        memberPath = GetMemberPath(str, nameStartPos);
                        return true;
                    }
                }

                memberName = null;
                memberPath = null;
                return false;
            }
        }

        private static string GetMemberPath(string buffer, int nameStartPos)
        {
            int pathEndPos = -1;
            int pathStartPos = -1;

            bool isOperatorStarted = false;
            bool isOperatorEnded = false;
            bool isPathStarted = false;
            for (int i = nameStartPos - 1; i >= 0; i--)
            {
                int cur = buffer[i];
                if (!isOperatorStarted)
                {
                    if (Char.IsWhiteSpace((char)cur))
                        continue;
                    else if (cur == ':')
                        isOperatorStarted = true;
                    else
                        return null; // other char than ':'
                }
                else if (!isOperatorEnded)
                {
                    if (cur == ':')
                        isOperatorEnded = true;
                    else
                        return null; // other char than ':'
                }
                else if (!isPathStarted)
                {
                    if (Char.IsWhiteSpace((char)cur))
                        continue;
                    else if (IsValidPath(cur))
                    {
                        isPathStarted = true;
                        pathEndPos = i + 1;
                    }
                    else
                        return string.Empty; // ::funcName
                }
                else
                {
                    if (IsValidPath(cur))
                        continue;
                    else
                    {
                        pathStartPos = i + 1;
                        break; // path::funcName
                    }
                }
            }

            int length = pathEndPos - pathStartPos;
            if (pathStartPos >= 0 && length > 0)
                return buffer.Substring(pathStartPos, length);

            return null;
        }

        private static int GetOpenBracketIndex(string str, out int commasCount)
        {
            commasCount = 0;
            int level = 1;
            for (int i = str.Length - 1; i >= 0; i--)
            {
                if (str[i] == ')')
                    level++;
                else if (str[i] == '(')
                    level--;
                else if (str[i] == ',' && level == 1)
                    commasCount++;

                if (level == 0)
                    return i;
            }
            return -1;
        }

        private static bool IsValidName(int curChar)
        {
            return (curChar >= '0' && curChar <= '9') 
                || (curChar >= 'a' && curChar <= 'z') 
                || (curChar >= 'A' && curChar <= 'Z') 
                || (curChar == '_'); 
        }

        private static bool IsValidPath(int curChar)
        {
            return IsValidName(curChar) || curChar == '\\';
        }

        private static bool IsExternFuncOperator(int curChar, int nextChar)
        { 
            return curChar == ':' && nextChar == ':';
        }
        #endregion

        #region Autocompletion images
        private void RegisterAutoCImages()
        {
            if (String.IsNullOrEmpty(Main.xpmFuncPublic))
            {
                Main.xpmFuncPublic = PluginBase.PNG2XPM(new Bitmap(Path.Combine(Main.PluginSubFolder, "member_function.png")));
                Main.xpmFuncPrivate = PluginBase.PNG2XPM(new Bitmap(Path.Combine(Main.PluginSubFolder, "member_function_private.png")));
                Main.xpmConstPublic = PluginBase.PNG2XPM(new Bitmap(Path.Combine(Main.PluginSubFolder, "member_constant.png")));
                Main.xpmConstPrivate = PluginBase.PNG2XPM(new Bitmap(Path.Combine(Main.PluginSubFolder, "member_constant_private.png")));
                Main.xpmConstSealed = PluginBase.PNG2XPM(new Bitmap(Path.Combine(Main.PluginSubFolder, "member_constant_sealed.png")));
                Main.xpmUsingPublic = PluginBase.PNG2XPM(new Bitmap(Path.Combine(Main.PluginSubFolder, "member_using.png")));
                Main.xpmUsingPrivate = PluginBase.PNG2XPM(new Bitmap(Path.Combine(Main.PluginSubFolder, "member_using_private.png")));
                Main.xpmLocalVar = PluginBase.PNG2XPM(new Bitmap(Path.Combine(Main.PluginSubFolder, "member_variable.png")));

                IntPtr scintilla = PluginBase.GetCurrentScintilla();
                Win32.SendMessage(scintilla, SciMsg.SCI_REGISTERIMAGE, 0, Main.xpmFuncPublic);
                Win32.SendMessage(scintilla, SciMsg.SCI_REGISTERIMAGE, 1, Main.xpmFuncPrivate);
                Win32.SendMessage(scintilla, SciMsg.SCI_REGISTERIMAGE, 2, Main.xpmConstSealed);
                Win32.SendMessage(scintilla, SciMsg.SCI_REGISTERIMAGE, 3, Main.xpmConstPublic);
                Win32.SendMessage(scintilla, SciMsg.SCI_REGISTERIMAGE, 4, Main.xpmConstPrivate);
                Win32.SendMessage(scintilla, SciMsg.SCI_REGISTERIMAGE, 5, Main.xpmUsingPublic);
                Win32.SendMessage(scintilla, SciMsg.SCI_REGISTERIMAGE, 6, Main.xpmUsingPrivate);
                Win32.SendMessage(scintilla, SciMsg.SCI_REGISTERIMAGE, 7, Main.xpmLocalVar);
            }
        }
        #endregion

        #region AutoCompletion
        private string BuildAutoCList(string memberName, string memberPath)
        {
            int pos = (int)Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GETCURRENTPOS, 0, 0);
            List<IMemberInfo> members = new List<IMemberInfo>();

            StringBuilder sb = new StringBuilder();
            string upperName = memberName.ToUpper();

            if (memberPath == null) // funcName
            {
                if (CurrentFile.SecondSI != null)
                {
                    members = CurrentFile.SecondSI.GetAvailableMembers(pos);
                    members.Add(new LocalVarInfo(null, "level", -1, -1, null, null));
                    members.Add(new LocalVarInfo(null, "game", -1, -1, null, null));
                    members.Add(new UsingInfo(null, "COMPILER", "", MemberAccess.Public, null));
                }
            }
            else if (memberPath == string.Empty) // ::funcName
            {
                List<IMemberInfo> allMembers = new List<IMemberInfo>();
                if (CurrentFile.SecondSI != null)
                {
                    allMembers = CurrentFile.SecondSI.GetIncludesMembers(true);
                    allMembers.AddRange(CurrentFile.SecondSI.GetLocalMembers(false));
                }

                foreach (IMemberInfo m in allMembers)
                    if (m is FuncInfo)
                        members.Add(m);
            }
            else // path::funcName
            {
                if (CurrentFile.SecondSI != null)
                {
                    UsingInfo info = CurrentFile.SecondSI.FindUsing(memberPath);
                    if (info != null)
                        memberPath = info.SFPath;
                }

                ScriptFile sf = ScriptManager.GetSF(memberPath);
                if (sf != null && sf.SecondSI != null)
                    members = sf.SecondSI.GetLocalMembers(true);
            }

            List<IMemberInfo> selectedMembers = new List<IMemberInfo>(members.Count);
            foreach (IMemberInfo m in members)
            {
                if (upperName == string.Empty || m.Name.ToUpper().Contains(upperName))
                    selectedMembers.Add(m);
            }

            selectedMembers.Sort((a, b) => a.Name.CompareTo(b.Name));

            foreach (IMemberInfo m in selectedMembers)
            {
                int image = -1;
                if (m is FuncInfo && ((FuncInfo)m).Access == MemberAccess.Public)
                    image = 0;
                else if (m is FuncInfo)
                    image = 1;
                else if (m is ConstInfo && ((ConstInfo)m).Sealed)
                    image = 2;
                else if (m is ConstInfo && ((ConstInfo)m).Access == MemberAccess.Public)
                    image = 3;
                else if (m is ConstInfo)
                    image = 4;
                else if (m is UsingInfo && ((UsingInfo)m).Access == MemberAccess.Public)
                    image = 5;
                else if (m is UsingInfo)
                    image = 6;
                else if (m is LocalVarInfo)
                    image = 7;

                sb.Append(m.Name + "?" + image + " ");
            }

            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }
        #endregion

        #region GoToDefinition & FindAllReferences
        public void FindAllReferences()
        {
            if (CurrentFile == null)
                return;

            IntPtr scintilla = PluginBase.GetCurrentScintilla();
            int curPos = (int)Win32.SendMessage(scintilla, SciMsg.SCI_GETCURRENTPOS, 0, 0);
            IMemberInfo definition = null;

            IReferenceInfo reference = GetReferenceByPos(curPos);
            if (reference != null)
                definition = reference.Definition;
            else
                definition = GetDefinitionByPos(curPos);

            if (definition != null)
                FindAllReferences(definition);
        }

        public void FindAllReferences(IMemberInfo definition)
        {
            if (Main.MainDlg == null || !Main.MainDlg.Visible)
                Main.myDockableDialog();

            List<IReferenceInfo> references = definition.FindAllReferences();

            //MessageBox.Show("Refs count " + references.Count);
            Main.MainDlg.UpdateAssemblyTree(references, definition);
        }

        /// <summary>
        /// Prejde na definíciu referencie z kontextového menu.
        /// </summary>
        public void GoToDefinition()
        {
            if (CurrentFile == null)
                return;

            IntPtr scintilla = PluginBase.GetCurrentScintilla();
            int curPos = (int)Win32.SendMessage(scintilla, SciMsg.SCI_GETCURRENTPOS, 0, 0);
            IReferenceInfo reference = GetReferenceByPos(curPos);

            if (reference != null && reference.Definition != null)
                GoToDefinition(reference.Definition);
        }

        public static void GoToDefinition(IMemberInfo member)
        {
            if (!(member is FuncInfo) && !(member is ConstInfo) && !(member is UsingInfo))
                throw new ArgumentException("member");

            if (!member.SF.IsExtern)
            {
                GoToPosition(member);
            }
            else
            {
                string text = string.Empty;
                if (member is FuncInfo)
                    text = GetCallTipText((FuncInfo)member, null);
                else if (member is ConstInfo)
                    text = GetCallTipText((ConstInfo)member);
                else if (member is UsingInfo)
                    text = GetCallTipText((UsingInfo)member);

                MessageBox.Show(text.Trim(), "Could not find");

                Win32.SendMessage(PluginBase.GetCurrentScintilla(), SciMsg.SCI_GRABFOCUS, 0, 0);
            }
        }

        private class TargetGoToPos
        {
            public ScriptFile SF { get; private set; }
            public int? pos;
            public int? length;
            private IMemberInfo member;
            private IReferenceInfo refInfo;

            public TargetGoToPos(ScriptFile sf, int? pos, int? length, IMemberInfo member)
                : this(sf, pos, length)
            {
                this.member = member;
            }

            public TargetGoToPos(ScriptFile sf, int? pos, int? length, IReferenceInfo refInfo)
                : this(sf, pos, length)
            {
                this.refInfo = refInfo;
            }

            public TargetGoToPos(ScriptFile sf, int? pos, int? length)
            {
                SF = sf;
                this.pos = pos;
                this.length = length;                
            }

            public void Go()
            {
                if (member != null)
                {
                    int length = -1;
                    int pos = member.SF.GetMemberStartPos(member, ref length);
                    if (pos >= 0)
                    {
                        this.pos = pos;
                        this.length = length;

                        if (Main.MainDlg != null && Main.MainDlg.Visible)
                            Main.MainDlg.SelectMemberInTree(member);
                    }
                }

                if (refInfo != null)
                {
                    if (Main.MainDlg != null && Main.MainDlg.Visible)
                        Main.MainDlg.SelectMemberInTree(refInfo);
                }

                if (this.pos != null && this.length != null)
                    GoToPosition((int)this.pos, (int)this.length);

                //MessageBox.Show("TargetGoToPos.Go()");
            }
        }

        private TargetGoToPos targetGoToPos = null;

        /// <summary>
        /// Prejde na pozíciu v konkrétnom súbore.
        /// </summary>
        /// <param name="sf"></param>
        /// <param name="pos"></param>
        /// <param name="length"></param>
        public static void GoToPosition(ScriptFile sf, int pos, int length, IReferenceInfo refInfo)
        {
            //MessageBox.Show("FileManager.GoToPosition(ScriptFile sf, int pos, int length, IReferenceInfo refInfo)");

            if (fileManager != null && !sf.IsExtern)
            {
                if (fileManager.CurrentFile != null
                    && fileManager.CurrentFile == sf)
                {
                    GoToPosition(pos, length);
                    return;
                }
                else
                {
                    fileManager.targetGoToPos = new TargetGoToPos(sf, pos, length, refInfo);
                    Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DOOPEN, 0, sf.SFFullPath);
                    return;
                }
            }

            MessageBox.Show(String.Format("Could not go to position '{0}' in file '{1}'", pos, sf.SFFullPath));
        }

        /// <summary>
        /// Prejde na pozíciu v konkrétnom súbore.
        /// </summary>
        /// <param name="sf"></param>
        /// <param name="pos"></param>
        /// <param name="length"></param>
        public static void GoToPosition(ScriptFile sf, int pos, int length)
        {
            //MessageBox.Show("FileManager.GoToPosition(ScriptFile sf, int pos, int length)");

            if (!sf.IsExtern)
            {
                if (fileManager != null
                    && fileManager.CurrentFile != null
                    && fileManager.CurrentFile == sf)
                {
                    GoToPosition(pos, length);
                    return;
                }
                else if (fileManager != null
                    && fileManager.CurrentFile != null)
                {
                    fileManager.targetGoToPos = new TargetGoToPos(sf, pos, length);
                    Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DOOPEN, 0, sf.SFFullPath);
                    return;
                }
            }

            MessageBox.Show(String.Format("Could not go to position '{0}' in file '{1}'", pos, sf.SFFullPath));
        }

        public static void GoToPosition(IMemberInfo member)
        {
            //MessageBox.Show("FileManager.GoToPosition(IMemberInfo member)");

            if (!member.SF.IsExtern)
            {
                if (fileManager != null
                    && fileManager.CurrentFile != null
                    && fileManager.CurrentFile == member.SF)
                {
                    int length = -1;
                    int pos = member.SF.GetMemberStartPos(member, ref length);
                    if (pos >= 0)
                    {
                        GoToPosition(pos, length);
                        return;
                    }
                }
                else if (fileManager != null
                    && fileManager.CurrentFile != null)
                {
                    fileManager.targetGoToPos = new TargetGoToPos(member.SF, null, null, member);
                    Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_DOOPEN, 0, member.SF.SFFullPath);
                    return;
                }
            }

            MessageBox.Show(String.Format("Could not go to member '{0}' in file '{1}'", member.ToString(), member.SF.SFFullPath));
        }

        private static void GoToPosition(int pos, int length)
        {
            IntPtr scintilla = PluginBase.GetCurrentScintilla();
            Win32.SendMessage(scintilla, SciMsg.SCI_SETSEL, pos+length, pos+length);
            Win32.SendMessage(scintilla, SciMsg.SCI_SETSEL, pos, pos);
            Win32.SendMessage(scintilla, SciMsg.SCI_GRABFOCUS, 0, 0);
        }
        #endregion

        #region ERRORS
        private void CheckErrors()
        {
            // resets last errors
            PluginBase.ResetIndicatorInBuffer(0);
            PluginBase.ResetIndicatorInBuffer(1);
            PluginBase.ResetIndicatorInBuffer(2);
            // build indicators
            PluginBase.SetIndicator(0, (int)SciMsg.INDIC_SQUIGGLE, Color.Red, 256);
            PluginBase.SetIndicator(1, (int)SciMsg.INDIC_SQUIGGLE, Color.Blue, 256);
            PluginBase.SetIndicator(2, (int)SciMsg.INDIC_SQUIGGLE, Color.Green, 256);

            foreach (Error error in _currentFileInfo.SF.Errors)
                HighLightError(error);
        }

        private void HighLightError(Error error)
        {
            int indic = 0;
            if (error is SyntaxError)
                indic = 0;
            else if (error is SemanticError)
                indic = 1;
            else if (error is WarningError)
                indic = 2;
            else
                throw new ArgumentException("error");

            PluginBase.UseIndicator(indic, error.ErrorInfo.CurCharIndex, error.ErrorInfo.CurCharLength);
            //MessageBox.Show("s: " + error.Info.CurCharIndex + " l: " + error.Info.CurCharLength);
        }
        #endregion
    }
}
