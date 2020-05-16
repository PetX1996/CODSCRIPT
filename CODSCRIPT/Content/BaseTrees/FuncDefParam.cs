using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class FuncDefParam : SimpleTree
    {
        public string Name { get; private set; }

        private SQBracketGroup _group;
        public VarName VarName { get; private set; }

        public bool Optional { get; private set; }

        private FuncDefParam()
            : base()
        { 
        }

        public override IElement CreateCopy()
        {
            FuncDefParam e = new FuncDefParam();
            e.AddChildren(this.CopyChildren());
            e.Name = Name;
            e.Optional = Optional;

            if (_group != null)
            {
                e._group = (SQBracketGroup)e.children[this.children.IndexOf(_group)];
                e.VarName = (VarName)e._group.GetChildren()[this._group.GetChildren().IndexOf(VarName)];
            }
            else
                e.VarName = (VarName)e.children[this.children.IndexOf(VarName)];

            return e;
        }

        public static FuncDefParam Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            FuncDefParam param = new FuncDefParam();

            MoveInfo moveInfo = new MoveInfo(parentInfo);

            MoveInfo wordMoveInfo = moveInfo;
            IElement wordTry = moveInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            int startIndex = moveInfo.CurrentIndex;

            if (wordTry == null)
                throw new SyntaxException("Could not parse FuncDef param", parentInfo.GetErrorInfo());

            if (wordTry is SQBracketGroup)
            {
                param._group = (SQBracketGroup)wordTry;
                param.Optional = true;

                MoveInfo bracketInfo = new MoveInfo((SQBracketGroup)wordTry, SearchTree.ContentBlock, 0, moveInfo);
                wordTry = bracketInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
                wordMoveInfo = bracketInfo;
            }

            if (wordTry == null || !wordTry.IsTT(TokenType.Word))
                throw new SyntaxException("Could not parse FuncDef param", parentInfo.GetErrorInfo());

            VarName.Parse(wordMoveInfo, parsingInfo, scriptInfo);
            param.VarName = (VarName)wordMoveInfo.Current;

            param.Name = wordTry.ToString();
            param.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, moveInfo.CurrentIndex - startIndex + 1));
            parentInfo.Replace((moveInfo.CurrentIndex + 1) - startIndex, param);
            return param;
        }
    }
}
