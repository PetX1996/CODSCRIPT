using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class StringArray : ExpressionOperand
    {
        private StringArray()
            : base()
        {
        }

        public override IElement CreateCopy()
        {
            StringArray e = new StringArray();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.String))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            StringArray strArray = new StringArray();

            MoveInfo moveInfo = new MoveInfo(parentInfo);
            IElement next = moveInfo.FindNextBlack(SearchDirection.LeftToRight);

            int length = 1;
            if (next != null &&
                (ArrayIndexer.Check(moveInfo, parsingInfo, scriptInfo)
                || DataMember.Check(moveInfo, parsingInfo, scriptInfo)))
                length = (moveInfo.CurrentIndex + 1) - parentInfo.CurrentIndex;

            strArray.AddChildren(parentInfo.CurrentElements.GetRange(parentInfo.CurrentIndex, length));
            parentInfo.Replace(length, strArray);
        }
    }
}
