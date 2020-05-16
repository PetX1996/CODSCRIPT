using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class IfElseStatement : Statement
    {
        private IfElseStatement()
            : base()
        {
        }

        public override IElement CreateCopy()
        {
            IfElseStatement e = new IfElseStatement();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static new bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("if"))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            IfElseStatement ifElse = new IfElseStatement();

            MoveInfo moveInfo = new MoveInfo(parentInfo);

            IElement expTry = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (expTry == null || !(expTry is ParenthesesGroup))
                throw new SyntaxException("Could not find if expression", parentInfo.GetErrorInfo());

            ParenthesesGroup expGroup = (ParenthesesGroup)expTry;
            MoveInfo expInfo = new MoveInfo(expGroup, SearchTree.ContentBlock, 0, moveInfo);

            Expression exp = Expression.Parse(expInfo, parsingInfo, scriptInfo);
            if (exp == null)
                throw new SyntaxException("Could not find if expression", parentInfo.GetErrorInfo());

            if (expInfo.FindNextBlack(SearchDirection.LeftToRight) != null)
                throw new SyntaxException("Could not parse if expression", parentInfo.GetErrorInfo());

            moveInfo.Move(SearchDirection.LeftToRight);

            if (!Statement.Check(moveInfo, parsingInfo, scriptInfo))
                throw new SyntaxException("Could not find statement", parentInfo.GetErrorInfo());

            int endIndex = moveInfo.CurrentIndex;

            IElement tryElse = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (tryElse != null && tryElse.IsTT(TokenType.Word) && moveInfo.Current.ToString() == "else")
            {
                moveInfo.Move(SearchDirection.LeftToRight);
                if (!Statement.Check(moveInfo, parsingInfo, scriptInfo))
                    throw new SyntaxException("Could not find statement", parentInfo.GetErrorInfo());

                endIndex = moveInfo.CurrentIndex;
            }

            int totalLength = (endIndex + 1) - parentInfo.CurrentIndex;
            List<IElement> children = parentInfo.CurrentElements.GetRange(parentInfo.CurrentIndex, totalLength);

            ifElse.AddChildren(children);

            parentInfo.Replace(totalLength, ifElse);
        }
    }
}
