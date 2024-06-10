using System;
using System.IO;
using System.Configuration;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace COM3D2.ScriptTranslationTool
{
    internal static class Tools
    {
        /// <summary>
        /// Display progress as xx% in the console
        /// </summary>
        internal static void ShowProgress(double current, double max)
        {
            double progress = (current / max) * 100;
            string str = Math.Floor(progress).ToString().PadRight(3);
            Console.Write($"\b\b\b\b{str}%");
            if (str == "100") { Console.Write("\n"); }
        }


        /// <summary>
        /// Create directory helper
        /// </summary>
        /// <param name="folderPath"></param>
        public static void MakeFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }


        /// <summary>
        /// return a formated line suited for scripts and caches
        /// </summary>
        /// <param name="jp"></param>
        /// <param name="eng"></param>
        /// <returns></returns>
        internal static string FormatLine(string jp, string eng)
        {
            string formatedLine = $"{jp}{Program.splitChar}{eng}\n";
            return formatedLine;
        }


        /// <summary>
        /// Check if sugoi translator is up and running
        /// </summary>
        internal static bool CheckTranslatorState()
        {
            bool isRunning;
            ILine test = new ScriptLine("test", "テスト");
            try
            {
                Translate.ToEnglish(test);
                Tools.WriteLine("\nSugoi Translator is Ready", ConsoleColor.Green);
                isRunning = true;
            }
            catch (Exception)
            {
                Tools.WriteLine("\nSugoi Translator is Offline, uncached sentences won't be translated", ConsoleColor.Red);
                isRunning= false;
            }
            return isRunning;
        }


        /// <summary>
        /// Recover config from file
        /// </summary>
        internal static void GetConfig()
        {
            if (File.Exists("COM3D2.ScriptTranslationTool.exe.config"))
            {
                Program.machineCacheFile = ConfigurationManager.AppSettings.Get("MachineTranslationCache");
                Program.officialCacheFile = ConfigurationManager.AppSettings.Get("OfficialTranslationCache");
                Program.officialSubtitlesCache = ConfigurationManager.AppSettings.Get("OfficialSubtitlesCache");
                Program.manualCacheFile = ConfigurationManager.AppSettings.Get("ManualTranslationCache");
                Program.errorFile = ConfigurationManager.AppSettings.Get("TranslationErrors");

                Program.japaneseScriptFolder = ConfigurationManager.AppSettings.Get("JapaneseScriptPath");
                Program.i18nExScriptFolder = ConfigurationManager.AppSettings.Get("i18nExScriptPath");
                Program.englishScriptFolder = ConfigurationManager.AppSettings.Get("EnglishScriptPath");
                Program.translatedScriptFolder = ConfigurationManager.AppSettings.Get("AlreadyTranslatedScriptFolder");

                Program.moveFinishedRawScript = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("MoveTranslated"));
                Program.exportToi18nEx = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("ExportToi18nEx"));

                Program.jpGameDataPath = ConfigurationManager.AppSettings.Get("JPGamePath");
                Program.engGameDataPath = ConfigurationManager.AppSettings.Get("ENGGamePath");
            }
        }

        /// <summary>
        /// WriteLine with selected color then reset to default
        /// </summary>
        /// <param name="str"></param>
        /// <param name="color"></param>
        internal static void WriteLine(string str, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(str);
            Console.ResetColor();
        }

        /// <summary>
        /// Write with selected color then reset to default
        /// </summary>
        /// <param name="str"></param>
        /// <param name="color"></param>
        internal static void Write(string str, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.Write(str);
            Console.ResetColor();
        }
    }
}