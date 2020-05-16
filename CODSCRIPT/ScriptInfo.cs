using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Diagnostics;

namespace CODSCRIPT
{
    public enum MemberAccess
    { 
        Public,
        Private
    };

    public class ScriptInfo
    {
        private ScriptFile sf;
        public ScriptFile SF { get { return sf; } }

        public string Name { get; private set; }

        public bool IsGlobal { get; private set; }
        public DateTime CreateTime { get; private set; }

        public List<IncludeInfo> Includes { get; private set; }
        public List<UsingInfo> Usings { get; private set; }
        public List<ConstInfo> Constants { get; private set; }
        public List<FuncInfo> Functions { get; private set; }

        public List<IReferenceInfo> References { get; private set; }

        public bool IsCompiled { get; set; }

        private ScriptInfo(ScriptFile SF)
        {
            Includes = new List<IncludeInfo>();
            Usings = new List<UsingInfo>();
            Constants = new List<ConstInfo>();
            Functions = new List<FuncInfo>();
            References = new List<IReferenceInfo>();
            this.sf = SF;
        }

        public static ScriptInfo Create(ScriptFile SF, string name, bool isGlobal, DateTime createTime)
        {
            ScriptInfo si = new ScriptInfo(SF);
            si.Name = name;
            si.IsGlobal = isGlobal;
            si.CreateTime = createTime;

            return si;
        }

        /// <summary>
        /// Create deep copy of SI and Constants, but shallow copy of all other attributes!
        /// </summary>
        /// <param name="sf"></param>
        /// <returns></returns>
        public ScriptInfo CreateCopy(ScriptFile sf)
        {
            ScriptInfo si = new ScriptInfo(sf);
            si.Name = this.Name;
            si.IsGlobal = this.IsGlobal;
            si.CreateTime = this.CreateTime;

            si.Includes = this.Includes;
            si.Usings = this.Usings;
            //si.Constants = this.Constants;
            //si.Functions = this.Functions;
            si.References = this.References;

            foreach (ConstInfo c in this.Constants)
                si.Constants.Add(c.CreateCopy(this.sf));

            foreach (FuncInfo f in this.Functions)
                si.Functions.Add(f.CreateCopy(this.sf));

            return si;
        }

        #region Read Info from XML
        public static ScriptInfo ReadFromFile(ScriptFile SF, string fileFullPath, out bool successfully)
        {
            ScriptInfo SI = new ScriptInfo(SF);
            SI.CreateTime = DateTime.Now;

            XmlDocument doc = new XmlDocument();
            doc.Load(fileFullPath);

            successfully = true;

            foreach (XmlNode curNode in doc.DocumentElement.ChildNodes)
            {
                if (!(curNode is XmlElement))
                    continue;

                XmlElement curElem = (XmlElement)curNode;

                if (curElem.Name == "settings")
                {
                    SI.IsGlobal = Boolean.Parse(curElem.GetAttribute("global"));

                    string successfullyStr = curElem.GetAttribute("successfully");
                    successfully = (String.IsNullOrEmpty(successfullyStr) || successfullyStr == "True");

                    string isCompiledStr = curElem.GetAttribute("compiled");
                    SI.IsCompiled = String.IsNullOrEmpty(isCompiledStr) ? false : Boolean.Parse(isCompiledStr);

                    foreach (XmlNode settingNode in curElem.ChildNodes)
                    {
                        if (!(settingNode is XmlElement))
                            continue;

                        XmlElement settingElem = (XmlElement)settingNode;

                        if (settingElem.Name == "name")
                            SI.Name = settingElem.InnerText;
                        else
                            throw new XmlException("Unknown ScriptInfo settings node '" + settingElem.Name + "'");
                    }
                }
                else if (curElem.Name == "content")
                {
                    foreach (XmlNode contentNode in curElem.ChildNodes)
                    {
                        if (!(contentNode is XmlElement))
                            continue;

                        XmlElement contentElem = (XmlElement)contentNode;

                        IReferenceInfo refInfo;
                        switch (contentElem.Name)
                        {
                            case "include":
                                ReadInclude(SI, contentElem);
                                break;
                            case "using":
                                ReadUsing(SI, contentElem);
                                break;
                            case "const":
                                ReadConst(SI, contentElem);
                                break;
                            case "func":
                                ReadFunc(SI, contentElem);
                                break;
                            case "usingRef":
                                refInfo = UsingRefInfo.FromXML(contentElem, SI);
                                if (refInfo != null)
                                    SI.References.Add(refInfo);

                                break;
                            case "constRef":
                                refInfo = ConstRefInfo.FromXML(contentElem, SI);
                                if (refInfo != null)
                                    SI.References.Add(refInfo);

                                break;
                            case "funcRef":
                                refInfo = FuncRefInfo.FromXML(contentElem, SI);
                                if (refInfo != null)
                                    SI.References.Add(refInfo);

                                break;
                            case "error":
                                Error error = Error.FromXML(contentElem, SF);
                                if (error != null)
                                    SF.Errors.Add(error);

                                break;
                            default:
                                throw new XmlException("Unknown ScriptInfo content node '" + contentElem.Name + "'");
                        }
                    }
                }
                else
                    throw new XmlException("Unknown ScriptInfo node '" + curElem.Name + "'");
            }
            return SI;
        }

