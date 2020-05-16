using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT
{
    public class IncludeInfo
    {
        public string SFPath { get; private set; }
        public ScriptFile SF { get; private set; }

        public IncludeInfo(ScriptFile SF)
        {
            this.SF = SF;
            this.SFPath = SF.SFPath;

            //this.SF.ReadSI();
        }

        public override string ToString()
        {
            return this.SFPath;
        }
    }

    public interface IMemberInfo
    {
        string Name { get; }
        ScriptFile SF { get; }

        NppElementInfo NppInfo { get; }

        List<IReferenceInfo> FindAllReferences();

        bool Compare(IMemberInfo anotherMember);
    }

    public class UsingInfo : IMemberInfo
    {
        public ScriptFile SF { get; private set; }

        public MemberAccess Access { get; private set; }
        public string Name { get; private set; }
        public string SFPath { get; private set; }

        public NppElementInfo NppInfo { get; private set; }

        public UsingInfo(ScriptFile sf, string name, string sfPath, MemberAccess access, CODSCRIPT.Content.UsingDef def)
        {
            SF = sf;
            Name = name;
            SFPath = sfPath;
            Access = access;

            if (def != null)
                NppInfo = new NppElementInfo(def.CharIndex, def.CharLength, def.ImportantCharIndex, def.ImportantCharLength);
        }

        public List<IReferenceInfo> FindAllReferences()
        {
            List<IReferenceInfo> references = new List<IReferenceInfo>();
            foreach (ScriptFile sf in SF.Manager.GetAllSFs())
            {
                if (sf.SI == null)
                    continue;

                foreach (IReferenceInfo curRef in sf.SI.References)
                    if (curRef.Definition != null && curRef.Definition.Compare(this))
                        references.Add(curRef);
            }
            return references;
        }

        public override string ToString()
        {
            return SF.SFPath + "::" + Name;
        }

        public bool Compare(IMemberInfo anotherMember)
        {
            if (anotherMember == null)
                return false;

            return (anotherMember is UsingInfo
                && this.SF == anotherMember.SF
                && this.Name == anotherMember.Name
                && this.SFPath == ((UsingInfo)anotherMember).SFPath);
        }
    }

    public class ConstInfo : IMemberInfo
    {
        public ScriptFile SF { get; private set; }

        public string Name { get; private set; }
        public string Summary { get; private set; }
        public MemberAccess Access { get; private set; }

        private CODSCRIPT.Content.Expression _value;
        public CODSCRIPT.Content.Expression Value
        {
            get { return _value; }
            set { _value = value; }
        }

        public string OriginalValue { get; private set; }

        public CODSCRIPT.Content.ConstDef ConstDef { get; set; }

        public bool Sealed { get; private set; }

        public NppElementInfo NppInfo { get; private set; }

        public ConstInfo(ScriptFile sf, 
            string name, 
            string originalValue, 
            CODSCRIPT.Content.Expression value, 
            MemberAccess access, 
            string summary, 
            bool isSealed, 
            CODSCRIPT.Content.ConstDef def)
        {
            SF = sf;

            Name = name;
            Summary = summary;
            OriginalValue = originalValue;
            Value = value;
            Access = access;
            Sealed = isSealed;

            ConstDef = def;

            if (def != null)
                NppInfo = new NppElementInfo(def.CharIndex, def.CharLength, def.ImportantCharIndex, def.ImportantCharLength);
        }

        public ConstInfo CreateCopy(ScriptFile sf)
        {
            return new ConstInfo(sf, Name, OriginalValue, (CODSCRIPT.Content.Expression)Value.CreateCopy(), Access, Summary, Sealed, ConstDef);
        }

        public List<IReferenceInfo> FindAllReferences()
        {
            List<IReferenceInfo> references = new List<IReferenceInfo>();
            foreach (ScriptFile sf in SF.Manager.GetAllSFs())
            {
                if (sf.SI == null)
                    continue;

                foreach (IReferenceInfo curRef in sf.SI.References)
                    if (curRef.Definition != null && curRef.Definition.Compare(this))
                        references.Add(curRef);
            }
            return references;
        }

        public override string ToString()
        {
            return SF.SFPath + "::" + Name;
        }

        public bool Compare(IMemberInfo anotherMember)
        {
            if (anotherMember == null)
                return false;

            return (anotherMember is ConstInfo
                && this.SF == anotherMember.SF
                && this.Name == anotherMember.Name);
        }
    }

    public class FuncInfo : IMemberInfo
    {
        public ScriptFile SF { get; private set; }

        public string Name { get; private set; }
        public string Summary { get; private set; }
        public MemberAccess Access { get; private set; }
        public string Returns { get; private set; }
        public List<FuncParamInfo> Parameters { get; private set; }
        public string Self { get; private set; }
        public string Example { get; private set; }

        public bool HasOutParams { get; private set; }
        public int? OptParamStartIndex { get; private set; }

        public List<LocalVarInfo> LocalVars { get; private set; }

        public NppElementInfo NppInfo { get; private set; }

        public FuncInfo(ScriptFile sf, string name, MemberAccess access, string summary, string returns, string self, string example, CODSCRIPT.Content.FuncDef def)
        {
            SF = sf;

            Name = name;
            Summary = summary;
            Access = access;
            Returns = returns;
            Parameters = new List<FuncParamInfo>();
            Self = self;
            Example = example;

            HasOutParams = false;
            OptParamStartIndex = null;

            if (def != null)
            {
                LocalVars = def.LocalVars;
                NppInfo = new NppElementInfo(def.CharIndex, def.CharLength, def.ImportantCharIndex, def.ImportantCharLength);
            }
        }

        public FuncInfo CreateCopy(ScriptFile sf)
        {
            FuncInfo f = new FuncInfo(sf, Name, Access, Summary, Returns, Self, Example, null);
            f.HasOutParams = HasOutParams;
            f.OptParamStartIndex = OptParamStartIndex;
            f.Parameters = Parameters;
            return f;
        }

        public void AddParam(FuncParamInfo param)
        {
            if (param.IsOut)
                HasOutParams = true;

            if (param.IsOpt && OptParamStartIndex == null)
                OptParamStartIndex = Parameters.Count;

            Parameters.Add(param);
        }

        public string GetHead()
        {
            return GetHead(null);
        }

        public string GetHead(int? paramsCount)
        {
            int? hltStart, hltEnd;
            return GetHead(paramsCount, null, out hltStart, out hltEnd);
        }

        public string GetHead(int? paramsCount, int? hltParam, out int? hltStart, out int? hltEnd)
        {
            hltStart = null;
            hltEnd = null;

            if (paramsCount == null || paramsCount > Parameters.Count)
                paramsCount = Parameters.Count;

            StringBuilder sb = new StringBuilder(Name + "(");
            for (int i = 0; i < paramsCount; i++)
            {
                if (hltParam != null && i == hltParam)
                    hltStart = sb.Length;

                if (Parameters[i].IsOpt)
                    sb.Append("[" + Parameters[i].Name + "]");
                else
                    sb.Append(Parameters[i].Name);

                if (hltParam != null && i == hltParam)
                    hltEnd = sb.Length;

                if (i != paramsCount - 1)
                    sb.Append(", ");
            }

            if (paramsCount < Parameters.Count && paramsCount > 0)
                sb.Append(", ...");

            sb.Append(")");

            return sb.ToString();
        }

        public List<IReferenceInfo> FindAllReferences()
        {
            List<IReferenceInfo> references = new List<IReferenceInfo>();
            foreach (ScriptFile sf in SF.Manager.GetAllSFs())
            {
                if (sf.SI == null)
                    continue;

                foreach (IReferenceInfo curRef in sf.SI.References)
                    if (curRef.Definition != null && curRef.Definition.Compare(this))
                        references.Add(curRef);
            }
            return references;
        }

        public override string ToString()
        {
            return SF.SFPath + "::" + Name;
        }

        public bool Compare(IMemberInfo anotherMember)
        {
            if (anotherMember == null)
                return false;

            return (anotherMember is FuncInfo
                && this.SF == anotherMember.SF
                && this.Name == anotherMember.Name);
        }
    }

    public class FuncParamInfo
    {
        public string Name { get; private set; }
        public string Summary { get; private set; }
        public bool IsOut { get; private set; }
        public bool IsOpt { get; set; }

        public FuncParamInfo(string name, string summary, bool isOut, bool isOpt)
        {
            Name = name;
            Summary = summary;
            IsOut = isOut;
            IsOpt = isOpt;
        }

        public string GetSummary()
        {
            if (String.IsNullOrEmpty(Summary))
                return null;
            else
                return Name + ": " + Summary;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class LocalVarInfo : IMemberInfo
    {
        public ScriptFile SF { get; private set; }
        public string Name { get; private set; }
        public int StartIndex { get; private set; }

        public CODSCRIPT.Content.Assign AssignDef { get; private set; }
        public CODSCRIPT.Content.VarName VarNameDef { get; private set; } // in the AssignDef

        public int RefCount { get; set; }

        public NppElementInfo NppInfo { get; private set; }

        public LocalVarInfo(ScriptFile sf, string name, int charIndex, int charLength, CODSCRIPT.Content.Assign assignDef, CODSCRIPT.Content.VarName varNameDef)
        {
            SF = sf;
            Name = name;
            StartIndex = charIndex;

            RefCount = 0;
            AssignDef = assignDef;
            VarNameDef = varNameDef;

            NppInfo = new NppElementInfo(charIndex, charLength);
        }

        public List<IReferenceInfo> FindAllReferences()
        {
            throw new NotImplementedException("Why?!");
        }

        public bool Compare(IMemberInfo anotherMember)
        {
            if (anotherMember == null)
                return false;

            return (this.SF == anotherMember.SF
                && this.Name == anotherMember.Name);
        }
    }
}
