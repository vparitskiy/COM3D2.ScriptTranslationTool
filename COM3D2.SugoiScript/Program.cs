using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace COM3D2.ScriptTranslationTool
{
    internal static class Program
    {
        internal static Dictionary<string, List<string>> subtitles = new Dictionary<string, List<string>>();


        internal static string appDirectory = Directory.GetCurrentDirectory();

        internal static string cacheFolder = "Caches";
        internal static string machineCacheFile = Path.Combine(appDirectory, "Caches", "MachineTranslationCache.txt");
        internal static string officialCacheFile = Path.Combine(appDirectory, "Caches", "OfficialTranslationCache.txt");
        internal static string officialSubtitlesCache = Path.Combine(appDirectory, "Caches", "officialSubtitlesCache.txt");
        internal static string manualCacheFile = Path.Combine(appDirectory, "Caches", "ManualTranslationCache.txt");
        internal static string archistoryFolder = Path.Combine(appDirectory, "Caches", "ArcHistory");
        internal static string errorFile = "Errors.txt";
        internal const string JpCacheFile = "JpCache.json";
        internal const char SplitChar = '\t';

        internal static string japaneseScriptFolder = Path.Combine(appDirectory, "Scripts", "Japanese");
        internal static string englishScriptFolder = Path.Combine(appDirectory, "Scripts", "English");
        internal static string translatedScriptFolder = Path.Combine(appDirectory, "Scripts", "AlreadyTranslated");
        internal static string japaneseUIFolder = Path.Combine(appDirectory, "UI", "Japanese");
        internal static string i18NExScriptFolder = Path.Combine(appDirectory, "Scripts", "i18nEx", "English", "Script");
        internal static string i18NExUiFolder = Path.Combine(appDirectory, "UI", "i18nEx", "English", "UI");


        internal static string jpGameDataPath = "";
        internal static string engGameDataPath = "";


        internal static bool isSugoiRunning = false;
        internal static bool exportToi18NEx = false;
        internal static bool isSafeExport = true;
        internal static bool isExportBson = true;
        internal static bool moveFinishedRawScript = false;
        internal static bool forcedTranslation = false;
        internal static bool isSourceJpGame = true;
        internal static bool isSourceEngGame = true;
        internal static bool isIgnoreCbl = true;

        private static void Main()
        {
            Tools.GetConfig();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("==================== Initialization ====================\n");
            Console.Title = "Initialization";

            Tools.MakeFolder(englishScriptFolder);
            Tools.MakeFolder(japaneseScriptFolder);
            Tools.MakeFolder(cacheFolder);
            Tools.MakeFolder(japaneseUIFolder);
            Tools.MakeFolder(archistoryFolder);

            if (moveFinishedRawScript)
                Tools.MakeFolder(translatedScriptFolder);

            var scriptsNb = System.IO.Directory.EnumerateFiles(japaneseScriptFolder, "*.*", SearchOption.AllDirectories)
                .Count(f => Path.GetExtension(f) == ".txt");
            var uInb = System.IO.Directory.EnumerateFiles(japaneseUIFolder, "*.csv", SearchOption.AllDirectories).Count();


            /*
            if (scriptsNb > 0)
                Tools.WriteLine($"Number of script files to translate: {scriptsNb}", ConsoleColor.DarkGreen);
            else
                Tools.WriteLine($"No Japanese scripts found, skipping script translation", ConsoleColor.DarkRed);

            if (UInb > 0)
                Tools.WriteLine($"Number of UI files to translate: {UInb}", ConsoleColor.DarkGreen);
            else
                Tools.WriteLine($"No UI files found, skipping UI translation", ConsoleColor.DarkRed);
            Console.WriteLine("");
            */


            var officialCount = 0;
            var manualCount = 0;
            var machineCount = 0;

            Cache.LoadOfficialCache(ref officialCount);
            Cache.LoadManualCache(ref manualCount);
            Cache.LoadMachineCache(ref machineCount);


            Console.WriteLine("\n");

            if (officialCount > 0)
            {
                Console.WriteLine($"Officially Translated lines Loaded: {officialCount}");
            }

            if (manualCount > 0)
            {
                Console.WriteLine($"Manually Translated lines Loaded: {manualCount}");
            }

            if (machineCount > 0)
            {
                Console.WriteLine($"Machine Translated lines Loaded: {machineCount}");
            }

            Console.WriteLine("\n===================== Information =====================");

            Console.WriteLine("English translation will be selected in this order when available:");
            Tools.WriteLine("Manual Translation", ConsoleColor.Cyan);
            Tools.WriteLine("Official Translations", ConsoleColor.Green);
            Tools.WriteLine("Machine Translation", ConsoleColor.DarkBlue);
            Tools.WriteLine("New Translation (Sugoi must be running)", ConsoleColor.Blue);

            Console.ResetColor();


            // Checking if sugoi translator is ready
            isSugoiRunning = Tools.CheckTranslatorState();

            // Opening option menu loop
            OptionMenu();

            var scriptCount = 0;
            var lineCount = 0;

            if (scriptsNb > 0 || isSourceJpGame)
                ScriptTranslation.Process(ref scriptCount, ref lineCount);

            //if (UInb > 0) { UITranslation.Process(); }

            if (scriptsNb > 0 || isSourceJpGame)
            {
                Tools.WriteLine($"\n{lineCount} lines translated across {scriptCount} files.", ConsoleColor.Green);
                Tools.WriteLine("Everything done, you may recover your scripts in Scripts\\i18nEx and copy them in your game folder.", ConsoleColor.Green);
            }

            if (uInb > 0)
            {
                Tools.WriteLine("\nEverything done, you may recover your UI files  in UI\\i18nEx and copy them in your game folder.", ConsoleColor.Green);
            }

            Console.WriteLine("Press any key to close this program.");
            Console.ReadKey();
        }

        internal static void OptionMenu()
        {
            var key = new ConsoleKeyInfo();

            Console.WriteLine("\n===================== Options =====================");
            while (key.Key != ConsoleKey.Enter)
            {
                if ((key.Key == ConsoleKey.D1) || (key.Key == ConsoleKey.NumPad1)) { isSourceJpGame = !isSourceJpGame; }
                if ((key.Key == ConsoleKey.D2) || (key.Key == ConsoleKey.NumPad2)) { isSourceEngGame = !isSourceEngGame; }
                if ((key.Key == ConsoleKey.D3) || (key.Key == ConsoleKey.NumPad3)) { exportToi18NEx = !exportToi18NEx; }
                if ((key.Key == ConsoleKey.D4) || (key.Key == ConsoleKey.NumPad4)) { isSafeExport = !isSafeExport; }
                if ((key.Key == ConsoleKey.D5) || (key.Key == ConsoleKey.NumPad5)) { forcedTranslation = !forcedTranslation; }
                if ((key.Key == ConsoleKey.D6) || (key.Key == ConsoleKey.NumPad6)) { isExportBson = !isExportBson; }
                if ((key.Key == ConsoleKey.D7) || (key.Key == ConsoleKey.NumPad7)) { JpScriptExtraction.ExtractJapanese(isSourceJpGame); }
                if ((key.Key == ConsoleKey.D8) || (key.Key == ConsoleKey.NumPad8)) { EngScriptExtraction.ExtractOfficial(isSourceEngGame); }
                if ((key.Key == ConsoleKey.D9) || (key.Key == ConsoleKey.NumPad9)) { UITranslation.Process(); }


                Console.ResetColor();
                Console.Write($" 1. Japanese Script Source: ");
                Tools.WriteLine(isSourceJpGame ? "JP Game .arc" : "Script Folder", ConsoleColor.Blue);
                Console.Write($" 2. English Script Source: ");
                Tools.WriteLine(isSourceEngGame ? "ENG Game .arc" : "Script Folder", ConsoleColor.Blue);
                Console.Write($" 3. Export to i18nEx: ");
                Tools.WriteLine(exportToi18NEx.ToString(), ConsoleColor.Blue);
                Console.Write($" 4. Export with official translation: ");
                Tools.WriteLine((!isSafeExport).ToString(), ConsoleColor.Blue);
                Console.Write($" 5. Forced translation: ");
                Tools.WriteLine(forcedTranslation.ToString(), ConsoleColor.Blue);
                Console.Write($" 6. Export as: ");
                Tools.WriteLine(isExportBson ? "A single .bson" : "Collection of .txt", ConsoleColor.Blue);
                Console.Write($" 7. Build/Update the japanese cache. Source: ");
                Tools.WriteLine($"{(isSourceJpGame ? jpGameDataPath : japaneseScriptFolder)}", ConsoleColor.Blue);
                Console.Write($" 8. Build/Update the official translation cache. Source: ");
                Tools.WriteLine($"{(isSourceEngGame ? engGameDataPath : englishScriptFolder)}", ConsoleColor.Blue);
                Console.Write($" 9. Translate UI .csv");
                Console.Write("\nPress Numbers for options or Enter to start translating: ");

                key = Console.ReadKey();
                Console.SetCursorPosition(0, Console.CursorTop - 9);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.Write(new string(' ', Console.WindowWidth));
                Console.Write(new string(' ', Console.WindowWidth));
                Console.Write(new string(' ', Console.WindowWidth));
                Console.Write(new string(' ', Console.WindowWidth));
                Console.Write(new string(' ', Console.WindowWidth));
                Console.Write(new string(' ', Console.WindowWidth));
                Console.Write(new string(' ', Console.WindowWidth));
                Console.Write(new string(' ', Console.WindowWidth));
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, Console.CursorTop - 9);
            }
        }
    }
}