        private static void ReadInclude(ScriptInfo si, XmlElement includeElem)
        {
            string SFPath = includeElem.InnerText;

            ScriptFile includeFile = si.SF.Manager.GetSF(SFPath);
            if (includeFile != null)
                si.AddInclude(new IncludeInfo(includeFile));
            else
                si.SF.Manager.Trace.TraceEvent(TraceEventType.Warning, 0, "Could not include file '" + SFPath + "'");
        }

        private static void ReadUsing(ScriptInfo SI, XmlElement usingElem)
        {
            string name = string.Empty;
            string sfPath = string.Empty;
            MemberAccess access = MemberAccess.Public;

            name = usingElem.GetAttribute("name");
            access = (MemberAccess)Enum.Parse(typeof(MemberAccess), usingElem.GetAttribute("access"));
            foreach (XmlNode curNode in usingElem)
            {
                if (!(curNode is XmlElement))
                    continue;

                XmlElement curElem = (XmlElement)curNode;

                if (curElem.Name == "sfPath")
                    sfPath = curElem.InnerText;
                else
                    throw new XmlException("Unknown ScriptInfo using node '" + curElem.Name + "'");
            }

            UsingInfo usingInfo = new UsingInfo(SI.SF, name, sfPath, access, null);
            SI.AddUsing(usingInfo);
        }

        private static void ReadConst(ScriptInfo SI, XmlElement constElem)
        {
            string name = string.Empty;
            string originalValue = string.Empty;
            CODSCRIPT.Content.Expression value = null;
            MemberAccess access = MemberAccess.Public;
            string summary = string.Empty;
            bool isSealed = false;

            name = constElem.GetAttribute("name");
            access = (MemberAccess)Enum.Parse(typeof(MemberAccess), constElem.GetAttribute("access"));
            string sealedStr = constElem.GetAttribute("sealed");
            isSealed = String.IsNullOrEmpty(sealedStr) ? false : Boolean.Parse(sealedStr);

            foreach (XmlElement curElem in constElem.ChildNodes.OfType<XmlElement>())
            {
                if (curElem.Name == "summary")
                    summary = curElem.InnerText;
                else if (curElem.Name == "originalValue")
                    originalValue = curElem.InnerText;
                else if (curElem.Name == "value")
                    value = CODSCRIPT.Content.Expression.FromXML(curElem);
                else
                    throw new XmlException("Unknown ScriptInfo const node '" + curElem.Name + "'");
            }

            ConstInfo constInfo = new ConstInfo(SI.SF, name, originalValue, value, access, summary, isSealed, null);
            SI.AddConstant(constInfo);
        }

