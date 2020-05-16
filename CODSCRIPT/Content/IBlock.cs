using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CODSCRIPT.Content
{
    public interface IBlock : IElement
    {
        /// <summary>
        /// Pozícia dôležitého subElementu vo vstupnom stringu(súbore).
        /// </summary>
        int? ImportantCharIndex { get; }

        /// <summary>
        /// Pozícia dôležitého subElementu vo vstupnom stringu(súbore).
        /// </summary>
        int? ImportantCharLength { get; }

        /// <summary>
        /// Pozícia dôležitého subElementu vo vstupnom stringu(súbore).
        /// </summary>
        int? ImportantLineIndex { get; }

        /// <summary>
        /// Vráti všetky potomky v bloku, niektoré z nich sú content
        /// </summary>
        /// <returns></returns>
        List<IElement> GetChildren();

        /// <summary>
        /// Vráti len obsah bloku, všetky tieto elemy sú takisto zahrnuté v children.
        /// </summary>
        /// <returns></returns>
        List<IElement> GetContent();
    }
}
