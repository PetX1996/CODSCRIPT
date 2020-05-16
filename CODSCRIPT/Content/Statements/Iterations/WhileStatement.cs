using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class WhileStatement : IterationStatement
    {
        private WhileStatement()
            : base()
        {
        }

        public override IElement CreateCopy()
        {
            WhileStatement e = new WhileStatement();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static new bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("while"))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;   
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            WhileStatement whileStatement = new WhileStatement();
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            // expression
            IElement expGroupTry = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (expGroupTry == null || !(expGroupTry is ParenthesesGroup))
                throw new SyntaxException("Could not find while expression", parentInfo.GetErrorInfo());

            ParenthesesGroup expGroup = (ParenthesesGroup)expGroupTry;
            MoveInfo expGroupInfo = new MoveInfo(expGroup, SearchTree.ContentBlock, 0, parentInfo);
            Expression exp = Expression.Parse(expGroupInfo, parsingInfo, scriptInfo);

            if (exp == null || expGroupInfo.FindNextBlack(SearchDirection.LeftToRight) != null)
                throw new SyntaxException("Could not parse while expression", parentInfo.GetErrorInfo());

            // statement
            moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (!Statement.Check(moveInfo, parsingInfo, scriptInfo))
                throw new SyntaxException("Could not parse while statement", parentInfo.GetErrorInfo());

            // build
            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            whileStatement.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, whileStatement);
        }
    }
}
