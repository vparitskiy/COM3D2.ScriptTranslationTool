using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Bson;

namespace COM3D2.ScriptTranslationTool;

internal static class Cache
{
    //internal static Dictionary<string, string> machine = new Dictionary<string, string>();
    //internal static Dictionary<string, string> official = new Dictionary<string, string>();
    //internal static Dictionary<string, string> manual = new Dictionary<string, string>();

    internal static readonly Dictionary<string, ScriptLine> ScriptCache = new Dictionary<string, ScriptLine>();
    internal static readonly Dictionary<string, CsvLine> CsvCache = new Dictionary<string, CsvLine>();

    /// <summary>
    /// Load translations from files
    /// </summary>
    private static Dictionary<string, string> LoadFromFile(string file, bool progress = false)
    {
        var dict = new Dictionary<string, string>();

        if (!File.Exists(file)) return dict;
        var subs = new List<string>();

        var rawText = File.ReadAllLines(file);
        double total = rawText.Length;
        double count = 0;

        foreach (string line in rawText)
        {
            count++;

            if (line.StartsWith(@"//")) { continue; }
            if (string.IsNullOrEmpty(line)) { continue; }
            if (line.StartsWith(@"@VoiceSubtitle")) {
                subs.Add(line);
                continue; 
            }

            try
            {
                var parts = line.Split(Program.SplitChar);
                var key = parts[0];
                var value = parts[1];

                // remove unwanted scenarios
                if (parts.Length != 2 || string.IsNullOrEmpty(value) || string.IsNullOrEmpty(key))
                {
                    continue; 
                }

                dict.TryAdd(key, value);                        
            }
            catch (IndexOutOfRangeException)
            {
                AddToError(new ScriptLine(file, line));
                continue;
            }

            if (progress)
            {                        
                Tools.ShowProgress(count, total);
            }
        }

        if (subs.Count >= 1)
        {
            BuildSubtitles(file, subs);
        }
        return dict;
    }

    internal static void LoadOfficialCache(ref int officialCount)
    {
        if (!File.Exists(Program.officialCacheFile)) return;

        Console.Write($"Loading Official Translation Cache:     ");
        var officialDic = LoadFromFile(Program.officialCacheFile, true);

        foreach (var entry in officialDic)
        {
            officialCount++;

            if (ScriptCache.TryGetValue(entry.Key, out var scriptLine))
            {
                if (string.IsNullOrEmpty(scriptLine.OfficialTranslation))
                    scriptLine.OfficialTranslation = entry.Value;
            }
            else
            {
                ScriptCache.Add(entry.Key,
                    new ScriptLine(Program.officialCacheFile, entry.Key, official: entry.Value));
            }
        }
    }

    internal static void LoadMachineCache(ref int machineCount)
    {
        //loading machine .txt cache 
        if (!File.Exists(Program.machineCacheFile)) return;

        Console.Write($"Loading Machine Translation Cache:     ");
        var machineDic = LoadFromFile(Program.machineCacheFile, true);

        foreach (var entry in machineDic)
        {
            machineCount++;

            if (ScriptCache.TryGetValue(entry.Key, out var scriptLine))
            {
                if (string.IsNullOrEmpty(scriptLine.MachineTranslation))
                    scriptLine.MachineTranslation = entry.Value;
            }
            else
            {
                ScriptCache.Add(entry.Key,
                    new ScriptLine(Program.machineCacheFile, entry.Key, machine: entry.Value));
            }
        }
    }

    internal static void LoadManualCache(ref int manualCount)
    {
        //loading manual .txt cache 
        if (!File.Exists(Program.manualCacheFile)) return;

        Console.Write($"Loading Manual Translation Cache:     ");
        var manualDic = LoadFromFile(Program.manualCacheFile, true);

        foreach (var entry in manualDic)
        {
            manualCount++;
            if (ScriptCache.TryGetValue(entry.Key, out var scriptLine))
            {
                if (string.IsNullOrEmpty(scriptLine.ManualTranslation))
                    scriptLine.ManualTranslation = entry.Value;
            }
            else
            {
                ScriptCache.Add(entry.Key, new ScriptLine(Program.manualCacheFile, entry.Key, manual: entry.Value));
            }
        }


        // loading multiple custom translation .txt cache 
        var manualCaches = Directory.GetFiles(Program.cacheFolder, "CustomTranslationCache_*",
            SearchOption.AllDirectories);
            
        if (manualCaches.Length <= 0) return;

        foreach (var manualCache in manualCaches)
        {
            Console.Write(
                $"Loading additional Manual Translations [{Path.GetFileNameWithoutExtension(manualCache).Replace("ManualTranslationCache_", "")}]:     ");
            var loadedCache = LoadFromFile(manualCache, true);
            foreach (var entry in loadedCache)
            {
                manualCount++;
                if (ScriptCache.TryGetValue(entry.Key, out var scriptLine))
                {
                    if (string.IsNullOrEmpty(scriptLine.ManualTranslation))
                        scriptLine.ManualTranslation = entry.Value;
                }
                else
                {
                    ScriptCache.Add(entry.Key,
                        new ScriptLine(Program.manualCacheFile, entry.Key, manual: entry.Value));
                }
            }
        }
    }


