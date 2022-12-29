using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace COM3D2.ScriptTranslationTool
{
    internal class Cache
    {
        /// <summary>
        /// Load translations from files
        /// </summary>
        /// <param name="file"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        internal static Dictionary<string, string> LoadFromFile(string file, bool progress = false)
        {
            if (File.Exists(file))
            {
                Dictionary<string, string> dict = new Dictionary<string, string>();
                List<string> subs = new List<string>();

                string[] rawText = File.ReadAllLines(file);
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
                        string[] parts = line.Split(Program.splitChar);
                        string key = parts[0];
                        string value = parts[1];

                        // remove unwanted scenarios
                        if (parts.Length != 2 || string.IsNullOrEmpty(value) || string.IsNullOrEmpty(key))
                        {
                            continue; 
                        }

                        if (!dict.ContainsKey(key))
                        {
                            dict[key] = value;
                        }                        
                    }
                    catch (IndexOutOfRangeException)
                    {
                        AddToError(new Line(file, line));
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
            else return null;
        }


        /// <summary>
        /// Build the official translation cache
        /// </summary>
        internal static void BuildOfficial()
        {
            string[] files = Directory.GetFiles(Program.englishScriptFolder, "*.txt", SearchOption.AllDirectories);
            double total = files.Length;
            double count = 0;

            Console.Write($"Building official cache from {total} Scripts:     ");


            // listing all english translated lines from the official scritps
            foreach (string file in files)
            {
                Dictionary<string, string> dict = LoadFromFile(file);

                foreach (KeyValuePair<string, string> kvp in dict)
                {
                    if (!Program.official.ContainsKey(kvp.Key))
                    {
                        Program.official.Add(kvp.Key, kvp.Value);

                        string str = Tools.FormatLine(kvp.Key, kvp.Value);
                        File.AppendAllText(Program.officialCacheFile, str);
                    }
                }

                count++;
                Tools.ShowProgress(count, total);
            }
        }


        /// <summary>
        /// Add subtitles to a specific cache.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="line"></param>
        internal static void BuildSubtitles(string file, List<string> subs)
        {
            // add to multiple files cache
            Tools.MakeFolder("Caches/Subtitles");
            File.AppendAllLines(Path.Combine("Caches", "Subtitles", Path.GetFileName(file)), subs);
        }


        /// <summary>
        /// Add an entry to the machine translation cache
        /// </summary>
        /// <param name="jp"></param>
        /// <param name="eng"></param>
        internal static void AddTo(Line line)
        {
            if (string.IsNullOrEmpty(line.Japanese) || string.IsNullOrEmpty(line.English))
            {
                return;
            }

            if (!Program.machine.ContainsKey(line.Japanese))
            {
                Program.machine.Add(line.Japanese, line.English);
            }            

            string savedLine = Tools.FormatLine(line.Japanese, line.English);
            File.AppendAllText(Program.machineCacheFile, savedLine, Encoding.UTF8);
        }


        /// <summary>
        /// Add a faulty line in an error file
        /// </summary>
        /// <param name="line"></param>
        internal static void AddToError(Line line)
        {
            string str = $"##{line.FileName}\n{line.Japanese}\n{line.English}\n\n";
            File.AppendAllText(Program.errorFile, str);
        }
 

        /// <summary>
        /// returns eventual translation from manual, official or machine cache 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        internal static Line Get(Line line)
        {
            if (Program.manual.ContainsKey(line.Japanese))
            {
                line.English = Program.manual[line.Japanese];
                line.Color = ConsoleColor.Cyan;
            }
            else if (Program.includeOfficial && Program.official.ContainsKey(line.Japanese))
            {
                line.English = Program.official[line.Japanese];
                line.Color = ConsoleColor.Green;
            }
            else if (Program.machine.ContainsKey(line.Japanese))
            {
                line.English = Program.machine[line.Japanese];
                line.Color = ConsoleColor.DarkBlue;
            }
            else
            {
                line.Color = ConsoleColor.Blue;                
            }
            return line;
        }
    }
}
