using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace CODSCRIPT.Content
{
    internal class Token : IElement
    {
        public int CharIndex { get; private set; }
        public int CharLength { get; private set; }
        public int LineIndex { get; private set; }

        public bool Visible { get; private set; }

        public TokenType Type { get; private set; }
        public string StringContent { get; private set; }

        public Token(int startIndex, int length, int lineIndex, TokenType type, string content)
        {
            CharIndex = startIndex;
            CharLength = length;
            LineIndex = lineIndex;
            Type = type;
            StringContent = content;
            Visible = type.Visible;
        }

        public Token(TokenType type, string content)
        {
            Type = type;
            Visible = type.Visible;
            StringContent = content;
        }

        public Token(TokenType type, int content)
        {
            Type = type;
            Visible = type.Visible;
            StringContent = content.ToString();
        }

        private Token(TokenType type)
        {
            Type = type;
            Visible = type.Visible;
            StringContent = type.Symbol;
        }

        public IElement CreateCopy()
        {
            Token t = new Token(CharIndex, CharLength, LineIndex, Type, StringContent);
            return t;
        }

        #region Parsing
        public static List<IElement> Parse(Scanner scanner)
        {
            int c = scanner.Current;
            List<IElement> t = new List<IElement>();
            if (IsWhiteSpace(c)) // white space
            {
                if (c == '\n' || c == '\r' || c == '\t' || c == ' ')
                    t.Add(new Token(scanner.CurIndex, 1, scanner.CurLineIndex, TokenType.WhiteSpace, ((char)c).ToString()));
                else
                    throw new SyntaxException("Unknown white character ASCII(dec): " + c, scanner.ErrorInfo);

                scanner.MoveNext();
            }
            else if (IsXMLComment(c, scanner)) // XML comment
            {
                int startI = scanner.CurIndex + 3;
                int end = scanner.FindEndOfLine();
                t.Add(new Token(startI - 3, end - (startI - 3), scanner.CurLineIndex, TokenType.XMLComment, scanner.SourceCode.Substring(startI, end - startI)));
            }
            else if (IsLineComment(c, scanner)) // Line comment
            {
                int startI = scanner.CurIndex + 2;
                int end = scanner.FindEndOfLine();
                t.Add(new Token(startI - 2, end - (startI - 2), scanner.CurLineIndex, TokenType.LineComment, scanner.SourceCode.Substring(startI, end - startI)));
            }
            else if (IsBlockComment(c, scanner)) // Block comment
            {
                int startI = scanner.CurIndex + 2;
                int startLine = scanner.CurLineIndex;

                scanner.TryMoveTo(1); // move behind '/*'

                if (!scanner.TryMoveTo("*/", false))
                    throw new SyntaxException("Could not find '*/'", scanner.ErrorInfo);

                t.Add(new Token(startI - 2, scanner.CurIndex - (startI - 2), startLine, TokenType.BlockComment, scanner.SourceCode.Substring(startI, scanner.CurIndex - startI)));
                scanner.TryMoveTo(2);
            }
            else if (IsString(c)) // string
            {
                int startI = scanner.CurIndex + 1;
                int startLine = scanner.CurLineIndex;

                do
                {
                    if (!scanner.TryMoveTo("\"", false))
                        throw new SyntaxException("Could not find '\"'", scanner.ErrorInfo);
                }
                while (scanner.Get(-1) == '\\' && scanner.Get(-2) != '\\'); // ignore \" in string, not \\

                t.Add(new Token(startI - 1, (scanner.CurIndex + 1) - (startI - 1), startLine, TokenType.String, scanner.SourceCode.Substring(startI, scanner.CurIndex - startI)));
                scanner.MoveNext();
            }
            else if (IsDigit(c, scanner.Get(1)))
            {
                int startI = scanner.CurIndex;

                do
                    scanner.MoveNext();
                while (IsDigit(scanner.Current, scanner.Get(1)));

                t.Add(new Token(startI, scanner.CurIndex - startI, scanner.CurLineIndex, TokenType.Number, scanner.SourceCode.Substring(startI, scanner.CurIndex - startI)));
            }
            else if (IsWord(c, true))
            {
                int startI = scanner.CurIndex;

                do
                    scanner.MoveNext();
                while (IsWord(scanner.Current, false));

                t.Add(new Token(startI, scanner.CurIndex - startI, scanner.CurLineIndex, TokenType.Word, scanner.SourceCode.Substring(startI, scanner.CurIndex - startI)));
            }
            else
            {
                int startI = scanner.CurIndex;

                do
                    scanner.MoveNext();
                while (IsSymbol(scanner.Current, scanner));

                t.AddRange(TokenType.ParseSymbols(scanner.SourceCode.Substring(startI, scanner.CurIndex - startI), scanner));
            }
            return t;
        }

        private static bool IsWhiteSpace(int c)
        {
            return c < 33;
        }

        private static bool IsDigit(int c)
        {
            return (c >= '0' && c <= '9');
        }

        private static bool IsDigit(int c, int next)
        {
            return (IsDigit(c) || (c == '.' && IsDigit(next)));
        }

        private static bool IsWord(int c, bool isFirst)
        {
            return ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_' || (!isFirst && IsDigit(c)));
        }

        private static bool IsLineComment(int c, Scanner scanner)
        {
            return (c == '/' && scanner.Get(1) == '/' && 
                (scanner.Get(2) != '/' || (scanner.Get(2) == '/' && scanner.Get(3) == '/')));
        }

        private static bool IsXMLComment(int c, Scanner scanner)
        {
            return (c == '/' && scanner.Get(1) == '/' && scanner.Get(2) == '/' && scanner.Get(3) != '/');
        }

        private static bool IsBlockComment(int c, Scanner scanner)
        {
            return (c == '/' && scanner.Get(1) == '*');
        }

        private static bool IsString(int c)
        {
            return c == '"';
        }

        private static bool IsSymbol(int c, Scanner scanner)
        {
            return (!IsWhiteSpace(c) 
                && !IsLineComment(c, scanner) 
                && !IsXMLComment(c, scanner) 
                && !IsBlockComment(c, scanner)
                && !IsString(c)
                && !IsDigit(c, scanner.Get(1)) 
                && !IsWord(c, true));
        }
        #endregion

        public virtual void CheckSemantic(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo)
        { 
        }

        public virtual void Compile(MoveInfo treeInfo, ScriptInfo scriptInfo, CompilingInfo compilingInfo)
        { 
        }

        public XmlElement ToXML(XmlDocument doc, XmlElement elem, ScriptInfo si)
        {
            elem.SetAttribute("tokenType", Type.GetName());
            elem.InnerText = StringContent;
            return elem;
        }

        public static Token FromXML(XmlElement elem)
        { 
            string type = elem.GetAttribute("tokenType");
            string content = elem.InnerText;
            return new Token(TokenType.GetByName(type), content);
        }

        public override string ToString()
        {
            if(Type == TokenType.WhiteSpace 
                || Type == TokenType.Word 
                || Type == TokenType.Number)
                return this.StringContent;
            else if(Type == TokenType.String)
                return "\"" + this.StringContent + "\"";
            else if(Type == TokenType.XMLComment)
                return "///" + this.StringContent;
            else if(Type == TokenType.LineComment)
                return "//" + this.StringContent;
            else if(Type == TokenType.BlockComment)
                return "/*" + this.StringContent + "*/";
            else
                return this.StringContent;
        }

        #region Tokens for replacing
        // spaces
        public static readonly Token Space = new Token(TokenType.WhiteSpace, " ");
        public static readonly Token EOL = new Token(TokenType.WhiteSpace, "\r\n");
        public static readonly Token TAB = new Token(TokenType.WhiteSpace, "\t");

        // symbols
        public static readonly Token AssignRShift = new Token(TokenType.AssignRShift);
        public static readonly Token AssignLShift = new Token(TokenType.AssignLShift);
        public static readonly Token LogicAND = new Token(TokenType.LogicAND);
        public static readonly Token LogicOR = new Token(TokenType.LogicOR);
        public static readonly Token LogicGEQ = new Token(TokenType.LogicGEQ);
        public static readonly Token LogicLEQ = new Token(TokenType.LogicLEQ);
        public static readonly Token LogicEQ = new Token(TokenType.LogicEQ);
        public static readonly Token LogicUNEQ = new Token(TokenType.LogicUNEQ);
        public static readonly Token AssignMUL = new Token(TokenType.AssignMUL);
        public static readonly Token AssignDIV = new Token(TokenType.AssignDIV);
        public static readonly Token AssignMOD = new Token(TokenType.AssignMOD);
        public static readonly Token AssignADD = new Token(TokenType.AssignADD);
        public static readonly Token AssignSUB = new Token(TokenType.AssignSUB);
        public static readonly Token INC = new Token(TokenType.INC);
        public static readonly Token DEC = new Token(TokenType.DEC);
        public static readonly Token AssignBitAND = new Token(TokenType.AssignBitAND);
        public static readonly Token AssignBitOR = new Token(TokenType.AssignBitOR);
        public static readonly Token AssignBitXOR = new Token(TokenType.AssignBitXOR);
        public static readonly Token RShift = new Token(TokenType.RShift);
        public static readonly Token LShift = new Token(TokenType.LShift);
        public static readonly Token Namespace = new Token(TokenType.Namespace);
        public static readonly Token DevCodeOpen = new Token(TokenType.DevCodeOpen);
        public static readonly Token DevCodeClose = new Token(TokenType.DevCodeClose);
        public static readonly Token MUL = new Token(TokenType.MUL);
        public static readonly Token DIV = new Token(TokenType.DIV);
        public static readonly Token MOD = new Token(TokenType.MOD);
        public static readonly Token ADD = new Token(TokenType.ADD);
        public static readonly Token SUB = new Token(TokenType.SUB);
        public static readonly Token Assign = new Token(TokenType.Assign);
        public static readonly Token BitAND = new Token(TokenType.BitAND);
        public static readonly Token BitOR = new Token(TokenType.BitOR);
        public static readonly Token BitXOR = new Token(TokenType.BitXOR);
        public static readonly Token BitNOT = new Token(TokenType.BitNOT);
        public static readonly Token LogicNOT = new Token(TokenType.LogicNOT);
        public static readonly Token LogicGreater = new Token(TokenType.LogicGreater);
        public static readonly Token LogicLess = new Token(TokenType.LogicLess);
        public static readonly Token Ref = new Token(TokenType.Ref);
        public static readonly Token Comma = new Token(TokenType.Comma);
        public static readonly Token SemiColon = new Token(TokenType.SemiColon);
        public static readonly Token Colon = new Token(TokenType.Colon);
        public static readonly Token ParenthesesOpen = new Token(TokenType.ParenthesesOpen);
        public static readonly Token ParenthesesClose = new Token(TokenType.ParenthesesClose);
        public static readonly Token ScopeOpen = new Token(TokenType.ScopeOpen);
        public static readonly Token ScopeClose = new Token(TokenType.ScopeClose);
        public static readonly Token SQBracketOpen = new Token(TokenType.SQBracketOpen);
        public static readonly Token SQBracketClose = new Token(TokenType.SQBracketClose);
        public static readonly Token BackSlash = new Token(TokenType.BackSlash);
        public static readonly Token PreComp = new Token(TokenType.PreComp);

        //other
        public static readonly Token True = new Token(TokenType.Word, "true");
        public static readonly Token False = new Token(TokenType.Word, "false");
        #endregion
    }

    public class TokenType
    {
        public enum SymbolType
        {
            None,
            Assign,
            Hacked,
            UnaryOperator,
            BinaryOperator,
            Other
        };

        private static List<TokenType> SymbolList = new List<TokenType>();
        private static int maxLength = 0;

        #region Types
        public static readonly TokenType WhiteSpace = new TokenType(false);

        public static readonly TokenType BlockComment = new TokenType(false);
        public static readonly TokenType LineComment = new TokenType(false);
        public static readonly TokenType XMLComment = new TokenType(false);

        public static readonly TokenType String = new TokenType(true);
        public static readonly TokenType Number = new TokenType(true);
        public static readonly TokenType Word = new TokenType(true);

        // longer string first
        /// <summary>
        /// >>=
        /// </summary>
        public static readonly TokenType AssignRShift = new TokenType(">>=", SymbolType.Assign);
        /// <summary>
        /// &lt;&lt;=
        /// </summary>
        public static readonly TokenType AssignLShift = new TokenType("<<=", SymbolType.Assign);

        /// <summary>
        /// &&
        /// </summary>
        public static readonly TokenType LogicAND = new TokenType("&&", SymbolType.BinaryOperator, 2);
        /// <summary>
        /// ||
        /// </summary>
        public static readonly TokenType LogicOR = new TokenType("||", SymbolType.BinaryOperator, 2);
        /// <summary>
        /// >=
        /// </summary>
        public static readonly TokenType LogicGEQ = new TokenType(">=", SymbolType.BinaryOperator, 2);
        /// <summary>
        /// &lt;=
        /// </summary>
        public static readonly TokenType LogicLEQ = new TokenType("<=", SymbolType.BinaryOperator, 2);
        /// <summary>
        /// ==
        /// </summary>
        public static readonly TokenType LogicEQ = new TokenType("==", SymbolType.BinaryOperator, 2);
        /// <summary>
        /// !=
        /// </summary>
        public static readonly TokenType LogicUNEQ = new TokenType("!=", SymbolType.BinaryOperator, 2);

        /// <summary>
        /// *=
        /// </summary>
        public static readonly TokenType AssignMUL = new TokenType("*=", SymbolType.Assign);
        /// <summary>
        /// /=
        /// </summary>
        public static readonly TokenType AssignDIV = new TokenType("/=", SymbolType.Assign);
        /// <summary>
        /// %=
        /// </summary>
        public static readonly TokenType AssignMOD = new TokenType("%=", SymbolType.Assign);
        /// <summary>
        /// +=
        /// </summary>
        public static readonly TokenType AssignADD = new TokenType("+=", SymbolType.Assign);
        /// <summary>
        /// -=
        /// </summary>
        public static readonly TokenType AssignSUB = new TokenType("-=", SymbolType.Assign);

        /// <summary>
        /// ++
        /// </summary>
        public static readonly TokenType INC = new TokenType("++", SymbolType.Hacked, 2);
        /// <summary>
        /// --
        /// </summary>
        public static readonly TokenType DEC = new TokenType("--", SymbolType.Hacked, 2);

        /// <summary>
        /// &=
        /// </summary>
        public static readonly TokenType AssignBitAND = new TokenType("&=", SymbolType.Assign);
        /// <summary>
        /// |=
        /// </summary>
        public static readonly TokenType AssignBitOR = new TokenType("|=", SymbolType.Assign);
        /// <summary>
        /// ^=
        /// </summary>
        public static readonly TokenType AssignBitXOR = new TokenType("^=", SymbolType.Assign);
        /// <summary>
        /// >>
        /// </summary>
        public static readonly TokenType RShift = new TokenType(">>", SymbolType.BinaryOperator);
        /// <summary>
        /// &lt;&lt;
        /// </summary>
        public static readonly TokenType LShift = new TokenType("<<", SymbolType.BinaryOperator);

        /// <summary>
        /// ::
        /// </summary>
        public static readonly TokenType Namespace = new TokenType("::", SymbolType.Other);

        // určuje sa podľa kontextu...
        /*/// <summary>
        /// [[
        /// </summary>
        public static readonly TokenType DelegateCallOpen = new TokenType("[[", SymbolType.Other);

        /// <summary>
        /// ]]
        /// </summary>
        public static readonly TokenType DelegateCallClose = new TokenType("]]", SymbolType.Other);*/

        /// <summary>
        /// /#
        /// </summary>
        public static readonly TokenType DevCodeOpen = new TokenType("/#", SymbolType.Other);

        /// <summary>
        /// #/
        /// </summary>
        public static readonly TokenType DevCodeClose = new TokenType("#/", SymbolType.Other);

        /// <summary>
        /// *
        /// </summary>
        public static readonly TokenType MUL = new TokenType("*", SymbolType.BinaryOperator, 2);
        /// <summary>
        /// /
        /// </summary>
        public static readonly TokenType DIV = new TokenType("/", SymbolType.BinaryOperator, 2);
        /// <summary>
        /// %
        /// </summary>
        public static readonly TokenType MOD = new TokenType("%", SymbolType.BinaryOperator, 2);
        /// <summary>
        /// +
        /// </summary>
        public static readonly TokenType ADD = new TokenType("+", SymbolType.Hacked, 2);
        /// <summary>
        /// -
        /// </summary>
        public static readonly TokenType SUB = new TokenType("-", SymbolType.Hacked, 2);

        /// <summary>
        /// =
        /// </summary>
        public static readonly TokenType Assign = new TokenType("=", SymbolType.Assign);

        /// <summary>
        /// &
        /// </summary>
        public static readonly TokenType BitAND = new TokenType("&", SymbolType.Hacked, 2);
        /// <summary>
        /// |
        /// </summary>
        public static readonly TokenType BitOR = new TokenType("|", SymbolType.BinaryOperator, 2);
        /// <summary>
        /// ^
        /// </summary>
        public static readonly TokenType BitXOR = new TokenType("^", SymbolType.BinaryOperator, 2);
        /// <summary>
        /// ~
        /// </summary>
        public static readonly TokenType BitNOT = new TokenType("~", SymbolType.UnaryOperator, 2);

        /// <summary>
        /// !
        /// </summary>
        public static readonly TokenType LogicNOT = new TokenType("!", SymbolType.UnaryOperator, 2);
        /// <summary>
        /// >
        /// </summary>
        public static readonly TokenType LogicGreater = new TokenType(">", SymbolType.BinaryOperator, 2);
        /// <summary>
        /// &lt;
        /// </summary>
        public static readonly TokenType LogicLess = new TokenType("<", SymbolType.BinaryOperator, 2);

        /// <summary>
        /// .
        /// </summary>
        public static readonly TokenType Ref = new TokenType(".", SymbolType.Other);

        /// <summary>
        /// ,
        /// </summary>
        public static readonly TokenType Comma = new TokenType(",", SymbolType.Other);
        /// <summary>
        /// ;
        /// </summary>
        public static readonly TokenType SemiColon = new TokenType(";", SymbolType.Other);

        /// <summary>
        /// :
        /// </summary>
        public static readonly TokenType Colon = new TokenType(":", SymbolType.Other);
        /*/// <summary>
        /// ?
        /// </summary>
        public static readonly TokenType Question = new TokenType("?", 2);
        */
        /// <summary>
        /// (
        /// </summary>
        public static readonly TokenType ParenthesesOpen = new TokenType("(", SymbolType.Other);
        /// <summary>
        /// )
        /// </summary>
        public static readonly TokenType ParenthesesClose = new TokenType(")", SymbolType.Other);
        /// <summary>
        /// {
        /// </summary>
        public static readonly TokenType ScopeOpen = new TokenType("{", SymbolType.Other);
        /// <summary>
        /// }
        /// </summary>
        public static readonly TokenType ScopeClose = new TokenType("}", SymbolType.Other);
        /// <summary>
        /// [
        /// </summary>
        public static readonly TokenType SQBracketOpen = new TokenType("[", SymbolType.Other);
        /// <summary>
        /// ]
        /// </summary>
        public static readonly TokenType SQBracketClose = new TokenType("]", SymbolType.Other);

        /// <summary>
        /// //
        /// </summary>
        public static readonly TokenType BackSlash = new TokenType("\\", SymbolType.Other);

        /// <summary>
        /// #
        /// </summary>
        public static readonly TokenType PreComp = new TokenType("#", SymbolType.Other);
        #endregion

        public bool Visible { get; private set; }
        private string name;
        public string Symbol { get { return name; } }
        public int Priority { get; private set; }
        public bool IsSymbol { get; private set; }
        public SymbolType SType { get; private set; }

        /// <summary>
        /// Create a new TokenType.
        /// </summary>
        /// <param name="visible"></param>
        private TokenType(bool visible)
        {
            Visible = visible;
            IsSymbol = false;
        }

        /// <summary>
        /// Create a new visible symbol.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        private TokenType(string name, SymbolType sType)
        {
            if (name.Length > maxLength)
                maxLength = name.Length;

            this.name = name;
            Visible = true;
            SType = sType;
            IsSymbol = true;

            SymbolList.Add(this);
        }

        /// <summary>
        /// Create a new visible symbol.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="priority"></param>
        private TokenType(string name, SymbolType sType, int priority)
            : this(name, sType)
        {
            Priority = priority;
        }

        private static Dictionary<string, TokenType> _tokenTypesList;
        public static Dictionary<string, TokenType> TokenTypesList
        {
            get 
            {
                if (_tokenTypesList == null)
                {
                    _tokenTypesList = typeof(TokenType)
                        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                        .Where(f => f.FieldType == typeof(TokenType))
                        .ToDictionary(f => f.Name, f => (TokenType)f.GetValue(null));
                }
                return _tokenTypesList;
            }
        }

        public static TokenType GetByName(string name)
        { 
            TokenType value;
            TokenTypesList.TryGetValue(name, out value);
            return value;
        }

        public string GetName()
        {
            foreach (string name in TokenTypesList.Keys)
            {
                if (TokenTypesList[name] == this)
                    return name;
            }
            return null;
        }

        public static List<IElement> ParseSymbols(string symbolsString, Scanner scanner)
        {
            int startIndex = 0;
            List<IElement> list = new List<IElement>();

            do
            { 
                list.Add(ParseOneSymbol(symbolsString, ref startIndex, scanner));
            }
            while (startIndex < symbolsString.Length);

            return list;
        }

        private static Token ParseOneSymbol(string symbolsString, ref int startIndex, Scanner scanner)
        {
            int curLength = symbolsString.Length - startIndex > maxLength ? maxLength : symbolsString.Length - startIndex;
            string curString = symbolsString.Substring(startIndex, curLength);

            TokenType type = SearchInList(curString);
            while (type == null)
            {
                curLength--;
                if (curLength <= 0)
                    throw new SyntaxException("Unknown symbol '" + curString + "'", scanner.ErrorInfo);

                curString = symbolsString.Substring(startIndex, curLength);
                type = SearchInList(curString);
            }

            int tokenStartI = (scanner.CurIndex - symbolsString.Length) + startIndex;
            Token t = new Token(tokenStartI, curLength, scanner.CurLineIndex, type, type.name);
            startIndex += curLength;
            return t;
        }

        private static TokenType SearchInList(string symbol)
        {
            TokenType t = null;
            foreach (TokenType temp in TokenType.SymbolList)
            {
                if (temp.name.Length < symbol.Length)
                    break;

                if (temp.name == symbol)
                    return temp;
            }
            return t;
        }

        public override string ToString()
        {
            return this.name;
        }
    }

    static class TokenTypeCompare
    {
        public static bool IsTT(this IElement elem, TokenType tt)
        {
            return (elem is Token && ((Token)elem).Type == tt);
        }
    }
}
