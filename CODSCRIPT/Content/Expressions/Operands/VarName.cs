using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    public class VarName : ExpressionOperand
    {
        private VarName()
            : base()
        {
        }

        public override IElement CreateCopy()
        {
            VarName e = new VarName();
            e.AddChildren(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.Word))
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        public static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            VarName varName = new VarName();

            MoveInfo moveInfo = new MoveInfo(parentInfo);

            int startIndex = parentInfo.CurrentIndex;
            int length;

            // find members and arrayIndexers
            IElement next = null;
            do
            {
                length = (moveInfo.CurrentIndex + 1) - startIndex;
                next = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
            }
            while (next != null &&
                (ArrayIndexer.Check(moveInfo, parsingInfo, scriptInfo)
                || DataMember.Check(moveInfo, parsingInfo, scriptInfo)));

            varName.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));
            parentInfo.Replace(length, varName);
        }

        public override void CheckSemantic(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        {
            IElement baseVar = this.GetChildren()[0];
            string baseVarName = baseVar.ToString();

            foreach (string tStr in ScriptManager.GlobalVariables)
                if (baseVarName.EqualCode(tStr))
                    return;

            if (checkingInfo.CurrentFunc != null) // inside func
            {
                LocalVarInfo tryVarInfo = checkingInfo.CurrentFunc.LocalVars.Find(a => a.Name.EqualCode(baseVarName));
                if (tryVarInfo != null && this.CharIndex >= tryVarInfo.StartIndex)
                {
                    // check "var = var;" | "var[5] = var[5] + 10"; | etc.
                    if (tryVarInfo.VarNameDef == null) // it is reference
                    {
                        tryVarInfo.RefCount++;
                        return;
                    }
                    else if (tryVarInfo.VarNameDef == this) // it is definition
                        return;
                    else if (tryVarInfo.AssignDef == null) // it is reference
                    {
                        tryVarInfo.RefCount++;
                        return;
                    }
                    else if (!treeInfo.IsInBlock(tryVarInfo.AssignDef)) // it is reference
                    {
                        tryVarInfo.RefCount++;
                        return;
                    }
                }
            }

            ConstInfo constInfo = scriptInfo.FindLocalConst(baseVarName);
            if (constInfo == null)
            {
                constInfo = scriptInfo.FindIncludesConst(baseVarName);
                if (constInfo == null)
                {
                    constInfo = scriptInfo.FindGlobalsConst(baseVarName);
                    if (constInfo == null)
                    {
                        scriptInfo.SF.Errors.Add(
                            new SemanticError("Unknown variable/constant '" + baseVarName + "'",
                                treeInfo.GetErrorInfo(treeInfo.Current)));
                        return;
                    }
                }

                if (constInfo.Access == MemberAccess.Private)
                {
                    scriptInfo.SF.Errors.Add(
                        new SemanticError("Cannot access member '" + baseVarName + "'",
                            treeInfo.GetErrorInfo(treeInfo.Current)));
                }
            }

            ToConstant(treeInfo, constInfo);
            scriptInfo.References.Add(new ConstRefInfo(scriptInfo.SF, constInfo, 
                this.CharIndex, this.CharLength, checkingInfo.SC.SourceCode.Substring(this.CharIndex, this.CharLength)));
        }

        private void ToConstant(MoveInfo treeInfo, ConstInfo constant)
        {
            ConstName constName = new ConstName(this.GetChildren(), constant);
            treeInfo.Replace(1, constName);
        }
    }
}
