using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    public class FuncCall : ExpressionOperand
    {
        private FuncInfo _funcInfo;

        private string _name;
        private string _pathOrUsing;
        private string _path;

        private Path _pathElem;
        private Token _nameElem;

        /// <summary>
        /// shallow copy!
        /// </summary>
        public List<Expression> Arguments { get; private set; }

        private FuncCall()
            : base()
        {
            Arguments = new List<Expression>();
        }

        public override IElement CreateCopy()
        {
            FuncCall e = new FuncCall();
            e.AddChildren(this.CopyChildren());

            e._funcInfo = _funcInfo;
            e._name = _name;
            e._pathOrUsing = _pathOrUsing;
            e._path = _path;
            e.Arguments = Arguments;

            //e._pathElem = (Path)e.children[this.children.IndexOf(_pathElem)];
            e._nameElem = (Token)e.children[this.children.IndexOf(_nameElem)];
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word))
            {
                // local function
                MoveInfo localInfo = new MoveInfo(parentInfo);
                IElement behindName = localInfo.FindNextBlack(SearchDirection.LeftToRight);
                if (behindName is ParenthesesGroup)
                {
                    ParseLocal(parentInfo, parsingInfo, scriptInfo);
                    return true;
                }

                // extern function
                MoveInfo externInfo = new MoveInfo(parentInfo);
                Path.MoveToEnd(externInfo, SearchDirection.LeftToRight);
                IElement behindPath = externInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
                if (behindPath.IsTT(TokenType.Namespace))
                {
                    IElement name = externInfo.FindNextBlack(SearchDirection.LeftToRight);
                    if (name.IsTT(TokenType.Word))
                    {
                        IElement args = externInfo.FindNextBlack(SearchDirection.LeftToRight);
                        if (args is ParenthesesGroup)
                        {
                            ParseExtern(parentInfo, parsingInfo, scriptInfo);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static void ParseLocal(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            FuncCall funcCall = new FuncCall();

            MoveInfo moveInfo = new MoveInfo(parentInfo);
            funcCall._nameElem = (Token)moveInfo.Current;
            funcCall._name = moveInfo.Current.ToString();

            #region ParsingInfo Args
            FuncInfo tryOutFuncInfo = scriptInfo.FindGlobalsFunc(funcCall._name);
            if (tryOutFuncInfo != null && tryOutFuncInfo.HasOutParams)
            {
                parsingInfo.OutParamFuncCall = funcCall;
                parsingInfo.OutParamFuncInfo = tryOutFuncInfo;
                parsingInfo.OutParamFuncArgIndex = 0;
            }

            object lastCall = parsingInfo.CurrentCall;
            int? lastCallArgIndex = parsingInfo.CurrentCallArgIndex;
            parsingInfo.CurrentCall = funcCall;
            parsingInfo.CurrentCallArgIndex = 0;
            #endregion

            // args
            moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (!FuncArgs.Check(moveInfo, parsingInfo, scriptInfo))
                throw new SyntaxException("Could not parse funcArgs", parentInfo.GetErrorInfo());

            #region ParsingInfo Args
            if (parsingInfo.OutParamFuncCall != null)
                parsingInfo.OutParamFuncCall = null;

            parsingInfo.CurrentCall = lastCall;
            parsingInfo.CurrentCallArgIndex = lastCallArgIndex;
            #endregion

            int startIndex = parentInfo.CurrentIndex;
            int length;

            // find self and modifiers
            MoveInfo selfInfo = new MoveInfo(parentInfo);
            IElement self = selfInfo.FindNextBlack(SearchDirection.RightToLeft);
            if (self != null && self is ExpressionOperand) // self is self or FuncCallModifier
            {
                startIndex = selfInfo.CurrentIndex;
                if (self is FuncCallModifier) // self is FuncCallModifier -> find self
                {
                    self = selfInfo.FindNextBlack(SearchDirection.RightToLeft);
                    if (self != null && self is ExpressionOperand)
                        startIndex = selfInfo.CurrentIndex;
                }
            }

            // find members and arrayIndexers
            IElement next = null;
            do
            {
                length = (moveInfo.CurrentIndex + 1) - startIndex;
                next = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            }
            while (next != null &&
                (ArrayIndexer.Check(moveInfo, parsingInfo, scriptInfo)
                || DataMember.Check(moveInfo, parsingInfo, scriptInfo)));

            // build
            funcCall.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.MoveToIndex(startIndex);
            parentInfo.Replace(length, funcCall);
        }

        private static void ParseExtern(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            FuncCall funcCall = new FuncCall();

            MoveInfo moveInfo = new MoveInfo(parentInfo);

            // path
            Path path = Path.Parse(moveInfo, parsingInfo, scriptInfo);
            if (path == null)
                throw new SyntaxException("Bad path", parentInfo.GetErrorInfo());

            funcCall._pathOrUsing = path.ToString();
            funcCall._pathElem = path;

            // ::
            moveInfo.FindNextBlack(SearchDirection.LeftToRight);

            // name
            moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            funcCall._nameElem = (Token)moveInfo.Current;
            funcCall._name = moveInfo.Current.ToString();

            #region ParsingInfo Args
            object lastCall = parsingInfo.CurrentCall;
            int? lastCallArgIndex = parsingInfo.CurrentCallArgIndex;
            parsingInfo.CurrentCall = funcCall;
            parsingInfo.CurrentCallArgIndex = 0;
            #endregion

            // args
            moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (!FuncArgs.Check(moveInfo, parsingInfo, scriptInfo))
                throw new SyntaxException("Could not parse funcArgs", parentInfo.GetErrorInfo());

            int startIndex = parentInfo.CurrentIndex;
            int length;

            #region ParsingInfo Args
            parsingInfo.CurrentCall = lastCall;
            parsingInfo.CurrentCallArgIndex = lastCallArgIndex;
            #endregion

            // find self and modifiers
            MoveInfo selfInfo = new MoveInfo(parentInfo);
            IElement self = selfInfo.FindNextBlack(SearchDirection.RightToLeft);
            if (self != null && self is ExpressionOperand) // self is self or FuncCallModifier
            {
                startIndex = selfInfo.CurrentIndex;
                if (self is FuncCallModifier) // self is FuncCallModifier -> find self
                {
                    self = selfInfo.FindNextBlack(SearchDirection.RightToLeft);
                    if (self != null && self is ExpressionOperand)
                        startIndex = selfInfo.CurrentIndex;
                }
            }

            // find members and arrayIndexers
            IElement next = null;
            do
            {
                length = (moveInfo.CurrentIndex + 1) - startIndex;
                next = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            }
            while (next != null &&
                (ArrayIndexer.Check(moveInfo, parsingInfo, scriptInfo)
                || DataMember.Check(moveInfo, parsingInfo, scriptInfo)));

            // build
            funcCall.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.MoveToIndex(startIndex);
            parentInfo.Replace(length, funcCall);
        }

        public override void CheckSemantic(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        {
            if (!String.IsNullOrEmpty(this._pathOrUsing))
                CheckSemanticExtern(treeInfo, scriptInfo, checkingInfo);
            else
                CheckSemanticLocal(treeInfo, scriptInfo, checkingInfo);

            if (_funcInfo != null)
            {
                if (Arguments.Count > _funcInfo.Parameters.Count)
                {
                    if (_funcInfo.SF.IsExtern) // codapi funcs -> only warning
                        scriptInfo.SF.Errors.Add(
                            new WarningError("Function '" + _funcInfo.ToString() + "' has more arguments than parameters in the definition",
                                treeInfo.GetErrorInfo(treeInfo.Current)));
                    else
                        scriptInfo.SF.Errors.Add(
                            new SemanticError("Function '" + _funcInfo.ToString() + "' has more arguments than parameters in the definition",
                                treeInfo.GetErrorInfo(treeInfo.Current)));
                }

                if ((_funcInfo.OptParamStartIndex != null && Arguments.Count < _funcInfo.OptParamStartIndex)
                    || (_funcInfo.OptParamStartIndex == null && Arguments.Count < _funcInfo.Parameters.Count))
                    scriptInfo.SF.Errors.Add(
                        new WarningError("Could not find enough arguments, function '" + _funcInfo.ToString() + "'",
                            treeInfo.GetErrorInfo(treeInfo.Current)));

                FuncRefInfo funcRefInfo = new FuncRefInfo(scriptInfo.SF, _funcInfo, this.CharIndex, this.CharLength, 
                    checkingInfo.SC.SourceCode.Substring(this.CharIndex, this.CharLength), true);
                foreach (Expression arg in this.Arguments)
                    funcRefInfo.AddArgument(arg.CharIndex, arg.CharLength);

                scriptInfo.References.Add(funcRefInfo);
            }
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
                        treeInfo.GetErrorInfo(_pathElem)));
                    return;
                }

                _path = usingInfoTry.SFPath;
                UsingName usingName = UsingName.ConvertToMe(this, _pathElem, usingInfoTry);

                scriptInfo.References.Add(new UsingRefInfo(scriptInfo.SF, usingInfoTry,
                    usingName.CharIndex, usingName.CharLength, checkingInfo.SC.SourceCode.Substring(usingName.CharIndex, usingName.CharLength)));
            }
            #endregion

            ScriptFile sf = scriptInfo.SF.Manager.GetSF(_path);
            if (sf == null)
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

            _funcInfo = sf.SI.FindLocalFunc(_name);
            if (_funcInfo == null)
            {
                scriptInfo.SF.Errors.Add(
                    new SemanticError("Unknown function '" + _path + "::" + _name + "'",
                        treeInfo.GetErrorInfo(treeInfo.Current)));
                return;
            }
            
            // member is private
            if (sf != scriptInfo.SF && _funcInfo.Access != MemberAccess.Public)
            {
                scriptInfo.SF.Errors.Add(
                    new SemanticError("Cannot access function '" + _path + "::" + _name + "'",
                        treeInfo.GetErrorInfo(treeInfo.Current)));
            }
        }

        private void CheckSemanticLocal(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        {
            _funcInfo = scriptInfo.FindLocalFunc(_name);

            if (_funcInfo == null)
                _funcInfo = scriptInfo.FindGlobalsFunc(_name);

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

                if (_funcInfo.Access != MemberAccess.Public) // private member in include
                {
                    scriptInfo.SF.Errors.Add(
                        new SemanticError("Cannot access function '" + _funcInfo.ToString() + "'",
                            treeInfo.GetErrorInfo(treeInfo.Current)));
                }
            }
        }

        public void Compile_SpecifyPath(BaseTree baseTree)
        {
            if (String.IsNullOrEmpty(_path) && !_funcInfo.SF.SI.IsGlobal) // path is not specify
            {
                _path = baseTree.GetActualSFPath(_funcInfo.SF.SFPath);
                IElement pathE = new Path(_funcInfo.SF.SFPath, _path);

                int nameI = this.children.IndexOf(_nameElem);
                this.children.Insert(nameI, Token.Namespace);
                this.children.Insert(nameI, pathE);
            }
        }
    }
}
