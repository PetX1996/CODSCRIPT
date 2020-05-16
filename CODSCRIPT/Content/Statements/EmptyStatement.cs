using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class EmptyStatement : Statement
    {
        private EmptyStatement(IElement elem)
            : base(new List<IElement>() { elem })
        {
        }

        public override IElement CreateCopy()
        {
            EmptyStatement e = new EmptyStatement(children[0].CreateCopy());
            return e;
        }

        public static new bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.SemiColon))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            EmptyStatement eS = new EmptyStatement(parentInfo.Current);
            parentInfo.Replace(1, eS);
        }
    }
}
