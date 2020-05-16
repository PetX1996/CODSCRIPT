using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    public class PreProcessorInclude : SimpleTree
    {
        public Path Path { get; private set; }

        private PreProcessorInclude()
            : base(null)
        { 
        }

        public override IElement CreateCopy()
        {
            PreProcessorInclude e = new PreProcessorInclude();
            e.AddChildren(this.CopyChildren());

            e.Path = (Path)e.children[this.children.IndexOf(Path)];

            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.PreComp))
            {
                MoveInfo moveInfo = new MoveInfo(parentInfo);
                IElement next = moveInfo.Move(SearchDirection.LeftToRight);
                if (next != null && next.IsTT(TokenType.Word) && next.ToString() == "include")
                {
                    Parse(parentInfo, parsingInfo, scriptInfo);
                    return true;
                }
            }
            return false;
        }

        private static void Parse(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            PreProcessorInclude include = new PreProcessorInclude();
            int startIndex = parentInfo.CurrentIndex;

            MoveInfo moveInfo = new MoveInfo(parentInfo);
            moveInfo.Move(SearchDirection.LeftToRight); // "include"
            moveInfo.Move(SearchDirection.LeftToRight); // after "include"
            moveInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);

            Path path = Path.Parse(moveInfo, parsingInfo, scriptInfo);
            if(path == null)
                throw new SyntaxException("Bad path", parentInfo.GetErrorInfo());

            include.Path = path;

            moveInfo.Move(SearchDirection.LeftToRight);
            IElement terminal = moveInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Visible);
            if (terminal == null || !terminal.IsTT(TokenType.SemiColon))
                throw new SyntaxException("Could not find ;", parentInfo.GetErrorInfo());


            int length = (moveInfo.CurrentIndex + 1) - startIndex;
            List<IElement> children = parentInfo.CurrentElements.GetRange(startIndex, length);

            include.AddChildren(children);
            parentInfo.Replace(length, include);

            // include file
            string SFPath = path.ToString();
            if (scriptInfo.Includes.Find(a => a.SFPath.EqualCode(SFPath)) != null)
                scriptInfo.SF.Errors.Add(
                    new SemanticError("File '" + SFPath + "' already included",
                        parentInfo.GetErrorInfo(include)));

            ScriptFile includeFile = scriptInfo.SF.Manager.GetSF(SFPath);
            if (includeFile != null)
            {
                IncludeInfo includeInfo = new IncludeInfo(includeFile);
                scriptInfo.AddInclude(includeInfo);

                parsingInfo.IncludeDefList.Add(include);
            }
            else
                scriptInfo.SF.Errors.Add(
                    new SemanticError("Could not include file '" + SFPath + "'",
                        parentInfo.GetErrorInfo(include)));
        }
    }

    class PreProcessorRegion : SimpleTree
    {
        private PreProcessorRegion(List<IElement> elems)
            : base(elems)
        { 
        }

        public override IElement CreateCopy()
        {
            PreProcessorRegion e = new PreProcessorRegion(this.CopyChildren());
            return e;
        }

        public static bool Check(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            if (parentInfo.Current.IsTT(TokenType.PreComp))
            {
                MoveInfo moveInfo = new MoveInfo(parentInfo);
                IElement next = moveInfo.Move(SearchDirection.LeftToRight);
                if (next != null && next.IsTT(TokenType.Word))
                {
                    if (next.ToString() == "region")
                    {
                        ParseRegion(parentInfo, parsingInfo, scriptInfo);
                        return true;
                    }
                    else if (next.ToString() == "endregion")
                    {
                        ParseEndRegion(parentInfo, parsingInfo, scriptInfo);
                        return true;
                    }
                }
            }
            return false;
        }

        public static void ParseRegion(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            MoveInfo moveInfo = new MoveInfo(parentInfo);
            List<IElement> content = new List<IElement>();
            content.Add(moveInfo.Current);
            content.Add(moveInfo.Move(SearchDirection.LeftToRight));

            moveInfo.Move(SearchDirection.LeftToRight);
            int nameStart = moveInfo.CurrentIndex;

            IElement end = moveInfo.Find(SearchDirection.LeftToRight, SearchVisibility.Unvisible, IsRegionEnd);

            if (end == null)
                throw new SyntaxException("Bad region syntax", parentInfo.GetErrorInfo());

            content.AddRange(moveInfo.CurrentElements.GetRange(nameStart, moveInfo.CurrentIndex - nameStart));

            IBlock region = new PreProcessorRegion(content);
            parentInfo.Replace(moveInfo.CurrentIndex - parentInfo.CurrentIndex, region);
        }

        private static bool IsRegionEnd(IElement elem)
        {
            return (elem.IsTT(TokenType.WhiteSpace) && elem.ToString() == "\n");
        }

        public static void ParseEndRegion(MoveInfo parentInfo, ParsingInfo parsingInfo, ScriptInfo scriptInfo)
        {
            MoveInfo moveInfo = new MoveInfo(parentInfo);
            List<IElement> content = new List<IElement>(2);
            content.Add(moveInfo.Current);
            content.Add(moveInfo.Move(SearchDirection.LeftToRight));

            IBlock region = new PreProcessorRegion(content);
            parentInfo.Replace(2, region);
        }
    }
}
