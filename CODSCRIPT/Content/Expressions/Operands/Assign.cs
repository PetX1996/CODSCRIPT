using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    public class Assign : ExpressionOperand
    {
        public VarName VarName { get; private set; }
        public Expression Exp { get; private set; }

        private Assign()
            : base()
        {
        }

        public override IElement CreateCopy()
        {
            Assign e = new Assign();
            e.AddChildren(this.CopyChildren());

            e.VarName = (VarName)e.children[this.children.IndexOf(VarName)];
            e.Exp = (Expression)e.children[this.children.IndexOf(Exp)];

            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current is Token)
            {
                TokenType tt = ((Token)parentInfo.Current).Type;
                if (tt.SType == TokenType.SymbolType.Assign)
                {
                    Parse(parentInfo, parsingInfo, scriptInfo);
                    return true;
                }
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            Assign assign = new Assign();

            int startIndex;
            int length;

            // find var define
            MoveInfo varInfo = new MoveInfo(parentInfo);
            IElement var = varInfo.FindNextBlack(SearchDirection.RightToLeft);
            if (var == null || !(var is ExpressionOperand) || !(var is VarName))
                throw new SyntaxException("Could not parse Assign VarName", parentInfo.GetErrorInfo());

            assign.VarName = (VarName)var;

            startIndex = varInfo.CurrentIndex;

            // parse expression
            MoveInfo moveInfo = new MoveInfo(parentInfo);

            moveInfo.FindNextBlack(SearchDirection.LeftToRight); // move behind =
            Expression exp = Expression.Parse(moveInfo, parsingInfo, scriptInfo, false, true, true);
            if (exp == null)
                throw new SyntaxException("Could not parse Assign Expression", parentInfo.GetErrorInfo());

            assign.Exp = exp;

            // build
            length = (moveInfo.CurrentIndex + 1) - startIndex;
            assign.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));

            parentInfo.MoveToIndex(startIndex);
            parentInfo.Replace(length, assign);

            // add local var def
            IElement baseVar = ((VarName)var).GetChildren()[0];
            string baseVarName = baseVar.ToString();

            foreach (string tStr in ScriptManager.GlobalVariables)
                if (baseVarName.EqualCode(tStr))
                    return;

            LocalVarInfo tryVarInfo = parsingInfo.CurrentFunc.LocalVars.Find(a => a.Name.EqualCode(baseVarName)); // there is maybe var with this name...
            if (tryVarInfo == null)
                parsingInfo.CurrentFunc.LocalVars.Add(new LocalVarInfo(parsingInfo.SF, baseVarName, baseVar.CharIndex, baseVar.CharLength, assign, (VarName)var));
        }
    }
}
