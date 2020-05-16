using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    public class OverwriteConstDef : SimpleTree
    {
        private ConstInfo _constInfo;
        public ConstInfo ConstInfo { get { return _constInfo; } }

        private string _path;
        private string _name;

        private Expression _originalContent; // created when it creates copy
        private Expression _compiledContent; // part of OverwriteConstDef

        public Expression OriginalContent { get { return _originalContent; } }
        public Expression CompiledContent { get { return _compiledContent; } }

        private OverwriteConstDef(int charIndex, int charLength, int lineIndex)
            : base(null, true, charIndex, charLength, lineIndex)
        {
        }

        public override IElement CreateCopy()
        {
            OverwriteConstDef e = new OverwriteConstDef((int)ImportantCharIndex, (int)ImportantCharLength, (int)ImportantLineIndex);
            e.AddChildren(this.CopyChildren());

            e._constInfo = _constInfo;

            e._path = _path;
            e._name = _name;

            e._compiledContent = (Expression)e.children[this.children.IndexOf(_compiledContent)];
            e._originalContent = (Expression)e._compiledContent.CreateCopy();
            
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("overwrite"))
            {
                /*MoveInfo moveInfo = new MoveInfo(parentInfo);
                IElement assignTry = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
                if (assignTry != null && assignTry.IsTT(TokenType.Assign))
                {*/
                    Parse(parentInfo, parsingInfo, scriptInfo);
                    return true;
                //}
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            OverwriteConstDef constDef = new OverwriteConstDef(parentInfo.Current.CharIndex, parentInfo.Current.CharLength, parentInfo.Current.LineIndex);

            int startIndex = parentInfo.CurrentIndex;
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            // overwrite [(name)(path::name)] = value;

            // overwrite name = value;
            IElement tryName = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            IElement tryAssign = moveInfo.FindNextBlack(SearchDirection.LeftToRight);

            if (tryName == null || !tryName.IsTT(TokenType.Word) || tryAssign == null)
                throw new SyntaxException("Could not parse overwrite constDef name", parentInfo.GetErrorInfo());

            if (tryAssign.IsTT(TokenType.Assign))
            {
                moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            }
            else // overwrite path::name = value;
            {
                moveInfo = new MoveInfo(parentInfo);
                moveInfo.FindNextBlack(SearchDirection.LeftToRight);
                Path tryPath = Path.Parse(moveInfo, parsingInfo, scriptInfo);

                IElement tryNamespace = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
                tryName = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
                tryAssign = moveInfo.FindNextBlack(SearchDirection.LeftToRight);

                moveInfo.FindNextBlack(SearchDirection.LeftToRight);

                if (tryPath == null 
                    || tryNamespace == null || !tryNamespace.IsTT(TokenType.Namespace)
                    || tryName == null || !tryName.IsTT(TokenType.Word) 
                    || tryAssign == null || !tryAssign.IsTT(TokenType.Assign))
                    throw new SyntaxException("Could not parse overwrite constDef name", parentInfo.GetErrorInfo());

                constDef._path = tryPath.ToString();
            }
            constDef._name = tryName.ToString();

            // expression
            Expression exp = Expression.Parse(moveInfo, parsingInfo, scriptInfo);
            if (exp == null)
                throw new SyntaxException("Could not parse overwrite constDef expression", parentInfo.GetErrorInfo());

            constDef._compiledContent = exp;

            // terminal
            IElement terminalTry = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (terminalTry == null || !(terminalTry.IsTT(TokenType.SemiColon)))
                throw new SyntaxException("Missing directive ';'?", parentInfo.GetErrorInfo());

            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            constDef.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.MoveToIndex(startIndex);
            parentInfo.Replace(length, constDef);

            // add const def to list
            parsingInfo.OverwriteConstDefList.Add(constDef);

            /*if (scriptInfo.Constants.FindIndex(a => a.Name == name) != -1)
                ErrorManager.Semantic("Constant '" + name + "' already defined", new ErrorInfo(parentInfo.ErrorInfo));
            else
            {*/
            /*ConstInfo constInfo = GetInfo(name, access, exp.ToString(), isSealed, constDef.xmlBlock, parsingInfo);
            scriptInfo.AddConstant(constInfo);
            constDef.constInfo = constInfo;*/
            //}
        }

        public override void CheckSemantic(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        {
            if (this._path != null)
            {
                ScriptFile sf = scriptInfo.SF.Manager.GetSF(this._path);
                if (sf == null)
                    scriptInfo.SF.Errors.Add(new SemanticError("Could not find file '" + this._path + "'",
                        treeInfo.GetErrorInfo(treeInfo.Current)));

                ConstInfo constInfo = sf.SI.FindLocalConst(this._name);
                if (constInfo == null)
                    scriptInfo.SF.Errors.Add(new SemanticError("Could not find constant '" + this._name + "'",
                        treeInfo.GetErrorInfo(treeInfo.Current)));
                else if (constInfo.Access == MemberAccess.Private)
                    scriptInfo.SF.Errors.Add(new SemanticError("Cannot access constant '" + constInfo.ToString() + "'",
                        treeInfo.GetErrorInfo(treeInfo.Current)));
                else if (constInfo.Sealed)
                    scriptInfo.SF.Errors.Add(new SemanticError("Cannot overwrite sealed constant '" + constInfo.ToString() + "'",
                        treeInfo.GetErrorInfo(treeInfo.Current)));
                else
                    this._constInfo = constInfo;
            }
            else
            {
                string upperName = this._name.ToUpperInvariant();
                List<IMemberInfo> members = scriptInfo.SF.SI.GetAvailableMembers();
                foreach (IMemberInfo m in members)
                {
                    if (m.Name.ToUpperInvariant() == upperName)
                    {
                        if (!(m is ConstInfo))
                        {
                            scriptInfo.SF.Errors.Add(new SemanticError("Cannot overwrite member '" + m.ToString() + "'",
                                treeInfo.GetErrorInfo(treeInfo.Current)));
                            return;
                        }

                        this._constInfo = (ConstInfo)m;
                    }
                }

                if (this._constInfo == null)
                    scriptInfo.SF.Errors.Add(new SemanticError("Could not find constant '" + this._name + "'",
                        treeInfo.GetErrorInfo(treeInfo.Current)));
                else if (this._constInfo.Sealed)
                {
                    scriptInfo.SF.Errors.Add(new SemanticError("Cannot overwrite sealed constant '" + this._constInfo.ToString() + "'",
                        treeInfo.GetErrorInfo(treeInfo.Current)));

                    this._constInfo = null;
                }
            }
        }

        public void ReplaceMembersInContent(BaseTree baseTree)
        {
            Expression newContent = baseTree.GetCompiledConstValue(_originalContent);
            _compiledContent.ClearChildren();
            _compiledContent.AddChildren(newContent.GetChildren());
            // do not update SI, because ConstInfo is in another file!!!
        }
    }
}
