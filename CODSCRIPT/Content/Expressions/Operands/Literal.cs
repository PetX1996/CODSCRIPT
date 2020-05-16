using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class Literal : ExpressionOperand
    {
        private Literal(IElement elem)
            : base(new List<IElement> { elem }) 
        { 
        }

        public override IElement CreateCopy()
        {
            return new Literal(children[0].CreateCopy());
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word))
            {
                string word = parentInfo.Current.ToString();
                switch (word)
                { 
                    case "true":
                    case "false":
                    case "undefined":
                        Parse(parentInfo, parsingInfo, scriptInfo);
                        return true;
                    default:
                        break;
                }
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            Literal l = new Literal(parentInfo.Current);
            parentInfo.Replace(1, l);
        }
    }
}
