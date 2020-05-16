using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    class ArrayContentDef : ExpressionOperand
    {
        private bool _isEmpty;

        private Dictionary<StrIntIndex, IElement> _content;
        private int _contentIntsCount;

        private ArrayContentDef()
            : base()
        {
            _content = new Dictionary<StrIntIndex, IElement>();
            _contentIntsCount = 0;
        }

        public override IElement CreateCopy()
        {
            ArrayContentDef e = new ArrayContentDef();
            e.AddChildren(this.CopyChildren());

            e._isEmpty = _isEmpty;

            e._contentIntsCount = _contentIntsCount;
            ScopeGroup thisScope = (ScopeGroup)this.children[0];
            ScopeGroup eScope = (ScopeGroup)e.children[0];
            foreach (StrIntIndex index in _content.Keys)
            {
                IElement value = _content[index];
                e._content.Add(index, eScope.GetChildren()[thisScope.GetChildren().IndexOf(value)]);
            }

            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current is ScopeGroup)
            {
                Parse(parentInfo, parsingInfo, scriptInfo);
                return true;
            }
            return false;
        }

        private static ArrayContentDef Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            ArrayContentDef arrayDef = new ArrayContentDef();

            ScopeGroup group = (ScopeGroup)parentInfo.Current;

            MoveInfo moveInfo = new MoveInfo(group, SearchTree.ContentBlock, 0, parentInfo);
            IElement tryNext = moveInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);

            // only for strIndex
            MoveInfo strIndexerInfo = new MoveInfo(moveInfo);
            IElement tryAssign = strIndexerInfo.FindNextBlack(SearchDirection.LeftToRight);

            if (tryNext == null) // { }
                arrayDef._isEmpty = true;

            while (!arrayDef._isEmpty)
            {
                if (tryNext == null)
                    throw new SyntaxException("Could not find next element in ArrayContentDef", parentInfo.GetErrorInfo());
                else if (tryNext is ScopeGroup) // { {...} }
                {
                    ArrayContentDef contentDef = ArrayContentDef.Parse(moveInfo, parsingInfo, scriptInfo);
                    arrayDef._content.Add(new StrIntIndex(arrayDef._contentIntsCount++), contentDef);

                    tryNext = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
                }
                else if (tryNext.IsTT(TokenType.Word) && tryAssign != null && tryAssign.IsTT(TokenType.Assign)) // { Name = "MyName" }
                {
                    string strIndex = tryNext.ToString();
                    IElement strValue = null;

                    IElement strValueTry = strIndexerInfo.FindNextBlack(SearchDirection.LeftToRight); // move behind "="
                    if (strValueTry == null)
                        throw new SyntaxException("Could not find value for strIndex in ArrayContentDef", parentInfo.GetErrorInfo());

                    if (strValueTry is ScopeGroup) // { Name = {...} }
                    {
                        strValue = ArrayContentDef.Parse(strIndexerInfo, parsingInfo, scriptInfo);
                    }
                    else
                    {
                        strValue = Expression.Parse(strIndexerInfo, parsingInfo, scriptInfo);
                        if (strValue == null)
                            throw new SyntaxException("Could not parse expression for strIndex in ArrayContentDef", parentInfo.GetErrorInfo());
                    }

                    StrIntIndex newIndex = new StrIntIndex(strIndex);
                    StrIntIndex createdIndex = arrayDef._content.Keys.FirstOrDefault(a => a == newIndex); // index may have been already defined in this def..
                    if (createdIndex != null)
                        scriptInfo.SF.Errors.Add(new SemanticError("ArrayContentDef already contains key '" + strIndex + "'", new ErrorInfo(moveInfo.GetErrorInfo())));
                    else
                        arrayDef._content.Add(newIndex, strValue);

                    tryNext = strIndexerInfo.FindNextBlack(SearchDirection.LeftToRight);
                    moveInfo = strIndexerInfo;
                }
                else // { 1, "dawd", self GetGuid() }
                {
                    Expression simpleExp = Expression.Parse(moveInfo, parsingInfo, scriptInfo);
                    if (simpleExp == null)
                        throw new SyntaxException("Could not parse expression in ArrayContentDef", parentInfo.GetErrorInfo());

                    arrayDef._content.Add(new StrIntIndex(arrayDef._contentIntsCount++), simpleExp);

                    tryNext = moveInfo.FindNextBlack(SearchDirection.LeftToRight);
                }

                if (tryNext == null) // end of def
                    break;
                else if (tryNext.IsTT(TokenType.Comma)) // new elem...
                {
                    tryNext = moveInfo.FindNextBlack(SearchDirection.LeftToRight);

                    // only for strIndex
                    strIndexerInfo = new MoveInfo(moveInfo);
                    tryAssign = strIndexerInfo.FindNextBlack(SearchDirection.LeftToRight);
                    continue;
                }
                else // WTF?!
                    throw new SyntaxException("Unexpected token '" + tryNext.ToString() + "' in ArrayContentDef", parentInfo.GetErrorInfo());
            }

            arrayDef.AddChildren(group);
            parentInfo.Replace(1, arrayDef);
            return arrayDef;
        }

        private class StrIntIndex
        {
            private int _int;
            private string _str;

            public StrIntIndex(int index)
            {
                _str = null;

                _int = index;
            }

            public StrIntIndex(string index)
            {
                if (index == null)
                    index = string.Empty;

                _str = index;
            }

            public object Index { get { return _str != null ? _str : (object)_int; } }

            #region Operator overloading
            public static bool operator ==(StrIntIndex l, StrIntIndex r)
            {
                if ((object)l == null && (object)r == null)
                    return true;
                else if ((object)l == null || (object)r == null)
                    return false;

                return l.Equals(r);
            }

            public static bool operator !=(StrIntIndex l, StrIntIndex r)
            {
                return !(r == l);
            }

            public bool Equals(StrIntIndex obj)
            {
                if (obj == null)
                    return false;

                return AreValuesEqual(this, obj);
            }

            public override bool Equals(object obj)
            {
                if (obj == null)
                    return false;

                StrIntIndex index = obj as StrIntIndex;
                if ((object)index == null)
                    return false;

                return AreValuesEqual(this, index);
            }

            private static bool AreValuesEqual(StrIntIndex a, StrIntIndex b)
            {
                if (a._str != null && b._str != null)
                    return a._str == b._str;
                else if (a._str == null && b._str == null)
                    return a._int == b._int;
                else
                    return false;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
            #endregion
        }

        private void GetIndexesAndValues(ArrayContentDef arrayDef, ref Dictionary<Queue<StrIntIndex>, IElement> indexesForValues, Queue<StrIntIndex> lastQueue)
        {
            Queue<StrIntIndex> indexesQueue;
            foreach (StrIntIndex curI in arrayDef._content.Keys)
            {
                if (lastQueue != null)
                    indexesQueue = new Queue<StrIntIndex>(lastQueue);
                else
                    indexesQueue = new Queue<StrIntIndex>();

                indexesQueue.Enqueue(curI);

                IElement curElem = arrayDef._content[curI];

                // empty array {} | []
                if ((curElem is ArrayContentDef && ((ArrayContentDef)curElem)._isEmpty)
                    || (curElem is Expression && ((Expression)curElem).GetChildren()[0] is ArrayDef))
                { 
                    IElement emptyArray = new BallOfMud(new List<IElement> { Token.SQBracketOpen, Token.SQBracketClose });
                    indexesForValues.Add(indexesQueue, emptyArray);
                }
                else if (curElem is ArrayContentDef) // { {...} }
                {
                    GetIndexesAndValues((ArrayContentDef)curElem, ref indexesForValues, indexesQueue);
                }
                else if (curElem is Expression) // {"5"}
                {
                    indexesForValues.Add(indexesQueue, curElem);
                }
                else
                    throw new InvalidOperationException("Ehm...WTF?!...check Parse");
            }
        }

        public IElement GetCompiledStatements(VarName varName)
        {
            BallOfMud statements = new BallOfMud();
            statements.AddChildren(new List<IElement> {Token.ScopeOpen, Token.Space});

            Dictionary<Queue<StrIntIndex>, IElement> indexesForValues = new Dictionary<Queue<StrIntIndex>, IElement>();
            GetIndexesAndValues(this, ref indexesForValues, null);

            foreach (Queue<StrIntIndex> queue in indexesForValues.Keys)
            {
                IElement value = indexesForValues[queue];
                BallOfMud statement = new BallOfMud(new List<IElement> { varName });
                foreach (StrIntIndex curIndex in queue)
                {
                    Token index = null;
                    if (curIndex.Index is String)
                        index = new Token(TokenType.String, (string)curIndex.Index);
                    else
                        index = new Token(TokenType.Number, (int)curIndex.Index);

                    statement.AddChildren(new List<IElement> { Token.SQBracketOpen, index, Token.SQBracketClose });
                }
                statement.AddChildren(new List<IElement> { Token.Space, Token.Assign, Token.Space, value, Token.SemiColon, Token.Space });
                statements.AddChildren(statement);
            }

            statements.AddChildren(Token.ScopeClose);
            return statements;
            /*
            numbers = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};
	        numbers[0] = 0;
	        numbers[1] = 1;
	        etc...

	        vectors = { {0, 0, 0}, {5, 10, 2}, {487, 516, 487486} };
	        vector[0][0] = 0;
	        vector[0][1] = 0;
	        vector[0][2] = 0;
	        vector[1][0] = 5;
	        vector[1][1] = 10;
	        vector[1][2] = 2;
	        vector[2][0] = 487;
	        vector[2][1] = 516;
	        vector[2][2] = 487486;

	        menus = { { {"Item0_0_0", "Item0_0_1"}, {"Item0_1_0", "Item0_1_1"} }, { {"Item1_0_0", "Item1_0_1"}, {"Item1_1_0", "Item1_1_1"} } };
	        menus[0][0][0] = "Item0_0_0";
	        menus[0][0][1] = "Item0_0_1";
	        menus[0][1][0] = "Item0_1_0";
	        menus[0][1][1] = "Item0_1_1";
	        menus[1][0][0] = "Item1_0_0";
	        menus[1][0][1] = "Item1_0_1";
	        menus[1][1][0] = "Item1_1_0";
	        menus[1][1][1] = "Item1_1_1";

	        personWithoutHobbys = {Name = "Peter", Age = 17, Hobbys = {}};
	        personWithoutHobbys = {Name = "Peter", Age = 17, Hobbys = []};
	        personWithoutHobbys["Name"] = "Peter";
	        personWithoutHobbys["Age"] = 17;
	        personWithoutHobbys["Hobbys"] = [];

	        person = {Name = "Peter", Age = 17, Hobbys = {"PC", "Skiing"} };
	        person["Name"] = "Peter";
	        person["Age"] = 17;
	        person["Hobbys"][0] = "PC";
	        person["Hobbys"][1] = "Skiing";

	        persons = { {Name = "Peter", Age = 17, Hobbys = {"PC", "Skiing"} }, {Name = "Andrea", Age = 21, Hobbys = {"Diving", "Cooking"} } };
	        persons[0]["Name"] = "Peter";
	        persons[0]["Age"] = 17;
	        persons[0]["Hobbys"][0] = "PC";
	        persons[0]["Hobbys"][1] = "Skiing";
	        persons[1]["Name"] = "Andrea";
	        persons[1]["Age"] = 21;
	        persons[1]["Hobbys"][0] = "Diving";
	        persons[1]["Hobbys"][1] = "Cooking";
            */
        }
    }
}
