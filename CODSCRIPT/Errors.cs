using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CODSCRIPT.Content;
using System.Xml;

namespace CODSCRIPT
{
    class SyntaxException : ApplicationException
    {
        public ErrorInfo ErrorInfo { get; private set; }

        public SyntaxException(string message, ErrorInfo info)
            : base(message)
        {
            this.ErrorInfo = info;
        }
    }

    public class ErrorInfo
    {
        /// <summary>
        /// Zdrojový súbor.
        /// </summary>
        public ScriptFile SF { get; private set; }
        /// <summary>
        /// Pozícia v zdrojovom kóde.
        /// </summary>
        public int CurCharIndex { get; set; }
        /// <summary>
        /// Dĺžka v zdrojovom kóde.
        /// </summary>
        public int CurCharLength { get; set; }
        /// <summary>
        /// Riadok v zdrojovom kóde.
        /// </summary>
        public int CurLineIndex { get; set; }

        public NppElementInfo NppInfo { get; private set; }

        private ErrorInfo(ScriptFile sf, int charIndex, int charLength, int lineIndex)
        {
            SF = sf;

            CurCharIndex = charIndex;
            CurCharLength = charLength;
            CurLineIndex = lineIndex;

            NppInfo = new NppElementInfo(charIndex, charLength);
        }

        public ErrorInfo(ScriptFile sf)
            : this(sf, 0, 0, 0)
        {
        }

        public ErrorInfo(ErrorInfo info)
            : this(info.SF, info.CurCharIndex, info.CurCharLength, info.CurLineIndex)
        {
        }

        /// <summary>
        /// Vypočíta dĺžku chyby od začiatku po koniec riadku
        /// </summary>
        /// <returns></returns>
        public int GetCharLengthToEOL()
        {
            int endIndex = SF.SC.SourceCode.Length;

            if (CurCharIndex > 0 && CurCharIndex < SF.SC.SourceCode.Length)
            {
                endIndex = SF.SC.SourceCode.IndexOf('\n', CurCharIndex);

                if (endIndex == -1)
                    return SF.SC.SourceCode.Length - CurCharIndex;

                return endIndex - CurCharIndex;
            }
            return 0;
        }
    }

    #region Error
    public class ErrorCollection : System.Collections.IEnumerable
    {
        private List<Error> errors;

        public bool AnyErrors { get; private set; }
        public bool AnyWarnings { get; private set; }

        public ErrorCollection GetOnlyErrors()
        {
            ErrorCollection list = new ErrorCollection();
            foreach (Error e in errors)
                if (e is SyntaxError || e is SemanticError)
                    list.Add(e);

            return list;
        }

        public ErrorCollection GetOnlyWarnings()
        {
            ErrorCollection list = new ErrorCollection();
            foreach (Error e in errors)
                if (e is WarningError)
                    list.Add(e);

            return list;            
        }

        public ErrorCollection()
        { 
            errors = new List<Error>();

            AnyErrors = false;
            AnyWarnings = false;
        }

        // IList
        public void Add(Error e)
        {
            errors.Add(e);

            if (e is SyntaxError || e is SemanticError)
                AnyErrors = true;
            else if (e is WarningError)
                AnyWarnings = true;
            else
                throw new ArgumentException("e");
        }

        public void AddRange(ErrorCollection errors)
        {
            foreach (Error e in errors)
                Add(e);
        }

        public void Clear()
        {
            errors.Clear();

            AnyErrors = false;
            AnyWarnings = false;
        }

        public Error this[int i]
        {
            get { return errors[i]; }
            set { errors[i] = value; }
        }

        // ICollection
        public int Count { get { return errors.Count; } }

        // IEnumerable
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return errors.GetEnumerator();
        }

