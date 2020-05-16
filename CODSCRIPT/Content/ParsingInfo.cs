using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    /// <summary>
    /// Pomocná trieda využívaná pri parsovaní do parse tree
    /// </summary>
    public class ParsingInfo
    {
        // references
        public ScriptFile SF { get; private set; }

        // autocompletion and local var check...
        public FuncDef CurrentFunc { get; set; }

        public List<PreProcessorInclude> IncludeDefList { get; private set; }
        public List<FuncDef> FuncDefList { get; private set; }
        public List<ConstDef> ConstDefList { get; private set; }
        public List<UsingDef> UsingDefList { get; private set; }
        public List<OverwriteConstDef> OverwriteConstDefList { get; private set; }

        // func with out params...
        public FuncCall OutParamFuncCall { get; set; }
        public FuncInfo OutParamFuncInfo { get; set; }
        public int OutParamFuncArgIndex { get; set; }

        // func call arguments
        public object CurrentCall { get; set; }
        public int? CurrentCallArgIndex { get; set; }

        public ParsingInfo(ScriptFile sf)
        {
            SF = sf;

            IncludeDefList = new List<PreProcessorInclude>();
            FuncDefList = new List<FuncDef>();
            ConstDefList = new List<ConstDef>();
            UsingDefList = new List<UsingDef>();
            OverwriteConstDefList = new List<OverwriteConstDef>();
        }
    }

    /// <summary>
    /// Pomocná trieda využívaná pri kontrole sémantiky
    /// </summary>
    public class CheckingInfo
    {
        public ScriptCode SC { get; private set; }

        public FuncDef CurrentFunc { get; set; }

        public CheckingInfo(ScriptCode sc)
        {
            SC = sc;
        }
    }

    /// <summary>
    /// Pomocná trieda využívaná pri kompilovaní
    /// </summary>
    public class CompilingInfo
    {
        public int IteratorsCount { get; set; }

        public CompilingInfo()
        {
        }
    }
}
