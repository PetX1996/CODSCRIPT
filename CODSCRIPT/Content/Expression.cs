using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace CODSCRIPT.Content
{
    public class Expression : SimpleTree
    {
        protected Expression(List<IElement> elems)
            : base(elems)
        {
        }

        protected Expression()
            : this(null)
        { 
        }

        public override IElement CreateCopy()
        {
            Expression e = new Expression(this.CopyChildren());
            return e;
        }

        public static Expression Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        { 
            return Parse(parentInfo, parsingInfo, scriptInfo, false, false, false);
        }

        public static Expression Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo, bool isExpStatement, bool isArrayContentDefEnabled, bool isArrayDefEnabled)
        {
            Expression expression = new Expression();

            IElement next = parentInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            if (next == null)
                return null;

            MoveInfo moveInfo = new MoveInfo(parentInfo);
            int startIndex = moveInfo.CurrentIndex;

            //int lastIndex = startIndex;
            //int curLength;

            bool isNextOperand = true;
            bool isNextNeeded = true;
            bool isSingleOperand;

            IElement cur = moveInfo.Current;
            while (cur != null)
            {
                //if (cur.IsTT(TokenType.SemiColon))
                    //break;

                if (!ExpressionMember.Check(moveInfo, parsingInfo, scriptInfo, ref isNextOperand, ref isNextNeeded, out isSingleOperand, isExpStatement, isArrayContentDefEnabled, isArrayDefEnabled))
                    break;

                // pridá súčasný elem + predchádzajúce medzery
                //curLength = (moveInfo.CurrentIndex + 1) - lastIndex;
                //expression.AddChildren(parentInfo.CurrentElements.GetRange(lastIndex, curLength));
                //lastIndex = moveInfo.CurrentIndex + 1;

                cur = moveInfo.FindNextBlack(SearchDirection.LeftToRight);

                if (isSingleOperand)
                {
                    // TODO: fixnúť! ..nejak...
                    //if (found > 1)
                        //throw new SyntaxException("Could not parse expression", parentInfo.ErrorInfo);

                    break;
                }
            }

            int length = moveInfo.CurrentIndex - startIndex;
            if (length == 0)
                return null;

            expression.AddChildren(parentInfo.CurrentElements.GetRange(startIndex, length));

            parentInfo.Replace(length, expression);
            return expression;
        }

        public XmlElement ToXML(XmlDocument doc, XmlElement elem, ScriptInfo si)
        {
            MoveInfo treeInfo = new MoveInfo(this, SearchTree.ChildrenTree, 0, si.SF);
            IElement curElem = treeInfo.Current;
            while (curElem != null)
            {
                if (curElem is Path)
                {
                    XmlElement pathElem = doc.CreateElement("path");
                    ((Path)curElem).ToXML(doc, pathElem, si);
                    elem.AppendChild(pathElem);
                }
                else if (curElem is Token
                    && !treeInfo.IsIn<Path>(true)) // ignore path tokens
                {
                    XmlElement tokenElem = doc.CreateElement("token");
                    ((Token)curElem).ToXML(doc, tokenElem, si);
                    elem.AppendChild(tokenElem);
                }

                curElem = treeInfo.Move(SearchDirection.LeftToRight);
            }
            return elem;
        }

        public static Expression FromXML(XmlElement elem)
        {
            List<IElement> elems = new List<IElement>(); 
            foreach (XmlElement e in elem.ChildNodes.OfType<XmlElement>())
            {
                if (e.Name == "path")
                    elems.Add(Path.FromXML(e));
                else if (e.Name == "token")
                    elems.Add(Token.FromXML(e));
            }
            return new Expression(elems);
        }
    }

    public abstract class ExpressionMember : SimpleTree
    {
        protected ExpressionMember(List<IElement> elems)
            : base(elems)
        { 
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo,
            ref bool isNextOperand, ref bool isNextNeeded, out bool isSingleOperand, bool isExpStatement, bool isArrayContentDefEnabled, bool isArrayDefEnabled)
        {
            isSingleOperand = false;
            IElement cur = parentInfo.Current;
            if (isNextOperand) // operand
            {
                // all unary operators -> without + and -
                // ex. !var
                if (!isExpStatement && UnaryOperator.Check(parentInfo, parsingInfo, scriptInfo))
                {
                    isNextOperand = true;
                    isNextNeeded = true;
                    return true;
                }

                // real operand
                // ex. VarName, FuncCall, etc.
                if (ExpressionOperand.Check(parentInfo, parsingInfo, scriptInfo, ref isSingleOperand, isArrayContentDefEnabled, isArrayDefEnabled))
                {
                    isNextOperand = false;
                    isNextNeeded = false;
                    return true;
                }
                else
                {
                    if (isNextNeeded)
                        throw new SyntaxException("Could not find operand", parentInfo.GetErrorInfo());

                    return false;
                }
            }
            else // operator
            {
                if (isExpStatement && PostfixIncDec.Check(parentInfo, parsingInfo, scriptInfo))
                {
                    isSingleOperand = ExpressionOperand.IsSingle(parentInfo, parsingInfo);
                    isNextOperand = false;
                    isNextNeeded = false;
                    return true;
                }

                if (!isExpStatement && BinaryOperator.Check(parentInfo, parsingInfo, scriptInfo))
                {
                    isNextOperand = true;
                    isNextNeeded = true;
                    return true;
                }

                if (isExpStatement && Assign.Check(parentInfo, parsingInfo, scriptInfo)) // = Expression
                {
                    isSingleOperand = ExpressionOperand.IsSingle(parentInfo, parsingInfo);
                    isNextNeeded = false;
                    isNextOperand = false;
                    return true;
                }

                // self thread f()
                if (FuncCallModifier.Check(parentInfo, parsingInfo, scriptInfo)) // thread
                {
                    isSingleOperand = ExpressionOperand.IsSingle(parentInfo, parsingInfo);
                    isNextNeeded = true;
                    isNextOperand = false;
                    return true;
                }

                // self f()
                // self thread f()
                // NOT f() !!
                if (DelegateCall.Check(parentInfo, parsingInfo, scriptInfo) // // [[delegate]](funcArgs) | [[d]]().member* | [[d]]()[]*
                    || FuncCall.Check(parentInfo, parsingInfo, scriptInfo)) // f() | path::f() | f().member* | f()[i]*
                {
                    isSingleOperand = ExpressionOperand.IsSingle(parentInfo, parsingInfo);
                    isNextOperand = false;
                    isNextNeeded = false;
                    return true;
                }

                if (isNextNeeded)
                    throw new SyntaxException("unknown operator '" + parentInfo.Current.ToString() + "'", parentInfo.GetErrorInfo());
                else
                    return false;
            }
        }
    }

    abstract class ExpressionOperator : ExpressionMember
    {
        protected ExpressionOperator(IElement elem)
            : base(new List<IElement> { elem })
        {
        }
    }

    public abstract class ExpressionOperand : ExpressionMember
    {
        protected ExpressionOperand(List<IElement> elems)
            : base(elems)
        {
        }

        protected ExpressionOperand()
            : this(null)
        { 
        }

        // TODO: dokončiť!
        // zostupne podľa priority vyhľadávania
        // konštanty sa pridružujú k iným operandom a určujú sa podľa kontextu!

        // "string"
        // "string".member
        // "string"[i]

        // &"FILE_LOCALIZED_STRING"

        // 56 | -56 | +56

        // []

        // { 1, 2, 3 }

        // (0,1,2)
        // (0,1,2)[i]

        // (subExpression)
        // (subExpression).member
        // (subExpression)[i]

        // true
        // false
        // undefined

        // thread

        // ::func
        // path::func | path::const

        // var | const
        // var.member
        // var[i]


        // as operators

        // = Expression

        // lastOperand [[delegate]]()
        // lastOperand [[delegate]]().member
        // lastOperand [[delegate]]()[i]

        // lastOperand func()
        // lastOperand func().member
        // lastOperand func()[i]
        // lastOperand path::func()
        // lastOperand path::func().member
        // lastOperand path::func()[i]

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo, ref bool isSingleOperand, bool isArrayContentDefEnabled, bool isArrayDefEnabled)
        {
            if ((isArrayDefEnabled && (ArrayDef.Check(parentInfo, parsingInfo, scriptInfo))) // []
                || (isArrayContentDefEnabled && ArrayContentDef.Check(parentInfo, parsingInfo, scriptInfo))) // { 0,1,2 }
            {
                isSingleOperand = IsSingle(parentInfo, parsingInfo);
                return true;                
            }

            if (StringArray.Check(parentInfo, parsingInfo, scriptInfo) // "str" | "str".member | "str"[]
                || LocalizedString.Check(parentInfo, parsingInfo, scriptInfo) // &"str"
                || SignedNumber.Check(parentInfo, parsingInfo, scriptInfo) // 56 | -56 | +56
                || Vector.Check(parentInfo, parsingInfo, scriptInfo) // (0,1,2) | (0,1,2)[]
                || SubExpression.Check(parentInfo, parsingInfo, scriptInfo) // (exp) | (exp).member* | (exp)[]*
                || Literal.Check(parentInfo, parsingInfo, scriptInfo) // true | false | undefined
                || FuncCallModifier.Check(parentInfo, parsingInfo, scriptInfo) // thread
                || DelegateCall.Check(parentInfo, parsingInfo, scriptInfo) // // [[delegate]](funcArgs) | [[d]]().member* | [[d]]()[]*
                || FuncCall.Check(parentInfo, parsingInfo, scriptInfo) // f() | path::f() | f().member* | f()[i]*
                || DelegateDef.Check(parentInfo, parsingInfo, scriptInfo) // ::func | path::func
                || VarName.Check(parentInfo, parsingInfo, scriptInfo) // var | var.member* | var[i]*
                )
            {
                isSingleOperand = IsSingle(parentInfo, parsingInfo);
                return true;
            }

            return false;
        }

        public static bool IsSingle(MoveInfo parentInfo, ParsingInfo parsingInfo)
        {
            // single operands
            return (parentInfo.Current is ArrayDef
                || parentInfo.Current is ArrayContentDef
                /*|| parentInfo.Current is DelegateDef*/ // maybe ConsName !!!
                || parentInfo.Current is Assign
                || parentInfo.Current is PostfixIncDec
                );
        }
    }
}
