using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    public class UsingDef : SimpleTree
    {
        private UsingInfo _usingInfo;
        public UsingInfo UsingInfo { get { return _usingInfo; } }

        private UsingDef(int charIndex, int charLength, int lineIndex)
            : base(null, true, charIndex, charLength, lineIndex)
        {
        }

        public override IElement CreateCopy()
        {
            UsingDef e = new UsingDef((int)ImportantCharIndex, (int)ImportantCharLength, (int)ImportantLineIndex);
            e._usingInfo = _usingInfo;
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("using"))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            int startIndex = parentInfo.CurrentIndex;
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            // modifier
            MemberAccess access;
            AccessModifier modifier = AccessModifier.GetModifier(moveInfo, out access);
            if (modifier != null)
                startIndex = moveInfo.CurrentIndex;
            
            // name
            moveInfo = new MoveInfo(parentInfo);
            IElement tryName = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (tryName == null || !tryName.IsTT(TokenType.Word))
                throw new SyntaxException("Could not find using name", parentInfo.GetErrorInfo());

            string name = tryName.ToString();
            UsingDef usingDef = new UsingDef(moveInfo.Current.CharIndex, moveInfo.Current.CharLength, moveInfo.Current.LineIndex);

            // assign
            moveInfo.FindNextBlack(SearchDirection.LeftToRight); // =
            moveInfo.FindNextBlack(SearchDirection.LeftToRight); // behind =

            // expression
            Path pathTry = Path.Parse(moveInfo, parsingInfo, scriptInfo);
            if (pathTry == null)
                throw new SyntaxException("Could not find using path", parentInfo.GetErrorInfo());

            // terminal
            IElement terminalTry = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (terminalTry == null || !(terminalTry.IsTT(TokenType.SemiColon)))
                throw new SyntaxException("Missing directive ';'?", parentInfo.GetErrorInfo());

            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            usingDef.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.MoveToIndex(startIndex);
            parentInfo.Replace(length, usingDef);

            // info

            // add const def to list
            parsingInfo.UsingDefList.Add(usingDef);

            /*if (scriptInfo.Constants.FindIndex(a => a.Name == name) != -1)
                ErrorManager.Semantic("Constant '" + name + "' already defined", new ErrorInfo(parentInfo.ErrorInfo));
            else
            {*/
            UsingInfo usingInfo = new UsingInfo(scriptInfo.SF, name, pathTry.ToString(), access, usingDef);
            scriptInfo.AddUsing(usingInfo);
            usingDef._usingInfo = usingInfo;
            //}
        }

        public override void CheckSemantic(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        {
            ScriptFile sf = scriptInfo.SF.Manager.GetSF(_usingInfo.SFPath);
            if (sf == null || sf.SI == null)
                scriptInfo.SF.Errors.Add(new SemanticError("Could not find file '" + _usingInfo.SFPath + "'", 
                    treeInfo.GetErrorInfo(treeInfo.Current)));

            string upperName = this._usingInfo.Name.ToUpperInvariant();
            List<IMemberInfo> members = scriptInfo.SF.SI.GetAvailableMembers();
            foreach (IMemberInfo m in members)
            {
                if (m is UsingInfo && m.Name.ToUpperInvariant() == upperName)
                {
                    if ((m as UsingInfo) != this._usingInfo)
                        scriptInfo.SF.Errors.Add(
                            new SemanticError("Using '" + this._usingInfo.Name + "' already defined(" + m.SF.SFPath + "::" + m.Name + ")",
                                treeInfo.GetErrorInfo(treeInfo.Current)));
                }
            }
        }
    }
}
