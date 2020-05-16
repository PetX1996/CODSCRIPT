using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class ReturnStatement : Statement
    {
        private ReturnStatement()
            : base()
        { 
        }

        public override IElement CreateCopy()
        {
            ReturnStatement e = new ReturnStatement();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static new bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("return"))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            ReturnStatement returnStatement = new ReturnStatement();
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            IElement tryExp = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (tryExp == null)
                throw new SyntaxException("Missing directive ';'?", parentInfo.GetErrorInfo());

            // expression defined
            if (!tryExp.IsTT(TokenType.SemiColon))
            {
                Expression exp = Expression.Parse(moveInfo, parsingInfo, scriptInfo, false, false, true);
                if (exp == null)
                    throw new SyntaxException("Could not parse return expression", parentInfo.GetErrorInfo());

                moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            }

            // terminal
            if (moveInfo.Current == null || !moveInfo.Current.IsTT(TokenType.SemiColon))
                throw new SyntaxException("Missing directive ';'?", parentInfo.GetErrorInfo());

            // build
            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            returnStatement.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, returnStatement);
        }

        public override void CheckSemantic(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        {
            // unreachable code
            MoveInfo blockInfo = new MoveInfo(treeInfo.CurrentBlock, SearchTree.ContentBlock, treeInfo.CurrentIndex, treeInfo);
            IElement nextTry = blockInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (nextTry != null && nextTry is Statement)
                scriptInfo.SF.Errors.Add(
                    new WarningError("Unreachable code detected",
                        blockInfo.GetErrorInfo(nextTry)));
        }
    }
}
