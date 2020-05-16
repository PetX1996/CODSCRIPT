using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class LocalizedString : SimpleTree
    {
        private LocalizedString(List<IElement> elems)
            : base(elems)
        {
        }

        public override IElement CreateCopy()
        {
            LocalizedString e = new LocalizedString(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.BitAND))
            {
                MoveInfo moveInfo = new MoveInfo(parentInfo);
                IElement next = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
                if (next != null && next.IsTT(TokenType.String))
                {
                    Parse(parentInfo, parsingInfo, scriptInfo);
                    return true;
                }
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            MoveInfo moveInfo = new MoveInfo(parentInfo);
            moveInfo.FindNextBlack(SearchDirection.LeftToRight);

            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex + 1) - startIndex;
            List<IElement> children = parentInfo.CurrentElements.GetRange(startIndex, length);

            LocalizedString s = new LocalizedString(children);
            parentInfo.Replace(length, s);
        }
    }
}
