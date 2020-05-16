using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class DevCode : SimpleTree
    {
        private DevCode(IElement elem)
            : base(new List<IElement>() { elem })
        {
        }

        public override IElement CreateCopy()
        {
            DevCode e = new DevCode(children[0].CreateCopy());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.DevCodeOpen)
                || parentInfo.Current.IsTT(TokenType.DevCodeClose))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            DevCode devCode = new DevCode(parentInfo.Current);
            parentInfo.Replace(1, devCode);
        }
    }
}
