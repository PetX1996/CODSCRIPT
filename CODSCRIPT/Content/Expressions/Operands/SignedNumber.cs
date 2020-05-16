using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    /// <summary>
    /// Samostatné číslo alebo číslo so znamienkom + | -
    /// </summary>
    class SignedNumber : ExpressionOperand
    {
        private SignedNumber(List<IElement> elems)
            : base(elems)
        {
        }

        public override IElement CreateCopy()
        {
            SignedNumber e = new SignedNumber(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.ADD) || parentInfo.Current.IsTT(TokenType.SUB))
            {
                MoveInfo moveInfo = new MoveInfo(parentInfo);
                IElement next = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
                if (next != null && next.IsTT(TokenType.Number)) // +5
                {
                    MoveInfo prevInfo = new MoveInfo(parentInfo);
                    IElement prev = prevInfo.FindNextBlack(SearchDirection.RightToLeft);
                    if (prev == null || !(prev is ExpressionOperand)) // +5 | 5 + +5
                    {
                        ParseSigned(parentInfo, parsingInfo, scriptInfo);
                        return true;
                    }
                }
            }
            else if (parentInfo.Current.IsTT(TokenType.Number))
            {
                ParseUnsigned(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void ParseSigned(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            MoveInfo moveInfo = new MoveInfo(parentInfo);
            moveInfo.FindNextBlack(SearchDirection.LeftToRight);

            int length = ((moveInfo.CurrentIndex + 1) - parentInfo.CurrentIndex);
            SignedNumber n = new SignedNumber(parentInfo.CurrentElements.GetRange(parentInfo.CurrentIndex, length));
            parentInfo.Replace(length, n);
        }

        private static void ParseUnsigned(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            SignedNumber n = new SignedNumber(new List<IElement>() {parentInfo.Current});
            parentInfo.Replace(1, n);
        }
    }
}
