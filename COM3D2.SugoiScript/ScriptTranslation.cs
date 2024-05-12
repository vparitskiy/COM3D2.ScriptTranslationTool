using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COM3D2.ScriptTranslationTool
{
    internal static class ScriptTranslation
    {

        internal static bool pause = false;

        internal readonly static Dictionary<string, string> tldScripts = new Dictionary<string, string>();


        internal static void Process(ref double scriptCount, ref int lineCount)
        {
            // Create folder to sort script files in
            if (Program.exportToi18nEx)
            {
                ScriptManagement.CreateSortedFolders();
            }

            Task.Run(() => Tools.ListerKeyBoardEvent());

            IEnumerable<string> scriptFiles = Directory.EnumerateFiles(Program.japaneseScriptFolder, "*.txt*", SearchOption.AllDirectories);
            double scriptTotal = scriptFiles.Count();

            foreach (string file in scriptFiles)
            {
                if (pause)
                {
                    Tools.WriteLine("\n===================== Pause =====================", ConsoleColor.Red);
                    Tools.WriteLine("Press any Key to resume.", ConsoleColor.Red);
                    Console.ReadKey(true);
                    pause = false;
                }


                Dictionary<string, string> dict = new Dictionary<string, string>();
                string filename = Path.GetFileName(file);
                bool hasError = false;

                // Load already translated script file with the same name if it exists
                if (tldScripts.ContainsKey(filename))
                {
                    Console.WriteLine($"{filename} already exists, merging scripts");
                    dict = Cache.LoadFromFile(tldScripts[filename]);
                }

                Tools.WriteLine($"\n-------- {filename} --------", ConsoleColor.Yellow);


                string[] lines = File.ReadAllLines(file);
                foreach (string l in lines)
                {
                    ScriptLine line = new ScriptLine(filename, l);
                    lineCount++;

                    // skip if the line has already been translated for this script
                    if (dict.ContainsKey(line.Japanese))
                    {
                        continue;
                    }

                    // skip if line is empty
                    if (string.IsNullOrEmpty(line.JapanesePrep)) { continue; }

                    Console.Write(line.Japanese);
                    Tools.Write(" => ", ConsoleColor.Yellow);

                    // recover translation from caches
                    Cache.Get(line);

                    // if no translation from cache, ask SugoiTranslator and add to cache, otherwise leave it blanck
                    if (string.IsNullOrEmpty(line.English))
                    {
                        if (Program.isSugoiRunning)
                        {
                            Translate.ToEnglish(line);

                            line.CleanPost();

                            if (line.HasRepeat || line.HasError)
                            {
                                hasError = true;
                                Cache.AddToError(line);
                                Tools.WriteLine($"This line returned a faulty translation and was placed in {Program.errorFile}", ConsoleColor.Red);
                                continue;
                            }

                            Cache.AddTo(line);
                        }
                        else
                        {
                            Tools.WriteLine($"This line wasn't found in any cache and can't be translated since sugoi isn't running", ConsoleColor.Red);
                            continue;
                        }
                    }

                    Tools.WriteLine(line.English, line.Color);

                    // add to i18nEx script folder
                    if (Program.exportToi18nEx)
                    {
                        ScriptManagement.AddTo(line);
                    }
                }

                scriptCount++;
                if (Program.moveFinishedRawScript)
                {
                    ScriptManagement.MoveFinished(file, hasError);
                }

                Console.Title = $"Processing ({scriptCount} out of {scriptTotal} scripts)";
            }

            // Adding back subtitles
            if (Directory.Exists("Caches/Subtitles") && Program.exportToi18nEx)
            {
                IEnumerable<string> subtitlesFiles = Directory.EnumerateFiles("Caches/Subtitles");

                foreach (string subFile in subtitlesFiles)
                {
                    string subFileName = Path.GetFileName(subFile);
                    string[] scriptFile = Directory.GetFiles(Program.i18nExScriptFolder, subFileName, SearchOption.AllDirectories);
                    if (scriptFile.Length > 0)
                    {
                        Tools.WriteLine($"Adding subtitles to {subFileName}.", ConsoleColor.Green);
                        string[] strings = File.ReadAllLines(subFile);
                        File.AppendAllLines(scriptFile[0], strings);
                    }
                    else
                    {
                        Tools.WriteLine($"Creating new subtitle script {subFileName}", ConsoleColor.Green);
                        string subPath = Path.Combine(Program.i18nExScriptFolder, "[Subtitles]");
                        Tools.MakeFolder(subPath);
                        File.Copy(subFile, Path.Combine(subPath, subFileName));
                    }
                }
            }
            else
            {
                Tools.WriteLine($"No subtitles cache found, skipping", ConsoleColor.White);
            }
        }
    }
}
