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
        internal static Dictionary<string, string> manual = new Dictionary<string, string>();
        internal static Dictionary<string, List<string>> subtitles = new Dictionary<string, List<string>>();

        internal static string cacheFolder = @"Caches";
        internal static string machineCacheFile = @"Caches\MachineTranslationCache.txt";
        internal static string officialCacheFile = @"Caches\OfficialTranslationCache.txt";
        internal static string officialSubtitlesCache = @"Caches\officialSubtitlesCache.txt";
        internal static string manualCacheFile = @"Caches\ManualTranslationCache.txt";
        internal static string errorFile = "Errors.txt";

        internal const char splitChar = '\t';

        internal static string japaneseScriptFolder = @"Scripts\Japanese";
        internal static string englishScriptFolder = @"Scripts\English";
        internal static string translatedScriptFolder = @"Scripts\AlreadyTranslated";
        internal static string japaneseUIFolder = @"UI\Japanese";
        internal static string i18nExScriptFolder = @"Scripts\i18nEx\English\Script";
        internal static string i18nExUIFolder = @"UI\i18nEx\English\UI";
        

        internal static bool isSugoiRunning = false;
        internal static bool includeOfficial = false;
        internal static bool exportToi18nEx = false;
        internal static bool moveFinishedRawScript = true;

        static void Main()
        {
            Tools.GetConfig();

            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Console.WriteLine("==================== Initialization ====================\n");
            Console.Title = "Initialization";

            Tools.MakeFolder(englishScriptFolder);
            Tools.MakeFolder(japaneseScriptFolder);
            Tools.MakeFolder("Caches");
            Tools.MakeFolder(japaneseUIFolder);

            if (moveFinishedRawScript)
                Tools.MakeFolder(translatedScriptFolder);

            int scriptsNb = Directory.EnumerateFiles(japaneseScriptFolder, "*.txt", SearchOption.AllDirectories).Count();
            int UInb = Directory.EnumerateFiles(japaneseUIFolder, "*.csv", SearchOption.AllDirectories).Count();

            if (scriptsNb > 0)
                Tools.WriteLine($"Number of script files to translate: {scriptsNb}", ConsoleColor.DarkGreen);
            else
                Tools.WriteLine($"No Japanese scripts found, skipping script translation", ConsoleColor.DarkRed);

            if (UInb > 0)
                Tools.WriteLine($"Number of UI files to translate: {UInb}", ConsoleColor.DarkGreen);
            else
                Tools.WriteLine($"No UI files found, skipping UI translation", ConsoleColor.DarkRed);
            Console.WriteLine("");


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

            // loading manual translation caches
            if (File.Exists(manualCacheFile))
            {
                Console.Write($"Loading Manual Translation Cache:     ");
                manual = Cache.LoadFromFile(manualCacheFile, true);
            }

            // loading multiple custom translation caches
            string[] manualCaches = Directory.GetFiles(cacheFolder, "CustomTranslationCache_*", SearchOption.AllDirectories);
            if (manualCaches.Length > 0)
            {
                foreach (string manualCache in manualCaches)
                {
                    Console.Write($"Loading additional Manual Translations [{Path.GetFileNameWithoutExtension(manualCache).Replace("ManualTranslationCache_", "")}]:     ");
                    var loadedCache = Cache.LoadFromFile(manualCache, true);
                    foreach (var entry in loadedCache)
                    {
                        if (!manual.ContainsKey(entry.Key))
                        {
                            manual.Add(entry.Key, entry.Value);
                        }
                    }
                }
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

            //Moved script translation to its own class
            double scriptCount = 0;
            int lineCount = 0;

            if (scriptsNb > 0)
            {
                ScriptTranslation.Process(ref scriptCount, ref lineCount);
            }

            if (UInb > 0)
            {
                UITranslation.Process();
            }

            if (scriptsNb > 0)
            {
                Tools.WriteLine($"\n{lineCount} lines translated across {scriptCount} files.", ConsoleColor.Green);
                Tools.WriteLine("Everything done, you may recover your scripts in Scripts\\i18nEx and copy them in your game folder.", ConsoleColor.Green);
            }
            if (UInb > 0)
            {
                Tools.WriteLine("\nEverything done, you may recover your UI files  in UI\\i18nEx and copy them in your game folder.", ConsoleColor.Green);
            }
            Console.WriteLine("Press any key to close this program.");
            Console.ReadKey();
        }
    }
}
