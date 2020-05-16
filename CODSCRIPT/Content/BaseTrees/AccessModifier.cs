using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class AccessModifier : SimpleTree
    {
        private AccessModifier(IElement elem, MemberAccess type)
            : base(new List<IElement> {elem})
        {
            Type = type;
        }

        public override IElement CreateCopy()
        {
            return new AccessModifier(children[0].CreateCopy(), Type);
        }

        public MemberAccess Type { get; private set; }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word))
            {
                string word = parentInfo.Current.ToString();

                string[] modifiers = Enum.GetNames(typeof(MemberAccess));
                foreach (string m in modifiers)
                {
                    if (word.EqualCode(m))
                    {
                        Parse(parentInfo, parsingInfo, scriptInfo, m);
                        return true;
                    }
                }
            }

            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo, string word)
        {
            AccessModifier modifier = new AccessModifier(parentInfo.Current, (MemberAccess)Enum.Parse(typeof(MemberAccess), word));

            parentInfo.Replace(1, modifier);
        }

        /// <summary>
        /// Nájde modifier, kt. sa nachádza pred pozíciou moveInfo
        /// </summary>
        /// <param name="moveInfo"></param>
        /// <returns></returns>
        public static AccessModifier GetModifier(MoveInfo moveInfo, out MemberAccess access)
        {
            access = MemberAccess.Public;
            IElement modifierTry = moveInfo.FindNextBlack(SearchDirection.RightToLeft);
            if (modifierTry != null && modifierTry is AccessModifier)
            {
                AccessModifier modifier = (AccessModifier)moveInfo.Current;
                access = modifier.Type;
                return modifier;
            }
            return null;
        }
    }
}