        private static void ReadFunc(ScriptInfo SI, XmlElement funcElem)
        {
            string name = string.Empty;
            MemberAccess access = MemberAccess.Public;
            string returns = string.Empty;
            string summary = string.Empty;
            string self = string.Empty;
            string example = string.Empty;

            List<FuncParamInfo> parameters = new List<FuncParamInfo>();

            name = funcElem.GetAttribute("name");
            access = (MemberAccess)Enum.Parse(typeof(MemberAccess), funcElem.GetAttribute("access"));
            foreach (XmlNode curNode in funcElem)
            {
                if (!(curNode is XmlElement))
                    continue;

                XmlElement curElem = (XmlElement)curNode;

                if (curElem.Name == "summary")
                    summary = curElem.InnerText;
                else if (curElem.Name == "returns")
                    returns = curElem.InnerText;
                else if (curElem.Name == "self")
                    self = curElem.InnerText;
                else if (curElem.Name == "example")
                    example = curElem.InnerText;
                else if (curElem.Name == "param")
                {
                    string isOut = curElem.GetAttribute("out");
                    string isOpt = curElem.GetAttribute("opt");
                    bool isOutBool = String.IsNullOrEmpty(isOut) ? false : Boolean.Parse(isOut);
                    bool isOptBool = String.IsNullOrEmpty(isOpt) ? false : Boolean.Parse(isOpt);

                    parameters.Add(new FuncParamInfo(curElem.GetAttribute("name"), curElem.InnerText, isOutBool, isOptBool));
                }
                else
                    throw new XmlException("Unknown ScriptInfo func node '" + curElem.Name + "'");
            }

            FuncInfo funcInfo = new FuncInfo(SI.SF, name, access, summary, returns, self, example, null);
            foreach (FuncParamInfo param in parameters)
                funcInfo.AddParam(param);
            SI.AddFunction(funcInfo);
        }
        #endregion

        #region Write Info to XML
        public void WriteInfoToFile(string fullPath)
        {
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlElement root = doc.CreateElement("scriptinfo");

                XmlElement settings = doc.CreateElement("settings");
                settings.SetAttribute("global", IsGlobal.ToString());
                settings.SetAttribute("successfully", (SF.ReadingState != ReadingState.Error).ToString());
                settings.SetAttribute("compiled", IsCompiled.ToString());

                    XmlElement name = doc.CreateElement("name");
                    name.InnerText = Name;
                    settings.AppendChild(name);

                root.AppendChild(settings);

                XmlElement content = doc.CreateElement("content");

                if (SF.ReadingState != ReadingState.Error)
                {
                    foreach (IncludeInfo inc in Includes)
                    {
                        XmlElement include = doc.CreateElement("include");
                        include.InnerText = inc.SFPath;
                        content.AppendChild(include);
                    }

                    foreach (UsingInfo curUsing in Usings)
                    {
                        XmlElement usingElem = doc.CreateElement("using");
                        usingElem.SetAttribute("name", curUsing.Name);
                        usingElem.SetAttribute("access", curUsing.Access.ToString());

                        XmlElement sfPath = doc.CreateElement("sfPath");
                        sfPath.InnerText = curUsing.SFPath;
                        usingElem.AppendChild(sfPath);

                        content.AppendChild(usingElem);
                    }

                    foreach (ConstInfo constant in Constants)
                    {
                        XmlElement constElem = doc.CreateElement("const");
                        constElem.SetAttribute("name", constant.Name);
                        constElem.SetAttribute("access", constant.Access.ToString());
                        constElem.SetAttribute("sealed", constant.Sealed.ToString());

                        XmlElement summary = doc.CreateElement("summary");
                        summary.InnerText = constant.Summary;
                        constElem.AppendChild(summary);

                        XmlElement value = doc.CreateElement("value");
                        value = constant.Value.ToXML(doc, value, this);
                        constElem.AppendChild(value);

                        content.AppendChild(constElem);
                    }

                    foreach (FuncInfo func in Functions)
                    {
                        XmlElement funcElem = doc.CreateElement("func");
                        funcElem.SetAttribute("name", func.Name);
                        funcElem.SetAttribute("access", func.Access.ToString());

                        XmlElement self = doc.CreateElement("self");
                        self.InnerText = func.Self;
                        funcElem.AppendChild(self);

                        XmlElement summary = doc.CreateElement("summary");
                        summary.InnerText = func.Summary;
                        funcElem.AppendChild(summary);

                        XmlElement returns = doc.CreateElement("returns");
                        returns.InnerText = func.Returns;
                        funcElem.AppendChild(returns);

                        XmlElement example = doc.CreateElement("example");
                        example.InnerText = func.Example;
                        funcElem.AppendChild(example);

                        foreach (FuncParamInfo param in func.Parameters)
                        {
                            XmlElement paramElem = doc.CreateElement("param");
                            paramElem.SetAttribute("name", param.Name);

                            if (param.IsOpt)
                                paramElem.SetAttribute("opt", param.IsOpt.ToString());

                            paramElem.InnerText = param.Summary;
                            funcElem.AppendChild(paramElem);
                        }

                        content.AppendChild(funcElem);
                    }

                    foreach (IReferenceInfo refInfo in References)
                        content.AppendChild(refInfo.ToXML(doc));

                }

                foreach (Error error in SF.Errors)
                    content.AppendChild(error.ToXML(doc));

                root.AppendChild(content);

            doc.AppendChild(root);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            doc.Save(fullPath);
        }
        #endregion

