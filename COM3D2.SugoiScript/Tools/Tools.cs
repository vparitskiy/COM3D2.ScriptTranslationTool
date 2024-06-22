using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using Microsoft.Win32;

namespace COM3D2.ScriptTranslationTool;

internal static class Tools
{
    /// <summary>
    /// Display progress as xx% in the console
    /// </summary>
    internal static void ShowProgress(double current, double max)
    {
        var progress = (current / max) * 100;
        var str = Math.Floor(progress).ToString(CultureInfo.CurrentCulture).PadRight(3);
        Console.Write($"\b\b\b\b{str}%");
        if (str == "100") { Console.Write("\n"); }
    }


    /// <summary>
    /// Create directory helper
    /// </summary>
    /// <param name="folderPath"></param>
    public static void MakeFolder(string folderPath)
    {
        if (Directory.Exists(folderPath)) return;
        Directory.CreateDirectory(folderPath);
    }


    /// <summary>
    /// return a formated line suited for scripts and caches
    /// </summary>
    /// <param name="jp"></param>
    /// <param name="eng"></param>
    /// <returns></returns>
    internal static string FormatLine(string jp, string eng)
    {
        return $"{jp}{Program.SplitChar}{eng}\n";
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
            WriteLine("\nSugoi Translator is Ready", ConsoleColor.Green);
            isRunning = true;
        }
        catch (Exception)
        {
            WriteLine("\nSugoi Translator is Offline, uncached sentences won't be translated", ConsoleColor.Red);
            isRunning= false;
        }
        return isRunning;
    }


    /// <summary>
    /// Recover config from file
    /// </summary>
    internal static void GetConfig()
    {
        if (!File.Exists("COM3D2.ScriptTranslationTool.dll.config")) return;
        Program.machineCacheFile = GetAbsolutePath(ConfigurationManager.AppSettings.Get("MachineTranslationCache"));
        Program.officialCacheFile = GetAbsolutePath(ConfigurationManager.AppSettings.Get("OfficialTranslationCache"));
        Program.officialSubtitlesCache = GetAbsolutePath(ConfigurationManager.AppSettings.Get("OfficialSubtitlesCache"));
        Program.manualCacheFile = GetAbsolutePath(ConfigurationManager.AppSettings.Get("ManualTranslationCache"));
        
        Program.errorFile = GetAbsolutePath(ConfigurationManager.AppSettings.Get("TranslationErrors"));

        Program.japaneseScriptFolder = GetAbsolutePath(ConfigurationManager.AppSettings.Get("JapaneseScriptPath"));
        Program.i18NExScriptFolder = GetAbsolutePath(ConfigurationManager.AppSettings.Get("i18nExScriptPath"));
        Program.englishScriptFolder = GetAbsolutePath(ConfigurationManager.AppSettings.Get("EnglishScriptPath"));
        Program.translatedScriptFolder = GetAbsolutePath(ConfigurationManager.AppSettings.Get("AlreadyTranslatedScriptFolder"));

        Program.moveFinishedRawScript = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("MoveTranslated"));
        Program.exportToi18NEx = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("ExportToi18nEx"));
        
        // Getting GameData path Setting > Registry > Ask
        if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings.Get("JPGamePath")))
        {
            var path = Path.Combine(ConfigurationManager.AppSettings.Get("JPGamePath"),"GameData");
            Program.jpGameDataPath = GetAbsolutePath(path);
        }

        if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings.Get("ENGGamePath")))
        {
            var path = Path.Combine(ConfigurationManager.AppSettings.Get("ENGGamePath"), "GameData");
            Program.engGameDataPath = GetAbsolutePath(path);
        }
    }

    private static string GetAbsolutePath(string path)
    {
        var absolutePath = Path.IsPathRooted(path)
            ? path
            : Path.Combine(Program.appDirectory, path ?? throw new ArgumentNullException(nameof(path)));
        return absolutePath;
    }
    
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