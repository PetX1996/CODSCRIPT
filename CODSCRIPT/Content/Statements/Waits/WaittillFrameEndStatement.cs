using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class WaittillFrameEndStatement : Statement
    {
        private WaittillFrameEndStatement()
            : base()
        { 
        }

        public override IElement CreateCopy()
        {
            WaittillFrameEndStatement e = new WaittillFrameEndStatement();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static new bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word) && parentInfo.Current.ToString().EqualCode("waittillframeend"))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;        
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            WaittillFrameEndStatement waittillFrameEnd = new WaittillFrameEndStatement();
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            // terminal
            IElement terminalTry = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            if (terminalTry == null || !terminalTry.IsTT(TokenType.SemiColon))
                throw new SyntaxException("Missing directive ';'?", parentInfo.GetErrorInfo());

            // build
            int startIndex = parentInfo.CurrentIndex;
            int length = (moveInfo.CurrentIndex + 1) - startIndex;

            waittillFrameEnd.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, waittillFrameEnd);
        }
    }
}
