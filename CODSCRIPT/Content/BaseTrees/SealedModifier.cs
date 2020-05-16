using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class SealedModifier : SimpleTree
    {
        private SealedModifier(IElement elem)
            : base(new List<IElement> { elem })
        {
        }

        public override IElement CreateCopy()
        {
            return new SealedModifier(children[0].CreateCopy());
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("sealed"))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            SealedModifier modifier = new SealedModifier(parentInfo.Current);
            parentInfo.Replace(1, modifier);
        }

        /// <summary>
        /// Nájde modifier, kt. sa nachádza pred pozíciou moveInfo
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <returns></returns>
        public static SealedModifier GetModifier(MoveInfo moveInfo)
        {
            IElement modifierTry = moveInfo.FindNextBlack(SearchDirection.RightToLeft);
            if (modifierTry != null && modifierTry is SealedModifier)
                return (SealedModifier)moveInfo.Current;

            return null;
        }
    }
}
