using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    /// <summary>
    /// Tokenizuje vstupný string.
    /// </summary>
    public class Scanner
    {
        public string SourceCode { get; private set; }

        public int CurIndex { get; private set; }
        public int CurLineIndex { get; private set; }

        public int Current { get; private set; }

        private ErrorInfo errorInfo;
        public ErrorInfo ErrorInfo
        {
            get
            {
                this.errorInfo.CurCharIndex = CurIndex;
                this.errorInfo.CurLineIndex = CurLineIndex;
                this.errorInfo.CurCharLength = this.errorInfo.GetCharLengthToEOL();
                return this.errorInfo;
            }
        }

        private Scanner(ScriptFile SF, string sourceCode)
        {
            SourceCode = sourceCode;
            CurIndex = 0;
            CurLineIndex = 0;
            if (sourceCode.Length == 0)
                Current = -1;
            else
                Current = sourceCode[0];
            errorInfo = new ErrorInfo(SF);
        }

        public static List<IElement> Tokenize(ScriptFile SF, string sourceCode)
        {
            List<IElement> list = new List<IElement>();
            Scanner scanner = new Scanner(SF, sourceCode);

            while (scanner.Current != -1)
            {
                list.AddRange(Token.Parse(scanner));
            }
            return list;
        }

        /// <summary>
        /// Získa char vzhľadom na aktuálnu pozíciu a offset.
        /// </summary>
        /// <param name="curOffset"></param>
        /// <returns></returns>
        public int Get(int offset)
        {
            int index = CheckIndex(CurIndex + offset);
            return index != -1 ? SourceCode[index] : -1;
        }

        /// <summary>
        /// Posunie pozíciu ďalej na najbližší token.
        /// </summary>
        /// <param name="destinationToken"></param>
        /// <returns></returns>
        public bool TryMoveTo(string destinationToken, bool afterToken)
        {
            int additionalOffset = afterToken ? destinationToken.Length : 0;
            int index = CheckIndex(SourceCode.IndexOf(destinationToken, CurIndex + 1) + additionalOffset);

            MoveTo(index);

            return (index != -1);
        }

        /// <summary>
        /// Posunie pozíciu dopredu o offset.
        /// </summary>
        /// <param name="curOffset"></param>
        /// <returns></returns>
        public bool TryMoveTo(int offset)
        {
            MoveTo(CurIndex + offset);
            int index = CheckIndex(CurIndex + offset);

            return (index != -1);
        }

        /// <summary>
        /// Posunie o pozíciu dopredu.
        /// </summary>
        /// <returns></returns>
        public void MoveNext()
        {
            MoveTo(CurIndex+1);
        }

        /// <summary>
        /// Posunie pozíciu na daný index dopredu.
        /// </summary>
        /// <param name="index"></param>
        private void MoveTo(int index)
        {
            int lastIndex = CurIndex;
            CurIndex = index;

            if (CheckIndex(index) != -1)
            {
                Current = SourceCode[index];
                int startI = index > lastIndex ? lastIndex : index;
                int endI = index > lastIndex ? index : lastIndex;

                int lines = SourceCode.Substring(startI, endI - startI).Count<char>(a => a == '\n');
                if (index > lastIndex)
                    CurLineIndex += lines;
                else
                    CurLineIndex -= lines;
            }
            else
                Current = -1;
        }

        /// <summary>
        /// Zkontroluje index, v prípade mimo rozsahu vráti -1.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private int CheckIndex(int index)
        {
            return (index >= 0 && index < SourceCode.Length) ? index : -1;
        }

        /// <summary>
        /// Vráti pozíciu znaku ukončujúceho riadok alebo dĺžku zdrojového kódu v prípade konca dokumentu.
        /// </summary>
        /// <returns></returns>
        public int FindEndOfLine()
        {
            bool exists = TryMoveTo("\n", false);
            if (!exists)
                return SourceCode.Length;

            if (Get(-1) == '\r')
                TryMoveTo(-1);

            return CurIndex;
        }
    }
}
