using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class UnaryOperator : ExpressionOperator
    {
        private UnaryOperator(IElement elem)
            : base(elem)
        { 
        }

        public override IElement CreateCopy()
        {
            UnaryOperator e = new UnaryOperator(children[0].CreateCopy());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current is Token && ((Token)parentInfo.Current).Type.SType == TokenType.SymbolType.UnaryOperator)
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            UnaryOperator op = new UnaryOperator(parentInfo.Current);
            parentInfo.Replace(1, op);
        }
    }
}
