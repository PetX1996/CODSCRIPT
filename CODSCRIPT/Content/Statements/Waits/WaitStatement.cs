using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class WaitStatement : Statement
    {
        private WaitStatement()
            : base()
        { 
        }

        public override IElement CreateCopy()
        {
            WaitStatement e = new WaitStatement();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static new bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("wait"))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            WaitStatement waitStatement = new WaitStatement();
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            IElement tryExp = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (tryExp == null)
                throw new SyntaxException("Could not find wait expression", parentInfo.GetErrorInfo());

            Expression exp = Expression.Parse(moveInfo, parsingInfo, scriptInfo);
            if (exp == null)
                throw new SyntaxException("Could not parse wait expression", parentInfo.GetErrorInfo());

            IElement terminalTry = moveInfo.FindNextBlack(SearchDirection.LeftToRight);

            // terminal
            if (terminalTry == null || !terminalTry.IsTT(TokenType.SemiColon))
                throw new SyntaxException("Missing directive ';'?", parentInfo.GetErrorInfo());

            // build
            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            waitStatement.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, waitStatement);
        }
    }
}
