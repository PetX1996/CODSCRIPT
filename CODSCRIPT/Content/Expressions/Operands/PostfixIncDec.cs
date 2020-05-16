using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class PostfixIncDec : ExpressionOperand
    {
        private PostfixIncDec()
            : base()
        { 
        }

        public override IElement CreateCopy()
        {
            PostfixIncDec e = new PostfixIncDec();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.INC)
                || parentInfo.Current.IsTT(TokenType.DEC))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            PostfixIncDec postfixIncDec = new PostfixIncDec();

            int startIndex = parentInfo.CurrentIndex;
            int length;

            // find operand
            MoveInfo operandInfo = new MoveInfo(parentInfo);
            IElement operand = operandInfo.FindNextBlack(SearchDirection.RightToLeft);
            if (operand != null && operand is ExpressionOperand)
                startIndex = operandInfo.CurrentIndex;
            else
                throw new SyntaxException("Could not find PostfixIncDec operand", parentInfo.GetErrorInfo());

            // build
            length = (parentInfo.CurrentIndex + 1) - startIndex;
            postfixIncDec.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));

            parentInfo.MoveToIndex(startIndex);
            parentInfo.Replace(length, postfixIncDec);
        }
    }
}