    /// <summary>
    /// Build the official translation cache
    /// </summary>
    internal static void BuildOfficial()
    {
        var files = Directory.GetFiles(Program.englishScriptFolder, "*.*", SearchOption.AllDirectories).Where(f => Path.GetExtension(f) == ".txt").ToArray();
        double total = files.Length;
        double count = 0;

        //Skip if not script found
        if (files.Length == 0)
        {
            Tools.WriteLine($"No script found in: {Program.englishScriptFolder}", ConsoleColor.Red);
            Program.OptionMenu();
        }

        Console.Write($"Building official cache from {total} Scripts:     ");


        // listing all english translated lines from the official scripts and save as .txt cache
        foreach (var file in files)
        {
            var fileContent = LoadFromFile(file);

            foreach (var entry in fileContent)
            {
                if (ScriptCache.ContainsKey(entry.Key)) continue;
                var scriptLine = new ScriptLine(file, entry.Key, entry.Value);

                //add that line to the cache
                ScriptCache.Add(entry.Key, scriptLine);

                //add that line to the .txt cache
                var str = Tools.FormatLine(entry.Key, entry.Value);
                File.AppendAllText(Program.officialCacheFile, str);
            }

            count++;
            Tools.ShowProgress(count, total);
        }

        Program.OptionMenu();
    }


    /// <summary>
    /// Add subtitles to a specific cache.
    /// </summary>
    private static void BuildSubtitles(string file, List<string> subs)
    {
        // add to multiple files cache
        Tools.MakeFolder("Caches/Subtitles");
        File.WriteAllLines(Path.Combine("Caches", "Subtitles", Path.GetFileName(file)), subs);
    }


    /// <summary>
    /// Add an entry to the machine translation cache
    /// </summary>
    internal static void AddToMachineCache(ILine line)
    {
        if (string.IsNullOrEmpty(line.Japanese) || string.IsNullOrEmpty(line.MachineTranslation))
        {
            return;
        }

        var savedLine = Tools.FormatLine(line.Japanese, line.MachineTranslation);
        File.AppendAllText(Program.machineCacheFile, savedLine, Encoding.UTF8);
    }


    /// <summary>
    /// Add a faulty line in an error file
    /// </summary>
    internal static void AddToError(ILine line)
    {
        var str = $"##{line.FilePath}\n{line.Japanese}\n{line.English}\n\n";
        File.AppendAllText(Program.errorFile, str);
    }
 
    /* outdated, uses the old cache format
    /// <summary>
    /// returns eventual translation from manual, official or machine cache
    /// </summary>
    internal static ILine Get(ILine line)
    {
        if (manual.ContainsKey(line.Japanese))
        {
            line.ManualTranslation = manual[line.Japanese];
            line.Color = ConsoleColor.Cyan;
        }
        else if (official.ContainsKey(line.Japanese))
        {
            line.OfficialTranslation = official[line.Japanese];
            line.Color = ConsoleColor.Green;
        }
        else if (machine.ContainsKey(line.Japanese))
        {
            line.ManualTranslation = machine[line.Japanese];
            line.Color = ConsoleColor.DarkBlue;
        }
        else
        {
            line.Color = ConsoleColor.Blue;
        }
        return line;
    }
    */

    /// <summary>
    /// Save Cache as .json
    /// </summary>
    public static void SaveJson<T>(T dic, string cacheFilePath, bool isSafeExport = false)
    {
        if (isSafeExport)
        {
            var safeCacheFilePath = $"{ cacheFilePath}_safe";
            var safeJson = JsonConvert.SerializeObject(dic, Formatting.Indented, new JsonSerializerSettings { ContractResolver = new SafeContractResolver()});
            File.WriteAllText(safeCacheFilePath, safeJson);
        }

        var json = JsonConvert.SerializeObject(dic, Formatting.Indented);
        File.WriteAllText(cacheFilePath, json);
    }

    public static T LoadJson<T>(T dictionary, string path)
    {
        if (!File.Exists(path)) return dictionary;
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<T>(json);
    }

    public static void SaveBson<T>(T objectToSerialize, string path)
    {
        using var fileStream = new FileStream(path, FileMode.Create);
        using var writer = new BsonDataWriter(fileStream);
        var serializer = new JsonSerializer();
        serializer.Serialize(writer, objectToSerialize);
    }

    private class SafeContractResolver : DefaultContractResolver
    {
        private readonly string _startingWithOfficial = "Official";

        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base.CreateProperties(type, memberSerialization);

            // only serializer properties that don't start with "Official"
            properties =
                properties.Where(p => p.PropertyName != null && !p.PropertyName.StartsWith(_startingWithOfficial)).ToList();

            return properties;
        }
    }
}