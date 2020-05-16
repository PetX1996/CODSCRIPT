using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace CODSCRIPT.Content
{
    public class ConstDef : SimpleTree
    {
        private ConstInfo _constInfo;
        public ConstInfo ConstInfo { get { return _constInfo; } }

        private Expression _originalContent; // created when it creates copy
        private Expression _compiledContent; // part of ConstDef

        public Expression OriginalContent { get { return _originalContent; } set { _originalContent = value; } }

        private XMLBlock xmlBlock;
        public XMLBlock XMLBlock { get { return xmlBlock; } }

        private ConstDef(int charIndex, int charLength, int lineIndex)
            : base(null, true, charIndex, charLength, lineIndex)
        { 
        }

        public override IElement CreateCopy()
        {
            ConstDef e = new ConstDef((int)ImportantCharIndex, (int)ImportantCharLength, (int)ImportantLineIndex);
            e.AddChildren(this.CopyChildren());

            e._constInfo = _constInfo;
            //e.xmlBlock = xmlBlock;

            e._compiledContent = (Expression)e.children[this.children.IndexOf(_compiledContent)];
            e._originalContent = (Expression)e._compiledContent.CreateCopy();

            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word))
            {
                MoveInfo moveInfo = new MoveInfo(parentInfo);
                IElement assignTry = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
                if (assignTry != null && assignTry.IsTT(TokenType.Assign))
                {
                    Parse(parentInfo, parsingInfo, scriptInfo);
                    return true;
                }
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            ConstDef constDef = new ConstDef(parentInfo.Current.CharIndex, parentInfo.Current.CharLength, parentInfo.Current.LineIndex);
            MemberAccess access = MemberAccess.Public;
            bool isSealed = false;

            int startIndex = parentInfo.CurrentIndex;
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            // sealed modifier
            SealedModifier trySealed = SealedModifier.GetModifier(moveInfo);
            if (trySealed != null)
            {
                startIndex = moveInfo.CurrentIndex;
                isSealed = true;
            }
            else
                moveInfo = new MoveInfo(parentInfo);

            // visibility modifier
            AccessModifier tryAccess = AccessModifier.GetModifier(moveInfo, out access);
            if (tryAccess != null)
                startIndex = moveInfo.CurrentIndex;
            else
                moveInfo = new MoveInfo(parentInfo);

            // xml
            XMLBlock xml = XMLBlock.GetXMLSummary(moveInfo);
            constDef.xmlBlock = xml;
            if (xml != null)
                startIndex = moveInfo.CurrentIndex;

            // assign
            moveInfo = new MoveInfo(parentInfo);
            IElement nameElem = moveInfo.Current;

            moveInfo.FindNextBlack(SearchDirection.LeftToRight); // =
            moveInfo.FindNextBlack(SearchDirection.LeftToRight); // behind =

            // expression
            Expression exp = Expression.Parse(moveInfo, parsingInfo, scriptInfo);
            if (exp == null)
                throw new SyntaxException("Could not parse constDef expression", parentInfo.GetErrorInfo());

            constDef._compiledContent = exp;

            // terminal
            IElement terminalTry = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (terminalTry == null || !(terminalTry.IsTT(TokenType.SemiColon)))
                throw new SyntaxException("Missing directive ';'?", parentInfo.GetErrorInfo());

            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            constDef.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.MoveToIndex(startIndex);
            parentInfo.Replace(length, constDef);

            // info
            string name = nameElem.ToString();

            // add const def to list
            parsingInfo.ConstDefList.Add(constDef);

            /*if (scriptInfo.Constants.FindIndex(a => a.Name == name) != -1)
                ErrorManager.Semantic("Constant '" + name + "' already defined", new ErrorInfo(parentInfo.ErrorInfo));
            else
            {*/
                ConstInfo constInfo = GetInfo(name, access, exp, isSealed, constDef.xmlBlock, parsingInfo, constDef);
                scriptInfo.AddConstant(constInfo);
                constDef._constInfo = constInfo;
            //}
        }

        private static ConstInfo GetInfo(string name, MemberAccess access, Expression value, bool isSealed, XMLBlock xml, ParsingInfo parsingInfo, ConstDef constDef)
        {
            string summary = string.Empty;
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
                            if (n.Name == "summary")
                                summary = n.InnerText;
                        }
                    }
                    catch
                    {
                        summary = "Could not parse XML.";
                    }
                }
            }
            return new ConstInfo(parsingInfo.SF, name, value.ToString().Replace("\r", "").Replace("\n", ""), 
                value, access, summary, isSealed, constDef);
        }

        public override void CheckSemantic(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        {
            string upperName = this._constInfo.Name.ToUpperInvariant();
            List<IMemberInfo> members = scriptInfo.SF.SI.GetAvailableMembers();
            foreach (IMemberInfo m in members)
            {
                if (m.Name.ToUpperInvariant() == upperName)
                {
                    if ((m as ConstInfo) != this._constInfo)
                        scriptInfo.SF.Errors.Add(
                            new SemanticError("Member '" + this._constInfo.Name + "' already defined(" + m.SF.SFPath + "::" + m.Name + ")",
                                treeInfo.GetErrorInfo(treeInfo.Current)));
                }
            }
        }

        public void ReplaceMembersInContent(BaseTree baseTree)
        {
            Expression newContent = baseTree.GetCompiledConstValue(_originalContent);
            _compiledContent.ClearChildren();
            _compiledContent.AddChildren(newContent.GetChildren());

            //this._constInfo.Value = _content; ConstInfo is for original SF...if this is overwrite, we need to find copied ConstInfo
            ConstInfo rightConst = baseTree.SF.SI.FindLocalConst(this._constInfo.Name);
            rightConst.Value = _compiledContent;
        }
    }
}