        #region Adding
        public void AddInclude(IncludeInfo include)
        {
            Includes.Add(include);
        }

        public void AddUsing(UsingInfo usingInfo)
        {
            Usings.Add(usingInfo);
            localMembers = null;
        }

        public void AddConstant(ConstInfo constInfo)
        {
            Constants.Add(constInfo);
            localMembers = null;
        }

        public void AddFunction(FuncInfo funcInfo)
        {
            Functions.Add(funcInfo);
            localMembers = null;
        }
        #endregion

        #region Finding Usings
        /// <summary>
        /// Nájde lokálnu konštantu.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public UsingInfo FindLocalUsing(string name)
        {
            return Usings.Find(a => a.Name.EqualCode(name));
        }

        /// <summary>
        /// Nájde funkciu v includovaných súboroch.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public UsingInfo FindIncludesUsing(string name)
        {
            UsingInfo curUsing = null;
            foreach (IncludeInfo includeInfo in this.Includes)
            {
                if (includeInfo.SF.SI == null)
                    continue;

                curUsing = includeInfo.SF.SI.FindLocalUsing(name);
                if (curUsing != null)
                    break;
            }
            return curUsing;
        }

        /// <summary>
        /// Nájde funkciu na všetkých dostupných miestach.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public UsingInfo FindUsing(string name)
        {
            UsingInfo curUsing = null;

            // search in local file...
            curUsing = FindLocalUsing(name);
            if (curUsing != null)
                return curUsing;

            // search in includes...
            curUsing = FindIncludesUsing(name);
            if (curUsing != null)
                return curUsing;

            return curUsing;
        }
        #endregion

        #region Finding Functions
        /// <summary>
        /// Nájde lokálnu funkciu.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FuncInfo FindLocalFunc(string name)
        {
            return Functions.Find(a => a.Name.EqualCode(name));
        }

        /// <summary>
        /// Nájde funkciu v includovaných súboroch.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FuncInfo FindIncludesFunc(string name)
        {
            FuncInfo func = null;
            foreach (IncludeInfo includeInfo in this.Includes)
            {
                if (includeInfo.SF.SI == null)
                    continue;

                func = includeInfo.SF.SI.FindLocalFunc(name);
                if (func != null)
                    break;
            }
            return func;
        }

        /// <summary>
        /// Nájde funkciu v globálnych súboroch.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FuncInfo FindGlobalsFunc(string name)
        {
            FuncInfo func = null;
            foreach (ScriptFile globalFile in this.SF.Manager.GetGlobalSFs())
            {
                func = globalFile.SI.FindLocalFunc(name);
                if (func != null)
                    break;
            }
            return func;
        }

        /// <summary>
        /// Nájde funkciu na všetkých dostupných miestach.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public FuncInfo FindFunc(string name)
        {
            FuncInfo func = null;

            // search in local file...
            func = FindLocalFunc(name);
            if (func != null)
                return func;

            // search in includes...
            func = FindIncludesFunc(name);
            if (func != null)
                return func;

            // search in global files...
            func = FindGlobalsFunc(name);

            return func;
        }

