using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class FuncArgs : ExpressionOperand
    {
        private FuncArgs()
            : base()
        {
        }

        public override IElement CreateCopy()
        {
            FuncArgs e = new FuncArgs();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current is ParenthesesGroup)
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            FuncArgs funcArgs = new FuncArgs();

            ParenthesesGroup group = (ParenthesesGroup)parentInfo.Current;
            MoveInfo groupInfo = new MoveInfo(group, SearchTree.ContentBlock, 0, parentInfo);

            IElement next = groupInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            while (next != null)
            {
                Expression exp = Expression.Parse(groupInfo, parsingInfo, scriptInfo);
                next = groupInfo.FindNextBlack(SearchDirection.LeftToRight); // jump behind funcArg
                if (exp == null
                    || (next != null && !next.IsTT(TokenType.Comma)))
                    throw new SyntaxException("Could not parse funcArg", parentInfo.GetErrorInfo());

                if (next != null)
                {
                    next = groupInfo.FindNextBlack(SearchDirection.LeftToRight); // jump behind ,
                    if (next == null)
                        throw new SyntaxException("Could not parse funcArg", parentInfo.GetErrorInfo());
                }

                CheckOutParam(parsingInfo, exp, parentInfo);

                if (parsingInfo.CurrentCall != null 
                    && parsingInfo.CurrentCallArgIndex != null)
                {
                    if (parsingInfo.CurrentCall is FuncCall)
                    {
                        ((FuncCall)parsingInfo.CurrentCall).Arguments.Add(exp);
                        parsingInfo.CurrentCallArgIndex++;
                    }
                    else if (parsingInfo.CurrentCall is DelegateCall)
                    {
                        ((DelegateCall)parsingInfo.CurrentCall).Arguments.Add(exp);
                        parsingInfo.CurrentCallArgIndex++;
                    }
                    else
                        throw new ArgumentException("parsingInfo.CurrentCall");
                }
            }

            funcArgs.AddChildren(group);
            parentInfo.Replace(1, funcArgs);
        }

        private static void CheckOutParam(ParsingInfo parsingInfo, Expression exp, MoveInfo parentInfo)
        { 
            if (parsingInfo.OutParamFuncCall != null)
            {
                FuncInfo funcInfo = parsingInfo.OutParamFuncInfo;
                if (parsingInfo.OutParamFuncArgIndex < funcInfo.Parameters.Count)
                {
                    if (funcInfo.Parameters[parsingInfo.OutParamFuncArgIndex].IsOut)
                    {
                        // check exp content
                        List<IElement> expChildren = exp.GetChildren();
                        if (expChildren.Count != 0 && (expChildren[0] is VarName)
                            && ((VarName)expChildren[0]).GetChildren().Count == 1)
                        {
                            VarName varName = (VarName)expChildren[0];
                            string varNameStr = varName.ToString();

                            parsingInfo.CurrentFunc.LocalVars.Add(new LocalVarInfo(parsingInfo.SF, varNameStr, varName.CharIndex, varName.CharLength, null, varName));
                        }
                        else
                            throw new SyntaxException("Only VarName is allowed as out parameter", parentInfo.GetErrorInfo());
                    }
                }

                parsingInfo.OutParamFuncArgIndex++;
            }
        }
    }
}
