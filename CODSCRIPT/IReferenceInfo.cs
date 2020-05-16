using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace CODSCRIPT
{
    public interface IReferenceInfo
    {
        int CharIndex { get; }
        int CharLength { get; }
        string CodePart { get; }

        NppElementInfo NppInfo { get; }

        /// <summary>
        /// Location of reference
        /// </summary>
        ScriptFile SF { get; }

        ScriptFile DefinitionSF { get; }

        /// <summary>
        /// Returns null if SF with definition has changed and definition does not exist anymore...
        /// </summary>
        IMemberInfo Definition { get; }

        XmlElement ToXML(XmlDocument doc);

        bool Compare(IReferenceInfo anotherRef);
    }

    public class UsingRefInfo : IReferenceInfo
    {
        public int CharIndex { get; private set; }
        public int CharLength { get; private set; }
        public string CodePart { get; private set; }

        public NppElementInfo NppInfo { get; private set; }

        public ScriptFile SF { get; private set; }

        private ScriptFile _sfDefinition;
        private string _name;

        private IMemberInfo _definition;
        public IMemberInfo Definition
        {
            get
            {
                if (_definition == null)
                {
                    if (_sfDefinition.SI == null)
                        throw new InvalidOperationException("SI");

                    _definition = _sfDefinition.SI.FindLocalUsing(_name);
                }

                return _definition;
            }
        }

        public ScriptFile DefinitionSF
        {
            get
            {
                if (Definition != null)
                    return Definition.SF;
                else if (_sfDefinition != null)
                    return _sfDefinition;
                else
                    throw new InvalidOperationException();
            }
        }

        private UsingRefInfo(ScriptFile sf, int charIndex, int charLength, string codePart)
        {
            SF = sf;

            CharIndex = charIndex;
            CharLength = charLength;
            CodePart = codePart;

            NppInfo = new NppElementInfo(charIndex, charLength);
        }

        /// <summary>
        /// Vytvorí referenciu priamo zo SC.
        /// </summary>
        /// <param name="constInfo"></param>
        /// <param name="charIndex"></param>
        /// <param name="charLength"></param>
        public UsingRefInfo(ScriptFile sf, UsingInfo usingInfo, int charIndex, int charLength, string codePart)
            : this(sf, charIndex, charLength, codePart)
        {
            this._definition = usingInfo;
        }

        /// <summary>
        /// Vytvorí referenciu len zo SI, Definition je priradený neskôr.
        /// </summary>
        /// <param name="sfPath"></param>
        /// <param name="name"></param>
        private UsingRefInfo(ScriptFile sf, ScriptFile sfDefinition, string name, int charIndex, int charLength, string codePart)
            : this(sf, charIndex, charLength, codePart)
        {
            this._sfDefinition = sfDefinition;
            this._name = name;
        }

        public static UsingRefInfo FromXML(XmlElement elem, ScriptInfo si)
        {
            string sfDefinitionPath = elem.GetAttribute("sfPath");
            string name = elem.GetAttribute("name");
            int charIndex = Int32.Parse(elem.GetAttribute("charIndex"));
            int charLength = Int32.Parse(elem.GetAttribute("charLength"));
            string codePart = elem.GetAttribute("codePart");

            ScriptFile sfDefinition = si.SF.Manager.GetSF(sfDefinitionPath);
            if (sfDefinition != null)
                return new UsingRefInfo(si.SF, sfDefinition, name, charIndex, charLength, codePart);
            else
            {
                si.SF.Manager.Trace.TraceEvent(TraceEventType.Warning, 0, "Could not find SF '{0}', reference '{1}' in '{2}' at '{3}'", sfDefinitionPath, name, si.SF.SFPath, charIndex);
                return null;
            }
        }

        public XmlElement ToXML(XmlDocument doc)
        {
            XmlElement elem = doc.CreateElement("usingRef");
            if (Definition != null)
            {
                elem.SetAttribute("sfPath", Definition.SF.SFPath);
                elem.SetAttribute("name", Definition.Name);
            }
            else if (_sfDefinition != null)
            {
                elem.SetAttribute("sfPath", _sfDefinition.SFPath);
                elem.SetAttribute("name", _name);
            }
            else
                throw new InvalidOperationException();

            elem.SetAttribute("charIndex", CharIndex.ToString());
            elem.SetAttribute("charLength", CharLength.ToString());
            elem.SetAttribute("codePart", CodePart);

            return elem;
        }

        public bool Compare(IReferenceInfo anotherRef)
        {
            if (anotherRef == null)
                return false;

            return (anotherRef is UsingRefInfo
                && this.SF == anotherRef.SF
                && this.Definition != null
                && anotherRef.Definition != null
                && this.Definition.Compare(anotherRef.Definition)
                && this.CharIndex == anotherRef.CharIndex
                && this.CharLength == anotherRef.CharLength);
        }
    }

    public class ConstRefInfo : IReferenceInfo
    {
        public int CharIndex { get; private set; }
        public int CharLength { get; private set; }
        public string CodePart { get; private set; }

        public NppElementInfo NppInfo { get; private set; }

        public ScriptFile SF { get; private set; }

        private ScriptFile _sfDefinition;
        private string _name;

        private IMemberInfo _definition;
        public IMemberInfo Definition
        {
            get
            {
                if (_definition == null)
                {
                    if (_sfDefinition.SI == null)
                        throw new InvalidOperationException("SI");

                    _definition = _sfDefinition.SI.FindLocalConst(_name);
                }

                return _definition;
            }
        }

        public ScriptFile DefinitionSF
        {
            get
            {
                if (Definition != null)
                    return Definition.SF;
                else if (_sfDefinition != null)
                    return _sfDefinition;
                else
                    throw new InvalidOperationException();
            }
        }

        private ConstRefInfo(ScriptFile sf, int charIndex, int charLength, string codePart)
        {
            SF = sf;

            CharIndex = charIndex;
            CharLength = charLength;
            CodePart = codePart;

            NppInfo = new NppElementInfo(charIndex, charLength);
        }

        /// <summary>
        /// Vytvorí referenciu priamo zo SC.
        /// </summary>
        /// <param name="constInfo"></param>
        /// <param name="charIndex"></param>
        /// <param name="charLength"></param>
        public ConstRefInfo(ScriptFile sf, ConstInfo constInfo, int charIndex, int charLength, string codePart)
            : this(sf, charIndex, charLength, codePart)
        {
            this._definition = constInfo;
        }

        /// <summary>
        /// Vytvorí referenciu len zo SI, Definition je priradený neskôr.
        /// </summary>
        /// <param name="sfPath"></param>
        /// <param name="name"></param>
        private ConstRefInfo(ScriptFile sf, ScriptFile sfDefinition, string name, int charIndex, int charLength, string codePart)
            : this(sf, charIndex, charLength, codePart)
        {
            this._sfDefinition = sfDefinition;
            this._name = name;
        }

        public static ConstRefInfo FromXML(XmlElement elem, ScriptInfo si)
        {
            string sfDefinitionPath = elem.GetAttribute("sfPath");
            string name = elem.GetAttribute("name");
            int charIndex = Int32.Parse(elem.GetAttribute("charIndex"));
            int charLength = Int32.Parse(elem.GetAttribute("charLength"));
            string codePart = elem.GetAttribute("codePart");

            ScriptFile sfDefinition = si.SF.Manager.GetSF(sfDefinitionPath);
            if (sfDefinition != null)
                return new ConstRefInfo(si.SF, sfDefinition, name, charIndex, charLength, codePart);
            else
            {
                si.SF.Manager.Trace.TraceEvent(TraceEventType.Warning, 0, "Could not find SF '{0}', reference '{1}' in '{2}' at '{3}'", sfDefinitionPath, name, si.SF.SFPath, charIndex);
                return null;
            }
        }

        public XmlElement ToXML(XmlDocument doc)
        {
            XmlElement elem = doc.CreateElement("constRef");
            if (Definition != null)
            {
                elem.SetAttribute("sfPath", Definition.SF.SFPath);
                elem.SetAttribute("name", Definition.Name);
            }
            else if (_sfDefinition != null)
            {
                elem.SetAttribute("sfPath", _sfDefinition.SFPath);
                elem.SetAttribute("name", _name);
            }
            else
                throw new InvalidOperationException();

            elem.SetAttribute("charIndex", CharIndex.ToString());
            elem.SetAttribute("charLength", CharLength.ToString());
            elem.SetAttribute("codePart", CodePart);
            return elem;
        }

        public bool Compare(IReferenceInfo anotherRef)
        {
            if (anotherRef == null)
                return false;

            return (anotherRef is ConstRefInfo
                && this.SF == anotherRef.SF
                && this.Definition != null
                && anotherRef.Definition != null
                && this.Definition.Compare(anotherRef.Definition)
                && this.CharIndex == anotherRef.CharIndex
                && this.CharLength == anotherRef.CharLength);
        }
    }

    public class FuncRefInfo : IReferenceInfo
    {
        public int CharIndex { get; private set; }
        public int CharLength { get; private set; }
        public string CodePart { get; private set; }

        public NppElementInfo NppInfo { get; private set; }

        public ScriptFile SF { get; private set; }

        private ScriptFile _sfDefinition;
        private string _name;

        private IMemberInfo _definition;
        public IMemberInfo Definition
        {
            get
            {
                if (_definition == null)
                {
                    if (_sfDefinition.SI == null)
                        throw new InvalidOperationException("SI");

                    _definition = _sfDefinition.SI.FindLocalFunc(_name);
                }

                return _definition;
            }
        }

        public ScriptFile DefinitionSF
        {
            get
            {
                if (Definition != null)
                    return Definition.SF;
                else if (_sfDefinition != null)
                    return _sfDefinition;
                else
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Zoznam argumentov funkcie, ak je null, funkcia sa nevolá.
        /// </summary>
        public List<FuncRefArgInfo> Arguments { get; private set; }

        private FuncRefInfo(ScriptFile sf, int charIndex, int charLength, string codePart, bool isCall)
        {
            SF = sf;

            CharIndex = charIndex;
            CharLength = charLength;
            CodePart = codePart;

            if (isCall)
                Arguments = new List<FuncRefArgInfo>();

            NppInfo = new NppElementInfo(charIndex, charLength);
        }

        /// <summary>
        /// Vytvorí referenciu priamo zo SC.
        /// </summary>
        /// <param name="funcInfo"></param>
        /// <param name="charIndex"></param>
        /// <param name="charLength"></param>
        /// <param name="isCall">Volá sa funkcia alebo sa len získava jej adresa?</param>
        public FuncRefInfo(ScriptFile sf, FuncInfo funcInfo, int charIndex, int charLength, string codePart, bool isCall)
            : this(sf, charIndex, charLength, codePart, isCall)
        {
            this._definition = funcInfo;
        }

        /// <summary>
        /// Vytvorí referenciu len zo SI, Definition je priradený neskôr.
        /// </summary>
        /// <param name="sfPath"></param>
        /// <param name="name"></param>
        private FuncRefInfo(ScriptFile sf, ScriptFile sfDefinition, string name, int charIndex, int charLength, string codePart, bool isCall)
            : this(sf, charIndex, charLength, codePart, isCall)
        {
            this._sfDefinition = sfDefinition;
            this._name = name;
        }

        public void AddArgument(int charIndex, int charLength)
        {
            if (Arguments == null)
                throw new InvalidOperationException("isCall");

            Arguments.Add(new FuncRefArgInfo(SF, charIndex, charLength));
        }

        public void AddArgument(FuncRefArgInfo arg)
        {
            if (Arguments == null)
                throw new InvalidOperationException("isCall");

            Arguments.Add(arg);
        }

        public static FuncRefInfo FromXML(XmlElement elem, ScriptInfo si)
        {
            string sfDefinitionPath = elem.GetAttribute("sfPath");
            string name = elem.GetAttribute("name");
            int charIndex = Int32.Parse(elem.GetAttribute("charIndex"));
            int charLength = Int32.Parse(elem.GetAttribute("charLength"));
            string codePart = elem.GetAttribute("codePart");
            bool isCall = Boolean.Parse(elem.GetAttribute("isCall"));

            ScriptFile sfDefinition = si.SF.Manager.GetSF(sfDefinitionPath);
            if (sfDefinition == null)
            {
                si.SF.Manager.Trace.TraceEvent(TraceEventType.Warning, 0, "Could not find SF '{0}', reference '{1}' in '{2}' at '{3}'", sfDefinitionPath, name, si.SF.SFPath, charIndex);
                return null;
            }

            FuncRefInfo funcRef = new FuncRefInfo(si.SF, sfDefinition, name, charIndex, charLength, codePart, isCall);
            if (isCall)
            {
                foreach (XmlNode node in elem.ChildNodes)
                    if (node is XmlElement && node.Name == "funcRefArg")
                        funcRef.AddArgument(FuncRefArgInfo.FromXML((XmlElement)node, si));
            }

            return funcRef;
        }

        public XmlElement ToXML(XmlDocument doc)
        {
            XmlElement elem = doc.CreateElement("funcRef");
            if (Definition != null)
            {
                elem.SetAttribute("sfPath", Definition.SF.SFPath);
                elem.SetAttribute("name", Definition.Name);
            }
            else if (_sfDefinition != null)
            {
                elem.SetAttribute("sfPath", _sfDefinition.SFPath);
                elem.SetAttribute("name", _name);
            }
            else
                throw new InvalidOperationException();

            elem.SetAttribute("charIndex", CharIndex.ToString());
            elem.SetAttribute("charLength", CharLength.ToString());
            elem.SetAttribute("codePart", CodePart);
            elem.SetAttribute("isCall", (Arguments != null).ToString());

            if (Arguments != null)
                foreach (FuncRefArgInfo funcArgRef in Arguments)
                    elem.AppendChild(funcArgRef.ToXML(doc));

            return elem;
        }

        public bool Compare(IReferenceInfo anotherRef)
        {
            if (anotherRef == null)
                return false;

            return (anotherRef is FuncRefInfo
                && this.SF == anotherRef.SF
                && this.Definition != null
                && anotherRef.Definition != null
                && this.Definition.Compare(anotherRef.Definition)
                && this.CharIndex == anotherRef.CharIndex
                && this.CharLength == anotherRef.CharLength);
        }
    }

    public class FuncRefArgInfo
    {
        public int CharIndex { get; private set; }
        public int CharLength { get; private set; }

        public NppElementInfo NppInfo { get; private set; }

        public ScriptFile SF { get; private set; }

        public FuncRefArgInfo(ScriptFile sf, int charIndex, int charLength)
        {
            SF = sf;

            CharIndex = charIndex;
            CharLength = charLength;

            NppInfo = new NppElementInfo(charIndex, charLength);
        }

        public static FuncRefArgInfo FromXML(XmlElement elem, ScriptInfo si)
        {
            int charIndex = Int32.Parse(elem.GetAttribute("charIndex"));
            int charLength = Int32.Parse(elem.GetAttribute("charLength"));
            return new FuncRefArgInfo(si.SF, charIndex, charLength);
        }

        public XmlElement ToXML(XmlDocument doc)
        {
            XmlElement argElem = doc.CreateElement("funcRefArg");
            argElem.SetAttribute("charIndex", CharIndex.ToString());
            argElem.SetAttribute("charLength", CharLength.ToString());
            return argElem;
        }
    }
}