        /// <summary>
        /// Nájde funkciu v externom súbore.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public FuncInfo FindFunc(string name, string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
                return FindFunc(name);

            // search local...
            if (filePath == this.SF.SFPath)
                return FindLocalFunc(name);

            // search in file...
            ScriptFile externFile = this.SF.Manager.GetSF(filePath);
            if (externFile == null)
                return null;

            //externFile.ReadSI();
            return externFile.SI.FindLocalFunc(name);
        }
        #endregion

        #region Finding Constants
        /// <summary>
        /// Nájde lokálnu konštantu.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ConstInfo FindLocalConst(string name)
        {
            return Constants.Find(a => a.Name.EqualCode(name));
        }

        /// <summary>
        /// Nájde konštantu v includovaných súboroch.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ConstInfo FindIncludesConst(string name)
        {
            ConstInfo constant = null;
            foreach (IncludeInfo includeInfo in this.Includes)
            {
                if (includeInfo.SF.SI == null)
                    continue;

                constant = includeInfo.SF.SI.FindLocalConst(name);
                if (constant != null)
                    break;
            }
            return constant;
        }

        /// <summary>
        /// Nájde funkciu v globálnych súboroch.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ConstInfo FindGlobalsConst(string name)
        {
            ConstInfo constant = null;
            foreach (ScriptFile globalFile in this.SF.Manager.GetGlobalSFs())
            {
                constant = globalFile.SI.FindLocalConst(name);
                if (constant != null)
                    break;
            }
            return constant;
        }

        /// <summary>
        /// Nájde konštantu na všetkých dostupných miestach.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ConstInfo FindConst(string name)
        {
            ConstInfo constant = null;

            // search in local file...
            constant = FindLocalConst(name);
            if (constant != null)
                return constant;

            // search in includes...
            constant = FindIncludesConst(name);
            if (constant != null)
                return constant;

            // search in global files...
            constant = FindGlobalsConst(name);

            return constant;
        }

        /// <summary>
        /// Nájde konštantu v externom súbore.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public ConstInfo FindConst(string name, string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
                return FindConst(name);

            // search local...
            if (filePath == this.SF.SFPath)
                return FindLocalConst(name);

            // search in file...
            ScriptFile externFile = this.SF.Manager.GetSF(filePath);
            if (externFile == null)
                return null;

