using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CODSCRIPT;
using System.IO;
using System.Diagnostics;

namespace ScriptPreCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Compiler c = new Compiler(args);
                c.Compile();
            }
            catch (Exception e)
            {
                Console.WriteLine("********************************");
                Console.WriteLine("************ ERROR *************");
                Console.WriteLine("********************************");

                string error = string.Empty;
                error += e.Message + Environment.NewLine + e.StackTrace;
                if (e.InnerException != null)
                    error += Environment.NewLine + e.InnerException + Environment.NewLine + e.InnerException.StackTrace;

                Console.WriteLine(e.GetType().ToString());
                Console.WriteLine(error);
            }

            /*DateTime start = DateTime.Now;
            DateTime last = DateTime.Now;

            ScriptManager manager = ScriptManager.Create(@"D:\CoD4 Tvorba Map\Call of Duty 4 - Modern Warfare\bin\CODSCRIPT", @"Mods\deathrun\compiler.xml");

            manager.FindAssemblySFs();
            Console.WriteLine("SF: " + (DateTime.Now - last).TotalMilliseconds);
            last = DateTime.Now;

            manager.ReadAssemblySFs(ReadingState.ScriptInfo);
            Console.WriteLine("SI: " + (DateTime.Now - last).TotalMilliseconds);
            last = DateTime.Now;
            */
            /*
            manager.ReadAssemblySFs(ReadingState.ScriptCode);
            Console.WriteLine("SC: " + (DateTime.Now - last).TotalMilliseconds);
            last = DateTime.Now;

            manager.ReadAssemblySFs(ReadingState.CheckCode);
            Console.WriteLine("Check: " + (DateTime.Now - last).TotalMilliseconds);
            last = DateTime.Now;
            */
            /*
            manager.CompileAssemblySFs(RawType.FSGame, false);
            Console.WriteLine("Compile: " + (DateTime.Now - last).TotalMilliseconds);
            last = DateTime.Now;
            */
            //manager.GetSFFromFullPath(@"D:\CoD4 Tvorba Map\Call of Duty 4 - Mod ESCAPE\Mods\escape\scripts\_bots", ".gsc");
            
            
            //ScriptFile sf = manager.GetSF("scripts\\include\\_string");
            
           /* sf.ReadSC();
            Console.WriteLine("Read: " + (DateTime.Now - last).TotalMilliseconds);
            last = DateTime.Now;

            sf.CheckSC();
            Console.WriteLine("Check: " + (DateTime.Now - last).TotalMilliseconds);
            last = DateTime.Now;

            sf.PrepareCompileSC();
            sf.CompileMembersSC();
            sf.CompileCodeSC();
            sf.CompileOutputSC();
            Console.WriteLine("Compile: " + (DateTime.Now - last).TotalMilliseconds);
            last = DateTime.Now;
            */

            /*sf.SC.GetMemberForXMLDoc(6820);

            string memStr = "";
            List<IMemberInfo> members = sf.GetAvailableMembers(1475);
            foreach (IMemberInfo mem in members)
            {
                if (mem is LocalVarInfo)
                    memStr += "Member: " + mem.ToString() + "\n\n";
            }
            Console.Write(memStr);



            string str = "";
            foreach (Error e in manager.Errors)
            {
                //HighLightError(e);
                str += "Error: " + e.FullMessage + " s: " + e.Info.CurCharIndex + " l: " + e.Info.CurCharLength + "\n\n";
            }
            Console.Write(str);

            Console.WriteLine("Errors\n==========================\n");
            foreach (Error e in manager.Errors)
            {
                Console.WriteLine(e.FullMessage);
            }*/
            /*
            Console.WriteLine("=====================================");
            Console.WriteLine("Compile time: " + (DateTime.Now - start).TotalMilliseconds);
            Console.WriteLine("=====================================");
            Console.ReadKey();
             */
        }
    }
}
