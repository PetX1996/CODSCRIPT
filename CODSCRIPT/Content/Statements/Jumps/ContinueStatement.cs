using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class ContinueStatement : Statement
    {
        private ContinueStatement()
            : base()
        { 
        }

        public override IElement CreateCopy()
        {
            ContinueStatement e = new ContinueStatement();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static new bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("continue"))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;        
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            ContinueStatement continueStatement = new ContinueStatement();
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            // terminal
            IElement terminalTry = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (terminalTry == null || !terminalTry.IsTT(TokenType.SemiColon))
                throw new SyntaxException("Missing directive ';'?", parentInfo.GetErrorInfo());

            // build
            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            continueStatement.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, continueStatement);
        }

        public override void CheckSemantic(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        {
            if (!treeInfo.IsIn<IterationStatement>())
                throw new SyntaxException("Keyword 'continue' cannot use here!", treeInfo.GetErrorInfo());

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
