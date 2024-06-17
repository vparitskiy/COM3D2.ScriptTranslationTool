using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace COM3D2.ScriptTranslationTool
{
    internal static class ScriptTranslation
    {
        private static readonly HashSet<string> AlreadyParsedScripts = [];
        private static Dictionary<string, List<string>> _jpCache = new();
        private static readonly Dictionary<string, byte[]> BsonDictionary = new();
        private static List<string> _scriptFiles = [];


        internal static void Process(ref int scriptCount, ref int lineCount)
        {
            // Create folder to sort script files in
            if (Program.exportToi18NEx && !Program.isExportBson)
            {
                ScriptManagement.CreateSortedFolders();
            }

            //getting script list from one of two potential sources
            _scriptFiles = GetScripts();

            var scriptTotal = _scriptFiles.Count;

            if (scriptTotal == 0)
            {
                Tools.WriteLine("No Scripts or Cache found, Translation Aborted", ConsoleColor.Red);
                Program.OptionMenu();
            }

            foreach (var file in _scriptFiles)
            {
                if (AlreadyParsedScripts.Contains(file)) { continue; }

                var concatStrings = new StringBuilder();
                var filename = Path.GetFileName(file);
                var hasError = false;

                Tools.WriteLine($"\n-------- {filename} --------", ConsoleColor.Yellow);

                //getting line list from one of two potential sources
                var lines = GetLines(filename);

                foreach (var line in lines.Select(l => l.Trim()))
                {
                    var currentLine = Cache.ScriptCache.TryGetValue(line, out var value) ? value : new ScriptLine(filename, line);

                    lineCount++;

                    // skip if line is empty
                    if (string.IsNullOrEmpty(currentLine.Japanese)) { continue; }

                    Console.Write(currentLine.Japanese);
                    Tools.Write(" => ", ConsoleColor.Yellow);


                    //Translate if needed/possible
                    if (Program.isSugoiRunning && (string.IsNullOrEmpty(currentLine.English) || (Program.forcedTranslation && string.IsNullOrEmpty(currentLine.MachineTranslation))))
                    {
                        currentLine.GetTranslation();

                        //ignore faulty returns
                        if (currentLine.HasRepeat || currentLine.HasError)
                        {
                            hasError = true;
                            Cache.AddToError(currentLine);
                            Tools.WriteLine($"This line returned a faulty translation and was placed in {Program.errorFile}", ConsoleColor.Red);
                            continue;
                        }

                        Cache.AddToMachineCache(currentLine);
                    }
                    else if (string.IsNullOrEmpty(currentLine.English))
                    {
                        Tools.WriteLine($"This line wasn't found in any cache and can't be translated since sugoi isn't running", ConsoleColor.Red);
                        continue;
                    }


                    Tools.WriteLine(currentLine.English, currentLine.Color);

                    currentLine.FilePath = filename;

                    // add to i18nEx script folder
                    if (Program.exportToi18NEx)
                    {
                        if (Program.isExportBson)
                            concatStrings.AppendLine($"{currentLine.Japanese}\t{currentLine.English}");                        
                        else
                            ScriptManagement.AddTo(currentLine);
                    }
                }

                scriptCount++;

                if (Program.isExportBson)
                {
                    BsonDictionary.Add(filename,Encoding.UTF8.GetBytes(concatStrings.ToString()));
                }

                AlreadyParsedScripts.Add(filename);

                if (Program.moveFinishedRawScript)
                {
                    ScriptManagement.MoveFinished(file, hasError);
                }

                Console.Title = $"Processing ({scriptCount} out of {scriptTotal} scripts)";
            }

            // Adding back subtitles
            if (Directory.Exists(Path.Combine(Program.cacheFolder, "Subtitles")) && Program.exportToi18NEx)
            {
                IEnumerable<string> subtitlesFiles = Directory.EnumerateFiles(Path.Combine(Program.cacheFolder, "Subtitles"));

                if (subtitlesFiles.Any())
                {
                    foreach (string subFile in subtitlesFiles)
                    {
                        string subFileName = Path.GetFileName(subFile);

                        if (Program.isExportBson)
                        {
                            if (BsonDictionary.ContainsKey(subFileName))
                            {
                                BsonDictionary[subFileName] = BsonDictionary[subFileName].Concat(File.ReadAllBytes(subFile)).ToArray();
                            }
                            else
                            {
                                BsonDictionary.Add(subFile, File.ReadAllBytes(subFile));
                            }
                        }
                        else
                        {
                            string[] scriptFile = Directory.GetFiles(Program.i18NExScriptFolder, subFileName, SearchOption.AllDirectories);
                            if (scriptFile.Length > 0)
                            {
                                Tools.WriteLine($"Adding subtitles to {subFileName}.", ConsoleColor.Green);
                                string[] strings = File.ReadAllLines(subFile);
                                File.AppendAllLines(scriptFile[0], strings);
                            }
                            else
                            {
                                Tools.WriteLine($"Creating new subtitle script {subFileName}", ConsoleColor.Green);
                                string subPath = Path.Combine(Program.i18NExScriptFolder, "[Subtitles]");
                                Tools.MakeFolder(subPath);
                                File.Copy(subFile, Path.Combine(subPath, subFileName));
                            }
                        }
                    }
                }
            }
            else
            {
                Tools.WriteLine($"No subtitles cache found, skipping", ConsoleColor.White);
            }

            if (Program.exportToi18NEx && Program.isExportBson)
            {
                Tools.WriteLine("Saving script as .bson.", ConsoleColor.Magenta);
                string bsonPath = Path.Combine(Program.i18NExScriptFolder, "script.bson");
                Cache.SaveBson(BsonDictionary, bsonPath);
            }
        }



        private static List<string> GetScripts()
        {
            var scripts = new List<string>();

            if (Program.isSourceJpGame)
            {
                string jpCachePath = Path.Combine(Program.cacheFolder, Program.JpCacheFile);

                if (File.Exists(jpCachePath))
                {
                    Console.WriteLine("Loading Jp cache");
                    _jpCache = Cache.LoadJson(_jpCache, jpCachePath);
                    Tools.WriteLine($"{_jpCache.Count} scripts cached", ConsoleColor.Green);

                    scripts = _jpCache.Keys.ToList();
                }
            }
            else
            {
                scripts = Directory.EnumerateFiles(Program.japaneseScriptFolder, "*.txt*", SearchOption.AllDirectories)
                                   .ToList();
            }

            return scripts;
        }

        private static List<string> GetLines(string filename)
        {
            List<string> lines = new List<string>();

            if (Program.isSourceJpGame)
            {
                lines = _jpCache[filename];
            }
            else
            {
                //Load all scripts with the same name
                string[] sameNameScripts = _scriptFiles.Where(f => Path.GetFileName(f) == filename).ToArray();

                //merge them as one without duplicated lines.
                foreach (string s in sameNameScripts)
                {
                    lines.AddRange(File.ReadAllLines(s));
                }
            }

            return lines.Distinct().ToList();
        }
    }
}
