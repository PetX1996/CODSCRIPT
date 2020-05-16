using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class FuncCallModifier : ExpressionOperand
    {
        private FuncCallModifier(IElement elem)
            : base(new List<IElement>() { elem })
        { 
        }

        public override IElement CreateCopy()
        {
            return new FuncCallModifier(children[0].CreateCopy());
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word))
            {
                if (parentInfo.Current.ToString().EqualCode("thread"))
                {
                    Parse(parentInfo, parsingInfo, scriptInfo);
                    return true;
                }
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            FuncCallModifier modifier = new FuncCallModifier(parentInfo.Current);
            parentInfo.Replace(1, modifier);
        }
    }
}
