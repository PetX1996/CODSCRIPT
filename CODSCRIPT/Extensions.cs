using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using CODSCRIPT.Content;
using System.IO;

namespace CODSCRIPT
{
    static class TokenTypeCompare
    {
        public static bool IsTT(this IElement elem, TokenType tt)
        {
            return (elem is Token && ((Token)elem).Type == tt);
        }
    }

    public static class StringCodeCompare
    {
        public static bool EqualCode(this String str1, String str2)
        {
            if (str1.ToLower(CultureInfo.InvariantCulture) == str2.ToLower(CultureInfo.InvariantCulture))
                return true;

            return false;
        }
    }

    public static class DictionaryExtension
    {
        public static ScriptFile Find(this Dictionary<ScriptFile, ScriptFile> dic, Predicate<ScriptFile> key)
        {
            foreach (ScriptFile curKey in dic.Keys)
                if (key(curKey))
                    return dic[curKey];

            return null;
        }
    }

    public static class FileExtension
    {
        // http://stackoverflow.com/questions/876473/is-there-a-way-to-check-if-a-file-is-in-use
        public static bool IsLocked(this FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
    }
}
