using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace CODSCRIPT.Content
{
    public interface IElement
    {
        /// <summary>
        /// Pozícia elementu vo vstupnom stringu(súbore)
        /// </summary>
        int CharIndex { get; }

        /// <summary>
        /// Dĺžka elemu v zdrojovom kóde.
        /// </summary>
        int CharLength { get; }

        /// <summary>
        /// Číslo riadku vo vstupnom stringu(súbore).
        /// </summary>
        int LineIndex { get; }

        /// <summary>
        /// Viditeľnosť elemu vzhľadom na prekladač.
        /// </summary>
        bool Visible { get; }

        /// <summary>
        /// Zkontroluje sémantiku elementu.
        /// treeInfo je ContentTree, takže obsahuje aj informácie o rodičoch.
        /// </summary>
        /// <param name="scriptInfo"></param>
        void CheckSemantic(MoveInfo treeInfo, ScriptInfo scriptInfo, CheckingInfo checkingInfo);

        /// <summary>
        /// Preloží elem do výsledného kódu
        /// </summary>
        /// <returns></returns>
        string ToString();

        IElement CreateCopy();

        void Compile(MoveInfo treeInfo, ScriptInfo scriptInfo, CompilingInfo compilingInfo);
    }
}
