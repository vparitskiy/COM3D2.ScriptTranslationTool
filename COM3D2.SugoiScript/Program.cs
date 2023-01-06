using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace COM3D2.ScriptTranslationTool
{
    internal class Program
    {
        internal static Dictionary<string, string> machine = new Dictionary<string, string>();
        internal static Dictionary<string, string> official = new Dictionary<string, string>();
        internal static Dictionary<string, string> manual = new Dictionary<string,string> ();
        internal static Dictionary<string, List<string>> subtitles = new Dictionary<string, List<string>>();

        internal static Dictionary<string, string> tldScripts = new Dictionary<string, string>();

        internal static string machineCacheFile = @"Caches\MachineTranslationCache.txt";
        internal static string officialCacheFile = @"Caches\OfficialTranslationCache.txt";
        internal static string officialSubtitlesCache = @"Caches\officialSubtitlesCache.txt";
        internal static string manualCacheFile = @"Caches\ManualTranslationCache.txt";
        internal static string errorFile = "Errors.txt";

        internal static char splitChar = '\t';

        internal static string japaneseScriptFolder = @"Scripts\Japanese";
        internal static string englishScriptFolder = @"Scripts\English";
        internal static string translatedScriptFolder = @"Scripts\AlreadyTranslated";
        internal static string i18nExScriptFolder = @"Scripts\i18nEx\English\Script";
        

        internal static bool isSugoiRunning = false;
        internal static bool includeOfficial = false;
        internal static bool exportToi18nEx = false;
        internal static bool moveFinishedRawScript = true;

        internal static bool pause = false;

        static void Main()
        {
            Tools.GetConfig();

            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Console.WriteLine("==================== Initialization ====================\n");
            Console.Title = "Initialization";

            Tools.MakeFolder(englishScriptFolder);
            Tools.MakeFolder(japaneseScriptFolder);
            Tools.MakeFolder("Caches");
            if (moveFinishedRawScript) Tools.MakeFolder(translatedScriptFolder);

            if (Directory.EnumerateFiles(japaneseScriptFolder, "*.txt", SearchOption.AllDirectories).Count() == 0)
            {
                Tools.WriteLine($"No Japanese scripts found, please put your extracted japanese scripts in {japaneseScriptFolder}", ConsoleColor.Red);
            }


            // building the official translation cache.
            if (Directory.Exists(englishScriptFolder) && !File.Exists(officialCacheFile))
            {
                if (Directory.EnumerateFiles(englishScriptFolder, "*.txt", SearchOption.AllDirectories).Count() > 0)
                {
                    Tools.WriteLine("Official Translation Scripts found.", ConsoleColor.Green);
                    Cache.BuildOfficial();
                }

                includeOfficial = true;
            }

            // if not, loading from pre-existing cache if any
            if (official.Count == 0 && File.Exists(officialCacheFile))
            {
                Console.Write($"Loading Official Translation Cache:     ");
                official = Cache.LoadFromFile(officialCacheFile, true);

                includeOfficial = true;
            }

            // loading manual translation cache
            if (File.Exists(manualCacheFile))
            {
                Console.Write($"Loading Manual Translation Cache:     ");
                manual = Cache.LoadFromFile(manualCacheFile, true);
            }

            // loading machine translation cache
            if (File.Exists(machineCacheFile))
            {
                Console.Write($"Loading Machine Translation Cache:     ");
                machine = Cache.LoadFromFile(machineCacheFile, true);
            }


            Console.WriteLine("\n");

            if (official.Count > 0)
            {
                Console.WriteLine($"Officialy Translated lines Loaded: {official.Count}");
            }
            if (manual.Count > 0)
            {
                Console.WriteLine($"Manually Translated lines Loaded: {manual.Count}");
            }
            if (machine.Count > 0)
            {
                Console.WriteLine($"Machine Translated lines Loaded: {machine.Count}");
            }


            Console.WriteLine("\n===================== Translation =====================");

            Console.WriteLine("English translation will be selected in this order when available:");
            Tools.WriteLine("Manual Translation", ConsoleColor.Cyan);
            Tools.WriteLine("Official Translations", ConsoleColor.Green);
            Tools.WriteLine("Machine Translation", ConsoleColor.DarkBlue);
            Tools.WriteLine("New Translation (Sugoi must be running)", ConsoleColor.Blue);

            Console.ResetColor();

            // Checking if sugoi translator is ready
            isSugoiRunning = Tools.CheckTranslatorState();

            Console.WriteLine("Press any key to start");
            Console.ReadKey();

            // Create folder to sort script files in
            if (exportToi18nEx)
            {
                Script.CreateSortedFolders();
            }

            Task.Run(() => Tools.ListerKeyBoardEvent());

            IEnumerable<string> scriptFiles = Directory.EnumerateFiles(japaneseScriptFolder, "*.txt*", SearchOption.AllDirectories);
            double scriptTotal = scriptFiles.Count();
            double scriptCount = 0;
            int lineCount = 0;

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
                foreach(string l in lines)
                {
                    Line line = new Line(filename, l);
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
                    line = Cache.Get(line);

                    // if no translation from cache, ask SugoiTranslator and add to cache, otherwise leave it blanck
                    if (string.IsNullOrEmpty(line.English))
                    {
                        if (isSugoiRunning)
                        {
                            line = Translate.ToJapanese(line);

                            if (line.HasRepeat || line.HasError)
                            {
                                hasError = true;
                                Cache.AddToError(line);
                                Tools.WriteLine($"This line returned a faulty translation and was placed in {errorFile}", ConsoleColor.Red);
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
                    if (exportToi18nEx)
                    {
                        Script.AddTo(line);
                    }                    
                }

                scriptCount++;
                if (moveFinishedRawScript)
                {
                    Script.MoveFinished(file, hasError);
                }

                Console.Title = $"Processing ({scriptCount} out of {scriptTotal} scripts)";
            }

            // Adding back subtitles
            if (Directory.Exists("Caches/Subtitles") && exportToi18nEx)
            {
                IEnumerable<string> subtitlesFiles = Directory.EnumerateFiles("Caches/Subtitles");

                foreach (string subFile in subtitlesFiles)
                {
                    string subFileName = Path.GetFileName(subFile);
                    string [] scriptFile = Directory.GetFiles(i18nExScriptFolder, subFileName, SearchOption.AllDirectories);
                    if (scriptFile.Length > 0)
                    {
                        Tools.WriteLine($"Adding subtitles to {subFileName}.", ConsoleColor.Green);
                        string[] strings = File.ReadAllLines(subFile);
                        File.AppendAllLines(scriptFile[0], strings);
                    }
                    else
                    {
                        Tools.WriteLine($"Creating new subtitle script {subFileName}", ConsoleColor.Green);
                        string subPath = Path.Combine(i18nExScriptFolder, "[Subtitles]");
                        Tools.MakeFolder(subPath);
                        File.Copy(subFile, Path.Combine(subPath, subFileName));
                    }
                }
            }
            else
            {
                Tools.WriteLine($"No subtitles cache found, skipping", ConsoleColor.White);
            }


            Tools.WriteLine($"\n{lineCount} lines translated across {scriptCount} files.", ConsoleColor.Green);
            Tools.WriteLine("Everything done, you may recover your scripts in Scripts\\i18nEx and copy them in your game folder.", ConsoleColor.Green);
            Console.WriteLine("Press any key to close this program.");
            Console.ReadKey();
        }
    }
}
