using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class BinaryOperator : ExpressionOperator
    {
        private BinaryOperator(IElement elem)
            : base(elem)
        {
        }

        public override IElement CreateCopy()
        {
            BinaryOperator e = new BinaryOperator(children[0].CreateCopy());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            IElement cur = parentInfo.Current;
            if (cur is Token && ((Token)cur).Type.SType == TokenType.SymbolType.BinaryOperator
            || cur.IsTT(TokenType.BitAND) || cur.IsTT(TokenType.ADD) || cur.IsTT(TokenType.SUB))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            BinaryOperator op = new BinaryOperator(parentInfo.Current);
            parentInfo.Replace(1, op);
        }
    }
}