        public override string ToString()
        {
            return "Count " + Count.ToString();
        }
    }

    public abstract class Error
    {
        public string Message { get; private set; }
        public string FullMessage { get; protected set; }
        public ErrorInfo ErrorInfo { get; private set; }

        public Error(string message, ErrorInfo errorInfo)
        {
            Message = message;
            ErrorInfo = new ErrorInfo(errorInfo);
        }

        public Error(string message, string fullMessage, ErrorInfo errorInfo)
        {
            Message = message;
            FullMessage = fullMessage;
            ErrorInfo = new ErrorInfo(errorInfo);
        }

        public static Error FromXML(XmlElement elem, ScriptFile sf)
        {
            int charIndex = Int32.Parse(elem.GetAttribute("charIndex"));
            int charLength = Int32.Parse(elem.GetAttribute("charLength"));
            int lineIndex = Int32.Parse(elem.GetAttribute("lineIndex"));

            string message = string.Empty, fullMessage = string.Empty;
            
            foreach (XmlNode node in elem.ChildNodes)
            {
                if (!(node is XmlElement))
                    continue;

                if (node.Name == "message")
                    message = node.InnerText;
                else if (node.Name == "fullMessage")
                    fullMessage = node.InnerText;
                else
                    throw new XmlException("Unknown error element");
            }

            Error error = null;
            ErrorInfo errorInfo = new ErrorInfo(sf);
            errorInfo.CurCharIndex = charIndex;
            errorInfo.CurCharLength = charLength;
            errorInfo.CurLineIndex = lineIndex;

            string type = elem.GetAttribute("type");
            if (type == "syntax")
                error = new SyntaxError(message, fullMessage, errorInfo);
            else if (type == "semantic")
                error = new SemanticError(message, fullMessage, errorInfo);
            else if (type == "warning")
                error = new WarningError(message, fullMessage, errorInfo);
            else
                throw new XmlException("Unknown error type");

            return error;
        }

        public XmlElement ToXML(XmlDocument doc)
        { 
            string type = null;
            if (this is SyntaxError)
                type = "syntax";
            else if (this is SemanticError)
                type = "semantic";
            else if (this is WarningError)
                type = "warning";
            else
                throw new InvalidOperationException("unknown error type");

            XmlElement error = doc.CreateElement("error");
            error.SetAttribute("type", type);
            error.SetAttribute("charIndex", ErrorInfo.CurCharIndex.ToString());
            error.SetAttribute("charLength", ErrorInfo.CurCharLength.ToString());
            error.SetAttribute("lineIndex", ErrorInfo.CurLineIndex.ToString());

            XmlElement message = doc.CreateElement("message");
            message.InnerText = Message;
            error.AppendChild(message);
            XmlElement fullMessage = doc.CreateElement("fullMessage");
            fullMessage.InnerText = FullMessage;
            error.AppendChild(fullMessage);

            return error;
        }

        public override string ToString()
        {
            return FullMessage;
        }
    }

    public class SyntaxError : Error
    {
        public SyntaxError(string message, string fullMessage, ErrorInfo errorInfo)
            : base(message, fullMessage, errorInfo)
        {
        }

        public SyntaxError(string message, ErrorInfo errorInfo)
            : base(message, errorInfo)
        {
            FullMessage = GetFullMessage();
        }

        private string GetFullMessage()
        {
            if (ErrorInfo.SF.SC == null)
                throw new InvalidOperationException("ErrorInfo.SF.SC");

            StringBuilder sb = new StringBuilder();
            sb.Append("Bad syntax: " + Message);
            sb.Append(", file '" + ErrorInfo.SF.SFPath);
            sb.Append("', line " + (ErrorInfo.CurLineIndex + 1));
            sb.Append(", '" + ErrorInfo.SF.SC.SourceCode.Split('\n')[ErrorInfo.CurLineIndex].Trim() + "'");
            return sb.ToString();
        }
    }

    public class SemanticError : Error
    {
        public SemanticError(string message, string fullMessage, ErrorInfo errorInfo)
            : base(message, fullMessage, errorInfo)
        {
        }

        public SemanticError(string message, ErrorInfo errorInfo)
            : base(message, errorInfo)
        {
            FullMessage = GetFullMessage();
        }

        private string GetFullMessage()
        { 
            if (ErrorInfo.SF.SC == null)
                throw new InvalidOperationException("ErrorInfo.SF.SC");

            StringBuilder sb = new StringBuilder();
            sb.Append("Bad semantic: " + Message);
            sb.Append(", file '" + ErrorInfo.SF.SFPath);
            sb.Append("', line " + (ErrorInfo.CurLineIndex + 1));
            sb.Append(", '" + ErrorInfo.SF.SC.SourceCode.Split('\n')[ErrorInfo.CurLineIndex].Trim() + "'");
            return sb.ToString();
        }
    }

    public class WarningError : Error
    {
        public WarningError(string message, string fullMessage, ErrorInfo errorInfo)
            : base(message, fullMessage, errorInfo)
        {
        }

        public WarningError(string message, ErrorInfo errorInfo)
            : base(message, errorInfo)
        {
            FullMessage = GetFullMessage();
        }

        private string GetFullMessage()
        {
            if (ErrorInfo.SF.SC == null)
                throw new InvalidOperationException("ErrorInfo.SF.SC");

            StringBuilder sb = new StringBuilder();
            sb.Append("Warning: " + Message);
            sb.Append(", file '" + ErrorInfo.SF.SFPath);
            sb.Append("', line " + (ErrorInfo.CurLineIndex + 1));
            sb.Append(", '" + ErrorInfo.SF.SC.SourceCode.Split('\n')[ErrorInfo.CurLineIndex].Trim() + "'");
            return sb.ToString();
        }
    }
    #endregion
}
