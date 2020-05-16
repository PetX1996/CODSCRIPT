using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace CODSCRIPT.Content
{
    public class FuncDef : SimpleTree
    {
        private FuncInfo funcInfo;
        public FuncInfo FuncInfo { get { return funcInfo; } }

        private XMLBlock xmlBlock;
        public XMLBlock XMLBlock { get { return xmlBlock; } }

        public List<LocalVarInfo> LocalVars { get; private set; }

        private ScriptFile _sf;

        private FuncDef(int charIndex, int charLength, int lineIndex, ScriptFile sf)
            : base(null, true, charIndex, charLength, lineIndex)
        {
            LocalVars = new List<LocalVarInfo>() { new LocalVarInfo(sf, "self", charIndex, charLength, null, null) };
            _sf = sf;
        }

        public override IElement CreateCopy()
        {
            FuncDef e = new FuncDef((int)ImportantCharIndex, (int)ImportantCharLength, (int)ImportantLineIndex, _sf);
            e.funcInfo = funcInfo;
            e.xmlBlock = xmlBlock;
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word))
            {
                MoveInfo m = new MoveInfo(parentInfo);
                IElement next = m.FindNextBlack(SearchDirection.LeftToRight);
                if (next != null && next is ParenthesesGroup)
                {
                    Parse(parentInfo, parsingInfo, scriptInfo);
                    return true;
                }
            }
            return false;
        }

        public static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            FuncDef function = new FuncDef(parentInfo.Current.CharIndex, parentInfo.Current.CharLength, parentInfo.Current.LineIndex, parsingInfo.SF);

            // začiatok názvu
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            int startIndex = parentInfo.CurrentIndex;

            // modifier
            MemberAccess access;
            AccessModifier modifier = AccessModifier.GetModifier(moveInfo, out access);
            if (modifier != null)
                startIndex = moveInfo.CurrentIndex;
            else
                moveInfo = new MoveInfo(parentInfo);

            // xml
            XMLBlock xml = XMLBlock.GetXMLSummary(moveInfo);
            function.xmlBlock = xml;
            if (xml != null)
                startIndex = moveInfo.CurrentIndex;

            // začiatok názvu
            moveInfo = new MoveInfo(parentInfo);
            IElement nameElem = moveInfo.Current;
            moveInfo.Move(SearchDirection.LeftToRight);

            // parametry
            IElement paramsTry = moveInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            if (!(paramsTry is ParenthesesGroup))
                throw new SyntaxException("Could not find FuncDef parameters", parentInfo.GetErrorInfo());

            ParenthesesGroup paramsGroup = (ParenthesesGroup)paramsTry;
            // získaj zoznam parametrov
            MoveInfo paramsMoveInfo = new MoveInfo(paramsGroup, SearchTree.ContentBlock, 0, moveInfo);
            List<FuncDefParam> defParams = GetParameterList(paramsMoveInfo, parsingInfo, scriptInfo);

            // pridaj zoznam parametrov do stromu a posuň sa zaň
            moveInfo.Move(SearchDirection.LeftToRight);
            
            // body
            IElement bodyTry = moveInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            if (!(bodyTry is ScopeGroup))
                throw new SyntaxException("Could not find FuncDef body", parentInfo.GetErrorInfo());

            ScopeGroup bodyGroup = (ScopeGroup)bodyTry;
            List<IElement> bodyGroupChildren = bodyGroup.GetChildren();

            moveInfo.Move(SearchDirection.LeftToRight); // skoč za telo

            // add func to tree
            int totalLength = moveInfo.CurrentIndex - startIndex;
            List<IElement> children = parentInfo.CurrentElements.GetRange(startIndex, totalLength);
            function.AddChildren(children);


            foreach (FuncDefParam p in defParams)
                function.LocalVars.Add(new LocalVarInfo(parsingInfo.SF, p.Name, (int)function.ImportantCharIndex, (int)function.ImportantCharLength, null, p.VarName)); // it must be after function.AddChildren() !!


            parentInfo.MoveToIndex(startIndex);
            parentInfo.Replace(totalLength, function);

            // set current function
            parsingInfo.CurrentFunc = function;
            parsingInfo.FuncDefList.Add(function);

            // go inside body
            MoveInfo bodyInfo = new MoveInfo(bodyGroup, SearchTree.ContentBlock, 0, parentInfo);
            Statement.ParseStatementList(bodyInfo, parsingInfo, scriptInfo);

            // info
            string name = nameElem.ToString();
            /*if (scriptInfo.)
            if (scriptInfo.Functions.FindIndex(a => a.Name.EqualCode(name)) != -1)
            {
                ErrorManager.Semantic("Function '" + name + "' already defined", new ErrorInfo(parentInfo.ErrorInfo));
                return;
            }*/

            FuncInfo funcInfo = GetInfo(name, access, defParams, xml, parsingInfo, function);
            scriptInfo.AddFunction(funcInfo);

            function.funcInfo = funcInfo;
        }

        private static FuncInfo GetInfo(string name, MemberAccess access, List<FuncDefParam> defParams, XMLBlock xml, ParsingInfo parsingInfo, FuncDef funcDef)
        {
            string summary = string.Empty;
            string returns = string.Empty;
            string self = string.Empty;
            string example = string.Empty;
            List<FuncParamInfo> xmlParams = new List<FuncParamInfo>();

            if (xml != null)
            {
                string str = xml.GetStringContent();
                if (str.Length > 0 && str[0] != '<')
                {
                    summary = str;
                }
                else
                {
                    str = "<root>" + str + "</root>";
                    XmlDocument doc = new XmlDocument();
                    try
                    {
                        doc.LoadXml(str);

                        foreach (XmlNode n in doc.ChildNodes[0].ChildNodes)
                        {
                            XmlElement e  = (XmlElement)n;
                            if (e.Name == "summary")
                                summary = e.InnerText.Trim();
                            else if (e.Name == "returns")
                                returns = e.InnerText.Trim();
                            else if (e.Name == "self")
                                self = e.InnerText.Trim();
                            else if (e.Name == "example")
                                example = e.InnerText.Trim();
                            else if (e.Name == "param")
                            {
                                string paramName = e.GetAttribute("name");

                                if (!String.IsNullOrEmpty(paramName))
                                {
                                    FuncDefParam defParam = defParams.Find(a => a.Name.EqualCode(paramName));
                                    xmlParams.Add(new FuncParamInfo(paramName, e.InnerText.Trim(), false, defParam.Optional));
                                }
                            }
                        }
                    }
                    catch
                    {
                        summary = "Could not parse XML.";
                    }
                }
            }

            FuncInfo funcInfo = new FuncInfo(parsingInfo.SF, name, access, summary, returns, self, example, funcDef);

            bool anyOptional = false;
            foreach (FuncDefParam param in defParams)
            {
                if (param.Optional)
                    anyOptional = true;

                FuncParamInfo paramInfo = xmlParams.Find(a => a.Name.EqualCode(param.Name));
                if (paramInfo == null)
                    paramInfo = new FuncParamInfo(param.Name, "", false, anyOptional);
                else
                    paramInfo.IsOpt = anyOptional; // update in FuncParamInfos from XML

                funcInfo.AddParam(paramInfo);
            }
            return funcInfo;
        }

        private static List<FuncDefParam> GetParameterList(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            MoveInfo moveInfo = new MoveInfo(parentInfo);
            string error = "Could not parse function parameters";
            List<FuncDefParam> defParams = new List<FuncDefParam>();

            bool? isSeparator = null;
            IElement curElem = moveInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            while(curElem != null)
            {
                if (isSeparator == false || isSeparator == null)
                {
                    FuncDefParam param = FuncDefParam.Parse(moveInfo, parsingInfo, scriptInfo);
                    defParams.Add(param);
                    isSeparator = true;
                }
                else
                {
                    if (!curElem.IsTT(TokenType.Comma))
                        throw new SyntaxException(error, parentInfo.GetErrorInfo());

                    isSeparator = false;
                }
                curElem = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            }

            if (isSeparator == false)
                throw new SyntaxException(error, parentInfo.GetErrorInfo());

            return defParams;
        }

        public override void CheckSemantic(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        {
            string upperName = this.funcInfo.Name.ToUpperInvariant();
            List<IMemberInfo> members = scriptInfo.SF.SI.GetAvailableMembers();
            foreach (IMemberInfo m in members)
            {
                if (m.Name.ToUpperInvariant() == upperName)
                {
                    if ((m as FuncInfo) != this.funcInfo)
                        scriptInfo.SF.Errors.Add(
                            new WarningError("Member '" + this.funcInfo.Name + "' already defined(" + m.SF.SFPath + "::" + m.Name + ")",
                                treeInfo.GetErrorInfo(treeInfo.Current)));
                }
            }

            checkingInfo.CurrentFunc = this;
        }

        public void CheckSemantic_OnLeaveFuncDef(ScriptInfo si)
        {
            foreach (LocalVarInfo v in LocalVars)
            {
                if (v.RefCount == 0
                    && v.VarNameDef != null) // ignore self
                {
                    MoveInfo moveInfo = new MoveInfo(v.VarNameDef, SearchTree.ChildrenBlock, 0, si.SF);
                    si.SF.Errors.Add(
                        new WarningError("Variable '" + v.VarNameDef.ToString() + "' is defined, but its value is never used.", 
                            moveInfo.GetErrorInfo(v.VarNameDef)));
                }
            }
        }
    }
}