            //externFile.ReadSI();
            return externFile.SI.FindLocalConst(name);
        }
        #endregion

        #region Finding Member
        public IMemberInfo FindLocalMember(string name)
        {
            IMemberInfo member;

            member = FindConst(name);
            if (member != null)
                return member;

            member = FindFunc(name);
            return member;
        }
        #endregion

        private List<IMemberInfo> localMembers;
        public List<IMemberInfo> GetLocalMembers(bool publicOnly)
        {
            if (localMembers == null)
            {
                localMembers = new List<IMemberInfo>();

                foreach (IMemberInfo m in Constants)
                    localMembers.Add(m);

                foreach (IMemberInfo m in Functions)
                    localMembers.Add(m);
            }

            if (publicOnly)
            {
                List<IMemberInfo> publicMembers = new List<IMemberInfo>(localMembers.Count);
                foreach (IMemberInfo m in localMembers)
                    if ((m is FuncInfo && ((FuncInfo)m).Access == MemberAccess.Public)
                        || (m is ConstInfo && ((ConstInfo)m).Access == MemberAccess.Public))
                        publicMembers.Add(m);

                return publicMembers;
            }

            return localMembers;
        }

        public List<IMemberInfo> GetIncludesMembers(bool publicOnly)
        {
            List<IMemberInfo> members = new List<IMemberInfo>();
            foreach (IncludeInfo includeInfo in this.Includes)
            {
                if (includeInfo.SF.SI == null)
                    continue;

                foreach (ConstInfo c in includeInfo.SF.SI.Constants)
                    if (!publicOnly || (publicOnly && c.Access == MemberAccess.Public))
                        members.Add(c);

                foreach (FuncInfo f in includeInfo.SF.SI.Functions)
                    if (!publicOnly || (publicOnly && f.Access == MemberAccess.Public))
                        members.Add(f);
            }
            return members;
        }

        //private List<IMemberInfo> availableMembers;

        /// <summary>
        /// Vráti zoznam dostupných členov v súčasnom súbore(global, includes, local)
        /// </summary>
        /// <returns></returns>
        public List<IMemberInfo> GetAvailableMembers()
        {
                // global
            List<IMemberInfo> availableMembers = sf.Manager.GetGlobalMembers().ToList();

                // includes
                foreach (IncludeInfo includeInf in Includes)
                {
                    if (includeInf.SF.SecondSI == null) // error in file
                        continue;

                    foreach (UsingInfo curUsing in includeInf.SF.SecondSI.Usings)
                        if (curUsing.Access == MemberAccess.Public)
                            availableMembers.Add(curUsing);
                    foreach (ConstInfo curConst in includeInf.SF.SecondSI.Constants)
                        if (curConst.Access == MemberAccess.Public)
                            availableMembers.Add(curConst);
                    foreach (FuncInfo curFunc in includeInf.SF.SecondSI.Functions)
                        if (curFunc.Access == MemberAccess.Public)
                            availableMembers.Add(curFunc);
                }

                // local
                foreach (UsingInfo curUsing in Usings)
                    availableMembers.Add(curUsing);
                foreach (ConstInfo curConst in Constants)
                    availableMembers.Add(curConst);
                foreach (FuncInfo curFunc in Functions)
                    availableMembers.Add(curFunc);

                return availableMembers;
        }

        /// <summary>
        /// Vráti zoznam dostupných členov v súčasnom súbore(global, includes, local)
        /// </summary>
        /// <param name="pos">Pozícia v texte</param>
        /// <returns></returns>
        public List<IMemberInfo> GetAvailableMembers(int pos)
        {
            List<IMemberInfo> members = GetAvailableMembers().ToList();

            List<LocalVarInfo> varList = GetFuncVarsByPos(pos);
            foreach (LocalVarInfo varInfo in varList)
                members.Add(varInfo);

            return members;
        }

        public List<LocalVarInfo> GetFuncVarsByPos(int pos)
        {
            List<LocalVarInfo> vars = new List<LocalVarInfo>();
            foreach (FuncInfo func in Functions)
            {
                if (pos >= func.NppInfo.CharIndex && pos < (func.NppInfo.CharIndex + func.NppInfo.CharLength))
                {
                    foreach (LocalVarInfo varInfo in func.LocalVars)
                        if (varInfo.NppInfo.CharIndex <= pos)
                            vars.Add(varInfo);

                    //StringBuilder sb = new StringBuilder();
                    //foreach (LocalVarInfo varInfo in func.LocalVars)
                    //    sb.Append(varInfo.Name + ", ");

                    //MessageBox.Show("StartI: " + func.CharIndex + "; length: " + func.CharLength);
                    //MessageBox.Show("Pos: " + pos + "\n Func: " + func.FuncInfo.ToString() + "\n {" + sb.ToString() + "}");
                    break;
                }
            }
            return vars;
        }

        public IMemberInfo GetMemberDefinitionAtPos(int pos)
        {
            if (pos < 0)
                return null;

            foreach (IMemberInfo m in Usings)
                if (pos > m.NppInfo.ImportantCharIndex && pos < (m.NppInfo.ImportantCharIndex + m.NppInfo.ImportantCharLength))
                    return m;

            foreach (IMemberInfo m in Constants)
                if (pos > m.NppInfo.ImportantCharIndex && pos < (m.NppInfo.ImportantCharIndex + m.NppInfo.ImportantCharLength))
                    return m;

            foreach (IMemberInfo m in Functions)
                if (pos > m.NppInfo.ImportantCharIndex && pos < (m.NppInfo.ImportantCharIndex + m.NppInfo.ImportantCharLength))
                    return m;

            return null;
        }
    }
}
