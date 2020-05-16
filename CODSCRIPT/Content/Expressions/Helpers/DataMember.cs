using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class DataMember : ExpressionOperand
    {
        private DataMember(List<IElement> elems)
            : base(elems)
        {
        }

        public override IElement CreateCopy()
        {
            DataMember e = new DataMember(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Ref))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        public static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            MoveInfo moveInfo = new MoveInfo(parentInfo);
            IElement next = moveInfo.FindNextBlack(SearchDirection.LeftToRight);

            if (next == null || !next.IsTT(TokenType.Word))
                throw new SyntaxException("Could not find member", parentInfo.GetErrorInfo());

            int length = (moveInfo.CurrentIndex + 1) - parentInfo.CurrentIndex;
            DataMember m = new DataMember(parentInfo.CurrentElements.GetRange(parentInfo.CurrentIndex, length));

            parentInfo.Replace(length, m);
        }
    }
}
