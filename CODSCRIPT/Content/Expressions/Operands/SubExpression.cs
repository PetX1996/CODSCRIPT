using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class SubExpression : ExpressionOperand
    {
        private SubExpression()
            : base()
        {
        }

        public override IElement CreateCopy()
        {
            SubExpression e = new SubExpression();
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
            SubExpression subExp = new SubExpression();

            ParenthesesGroup group = (ParenthesesGroup)parentInfo.Current;
            MoveInfo groupInfo = new MoveInfo(group, SearchTree.ContentBlock, 0, parentInfo);
            Expression exp = Expression.Parse(groupInfo, parsingInfo, scriptInfo);

            if (exp == null
                || groupInfo.FindNextBlack(SearchDirection.LeftToRight) != null)
                throw new SyntaxException("Could not parse subExpression", parentInfo.GetErrorInfo());

            int startIndex = parentInfo.CurrentIndex;
            int length = 1;

            MoveInfo moveInfo = new MoveInfo(parentInfo);
            IElement next = null;
            do
            {
                length = (moveInfo.CurrentIndex + 1) - startIndex;
                next = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            }
            while (next != null &&
                (ArrayIndexer.Check(moveInfo, parsingInfo, scriptInfo)
                || DataMember.Check(moveInfo, parsingInfo, scriptInfo)));

            subExp.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, subExp);
        }
    }
}
