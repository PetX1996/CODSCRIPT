using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    /// <summary>
    /// Reprezentuje získanie adresy funkcie alebo adresu konštanty(rozdelené pri sémantike)
    /// </summary>
    class DelegateDef : ExpressionOperand
    {
        private FuncInfo _funcInfo;

        string _name;
        string _pathOrUsing;
        string _path;
        Path _pathElem;
        Token _nameSpaceElem;

        private DelegateDef()
            : base()
        {
        }

        public override IElement CreateCopy()
        {
            DelegateDef e = new DelegateDef();
            e.AddChildren(this.CopyChildren());

            e._funcInfo = _funcInfo;

            e._name = _name;
            e._pathOrUsing = _pathOrUsing;
            e._path = _path;

            //e._pathElem = (Path)e.children[this.children.IndexOf(_pathElem)];
            e._nameSpaceElem = (Token)e.children[this.children.IndexOf(_nameSpaceElem)];

            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            // local
            if (parentInfo.Current.IsTT(TokenType.Namespace))
            {
                MoveInfo localInfo = new MoveInfo(parentInfo);
                IElement next = localInfo.FindNextBlack(SearchDirection.LeftToRight);
                if (next.IsTT(TokenType.Word))
                {
                    ParseLocal(parentInfo, parsingInfo, scriptInfo);
                    return true;
                }
            }

            // extern
            if (parentInfo.Current.IsTT(TokenType.Word))
            {
                MoveInfo externInfo = new MoveInfo(parentInfo);
                Path.MoveToEnd(externInfo, SearchDirection.LeftToRight);
                IElement next = externInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
                if (next.IsTT(TokenType.Namespace))
                {
                    next = externInfo.FindNextBlack(SearchDirection.LeftToRight);
                    if (next.IsTT(TokenType.Word))
                    {
                        ParseExtern(parentInfo, parsingInfo, scriptInfo);
                        return true;
                    }
                }
            }

            return false;
        }

        private static void ParseLocal(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            DelegateDef delegDef = new DelegateDef();

            MoveInfo moveInfo = new MoveInfo(parentInfo);
            delegDef._nameSpaceElem = (Token)moveInfo.Current;

            moveInfo.FindNextBlack(SearchDirection.LeftToRight); // jump behind ::
            delegDef._name = moveInfo.Current.ToString();

            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            delegDef.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, delegDef);
        }

        private static void ParseExtern(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            DelegateDef delegDef = new DelegateDef();

            MoveInfo moveInfo = new MoveInfo(parentInfo);
            Path path = Path.Parse(moveInfo, parsingInfo, scriptInfo);
            if (path == null)
                throw new SyntaxException("Bad path", parentInfo.GetErrorInfo());

            delegDef._pathOrUsing = path.ToString();
            delegDef._pathElem = path;

            // ::
            moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            delegDef._nameSpaceElem = (Token)moveInfo.Current;

            // name
            moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            delegDef._name = moveInfo.Current.ToString();

            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            delegDef.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, delegDef);
        }

        public override void CheckSemantic(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        {
            if (!String.IsNullOrEmpty(_pathOrUsing)) // extern func or extern const
                CheckSemanticExtern(treeInfo, scriptInfo, checkingInfo);
            else // local or includes func
                CheckSemanticLocal(treeInfo, scriptInfo, checkingInfo);
        }

        private void CheckSemanticExtern(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        {
            _path = _pathOrUsing;

            #region Finding using
            UsingInfo usingInfoTry = scriptInfo.FindUsing(_path);
            if (usingInfoTry != null)
            {
                if (usingInfoTry.SF != scriptInfo.SF && usingInfoTry.Access != MemberAccess.Public)
                {
                    scriptInfo.SF.Errors.Add(new SemanticError("Could not access using '" + usingInfoTry.SF.SFPath + "::" + usingInfoTry.Name + "'",
                        treeInfo.GetErrorInfo(this._pathElem)));
                    return;
                }

                _path = usingInfoTry.SFPath;
                UsingName usingName = UsingName.ConvertToMe(this, this._pathElem, usingInfoTry);

                scriptInfo.References.Add(new UsingRefInfo(scriptInfo.SF, usingInfoTry,
                    usingName.CharIndex, usingName.CharLength, checkingInfo.SC.SourceCode.Substring(usingName.CharIndex, usingName.CharLength)));
            }
            #endregion

            ScriptFile sf = scriptInfo.SF.Manager.GetSF(_path);
            if (sf == null || sf.SI == null)
            {
                scriptInfo.SF.Errors.Add(
                    new SemanticError("Could not find file '" + _path + "'",
                        treeInfo.GetErrorInfo(treeInfo.Current)));
                return;
            }
            else if (sf.SI == null)
            {
                scriptInfo.SF.Errors.Add(
                    new WarningError("Could not read file '" + _path + "'",
                        treeInfo.GetErrorInfo(treeInfo.Current)));
                return;
            }

            IMemberInfo member = sf.SI.FindLocalMember(_name);
            if (member == null)
            {
                scriptInfo.SF.Errors.Add(
                    new SemanticError("Unknown member '" + _path + "::" + _name + "'",
                        treeInfo.GetErrorInfo(treeInfo.Current)));
                return;
            }

            // member is private
            if (sf != scriptInfo.SF &&
                ((member is FuncInfo && ((FuncInfo)member).Access != MemberAccess.Public)
                || (member is ConstInfo && ((ConstInfo)member).Access != MemberAccess.Public)))
            {
                scriptInfo.SF.Errors.Add(
                    new SemanticError("Cannot access member '" + _path + "::" + _name + "'",
                        treeInfo.GetErrorInfo(treeInfo.Current)));
            }

            // create constant
            if (member is ConstInfo)
            {
                ToConstant(treeInfo, (ConstInfo)member);
                scriptInfo.References.Add(new ConstRefInfo(scriptInfo.SF, (ConstInfo)member,
                    this.CharIndex, this.CharLength, checkingInfo.SC.SourceCode.Substring(this.CharIndex, this.CharLength)));
                return;
            }
            else if (member is FuncInfo)
            {
                _funcInfo = (FuncInfo)member;
                scriptInfo.References.Add(new FuncRefInfo(scriptInfo.SF, (FuncInfo)member, this.CharIndex, this.CharLength,
                    checkingInfo.SC.SourceCode.Substring(this.CharIndex, this.CharLength), false));
            }
            else
                scriptInfo.SF.Errors.Add(
                    new SemanticError("Unknown member type '" + _path + "::" + _name + "'",
                        treeInfo.GetErrorInfo(treeInfo.Current)));
        }

        private void CheckSemanticLocal(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        {
            _funcInfo = scriptInfo.FindLocalFunc(_name);

            if (_funcInfo == null) // find in includes
            {
                _funcInfo = scriptInfo.FindIncludesFunc(_name);

                if (_funcInfo == null)
                {
                    scriptInfo.SF.Errors.Add(
                        new SemanticError("Unknown function '" + _name + "'",
                            treeInfo.GetErrorInfo(treeInfo.Current)));
                    return;
                }

                if (_funcInfo.Access == MemberAccess.Private)
                {
                    scriptInfo.SF.Errors.Add(
                        new SemanticError("Cannot access member '" + _funcInfo.ToString() + "'",
                            treeInfo.GetErrorInfo(treeInfo.Current)));
                }
            }

            scriptInfo.References.Add(new FuncRefInfo(scriptInfo.SF, _funcInfo, this.CharIndex, this.CharLength, 
                checkingInfo.SC.SourceCode.Substring(this.CharIndex, this.CharLength), false));
        }

        private void ToConstant(MoveInfo treeInfo, ConstInfo constant)
        {
            ConstName constName = new ConstName(this.GetChildren(), constant);
            treeInfo.Replace(1, constName);
        }

        public void Compile_SpecifyPath(BaseTree baseTree)
        {
            if (String.IsNullOrEmpty(_path) && !_funcInfo.SF.SI.IsGlobal) // path is not specify
            {
                _path = baseTree.GetActualSFPath(_funcInfo.SF.SFPath);
                IElement pathE = new Path(_funcInfo.SF.SFPath, _path);
                //IElement namespaceE = new Token(TokenType.Namespace);

                int nameI = this.children.IndexOf(_nameSpaceElem);
                //this.children.Insert(nameI, namespaceE);
                this.children.Insert(nameI, pathE);
            }
        }
    }
}
