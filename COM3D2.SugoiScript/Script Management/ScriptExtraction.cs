using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CM3D2.Toolkit.Guest4168Branch.Arc;
using Newtonsoft.Json;
using ArcFileEntry = CM3D2.Toolkit.Guest4168Branch.Arc.Entry.ArcFileEntry;

namespace COM3D2.ScriptTranslationTool
{
    internal static class EngScriptExtraction
    {
        private const string EngArcHistoryFile = "eng_archistory.json";
        private static Dictionary<string, long> _arcHistoryEng = new();
        internal static HashSet<string> shortOfficialCache = []; //shortOfficialCache is there to avoid duplicated lines

        internal static void ExtractOfficial(bool isSourceEngGame)
        {
            //If built from the Script/English folder
            if (!isSourceEngGame)
            {
                Cache.BuildOfficial();
                Program.OptionMenu();
            }

            Console.Title = "Extracting ENG Scripts";

            if (string.IsNullOrEmpty(Program.engGameDataPath))
            {
                Tools.WriteLine("English game install path isn't set, please enter it.", ConsoleColor.Yellow);
                Tools.Write("COM3D2.exe path:", ConsoleColor.White);
                var path = Console.ReadLine();
                Program.engGameDataPath = Path.Combine(path, "GameData");
            }

            //Don't bother if the GameData can't be found
            if (!Directory.Exists(Program.engGameDataPath))
            {
                Tools.WriteLine($"{Program.engGameDataPath} doesn't exist, aborting extraction", ConsoleColor.Red);
                Program.OptionMenu();
            }


            var arcHistoryJson = Path.Combine(Program.archistoryFolder, EngArcHistoryFile);
            if (File.Exists(Program.officialCacheFile))
            {
                shortOfficialCache = File.ReadAllLines(Program.officialCacheFile).ToHashSet();
                Tools.WriteLine($"{shortOfficialCache.Count} lines already cached", ConsoleColor.Green);

                if (File.Exists(arcHistoryJson))
                {
                    Console.WriteLine("Loading extraction history");
                    _arcHistoryEng = Cache.LoadJson(_arcHistoryEng, arcHistoryJson);
                }
            }

            var startingLineNb = shortOfficialCache.Count;
            var newScripts = 0;
            var arcs = Directory.GetFiles(Program.engGameDataPath, "*.*", SearchOption.AllDirectories).Where(f => Path.GetExtension(f) == ".arc").ToArray();

            var scriptScannedNb = 0;
            foreach (var arc in arcs)
            {
                //skip already parsed .arc if they haven't changed size
                var arcName = Path.GetFileName(arc);
                var arcFileSize = new FileInfo(arc).Length;

                if (_arcHistoryEng.TryGetValue(arcName, out var value))
                {
                    if (value == arcFileSize)
                        continue;
                    _arcHistoryEng.Remove(arcName);
                }

                Tools.Write($"{arcName}: ", ConsoleColor.Gray);

                //opening the .arc file
                var arcFile = new ArcFile(arc);

                //getting all .ks and their string content
                foreach (var script in arcFile.GetAllScripts())
                {
                    //Console.Write($", [{script.Name}]");
                    for (var i = 0; i < script.Lines.Length; i++)
                    {
                        //Special case for the main story subtitles
                        if (script.Lines[i].StartsWith("@LoadSubtitleFile"))
                        {
                            script.CaptureSubtitleFile(i, arcFile);
                        }

                        //written dialogues always start by "@talk"
                        else if (script.Lines[i].StartsWith("@talk", StringComparison.InvariantCultureIgnoreCase))
                        {
                            script.CaptureTalk(i);
                        }

                        //another type of subtitles
                        else if (script.Lines[i].StartsWith("@SubtitleDisplayForPlayVoice", StringComparison.InvariantCultureIgnoreCase))
                        {
                            script.CaptureSubtitle(i);
                        }

                        //Choice type boxes
                        else if (script.Lines[i].StartsWith("@ChoicesSet", StringComparison.InvariantCultureIgnoreCase))
                        {
                            script.CaptureChoice(i);
                        }
                    }

                    scriptScannedNb++;
                    newScripts++;
                    script.SaveToCache(Program.officialCacheFile, false);
                    script.SaveToCache(Path.Combine(Program.cacheFolder, "__npc_names.txt"), true);
                    script.SaveSubtitles();
                }

                Tools.WriteLine($"{scriptScannedNb} scripts", ConsoleColor.Green);
                _arcHistoryEng.Add(arcName, arcFileSize);
                Cache.SaveJson(_arcHistoryEng, arcHistoryJson);
            }

            Tools.WriteLine($"{shortOfficialCache.Count - startingLineNb} new translations in {newScripts} new scripts\n\n", ConsoleColor.Green);

            var offCount = 0;
            if (newScripts > 0)
                Cache.LoadOfficialCache(ref offCount); //reload the cache if anything was added.
        }
    }

    internal static class JpScriptExtraction
    {
        private const string JpArcHistoryFile = "jp_archistory.json";
        private static Dictionary<string, long> _arcHistoryJp = new();
        private static Dictionary<string, List<string>> _jpCache = new();

        internal static void ExtractJapanese(bool isSourceJpGame)
        {
            //If built from the Script/Japanese folder
            if (!isSourceJpGame)
            {
                Program.OptionMenu();
            }

            Console.Title = "Extracting JP Scripts";

            if (string.IsNullOrEmpty(Program.jpGameDataPath))
            {
                Tools.WriteLine("Japanese game install path isn't set, please enter it.", ConsoleColor.Yellow);
                Tools.Write("COM3D2.exe path:", ConsoleColor.White);
                var path = Console.ReadLine();
                Program.jpGameDataPath = Path.Combine(path, "GameData");
            }

            //Don't bother if the GameData can't be found
            if (!Directory.Exists(Program.jpGameDataPath))
            {
                Tools.WriteLine($"{Program.jpGameDataPath} doesn't exist, aborting extraction", ConsoleColor.Red);
                Program.OptionMenu();
            }

            string jpCachePath = Path.Combine(Program.cacheFolder, Program.JpCacheFile);
            string arcHistoryJson = Path.Combine(Program.archistoryFolder, JpArcHistoryFile);

            if (File.Exists(jpCachePath))
            {
                Console.WriteLine("Loading extraction history");
                _jpCache = Cache.LoadJson(_jpCache, jpCachePath);
                Tools.WriteLine($"{_jpCache.Count} scripts already cached", ConsoleColor.Green);

                if (File.Exists(arcHistoryJson))
                {
                    _arcHistoryJp = Cache.LoadJson(_arcHistoryJp, arcHistoryJson);
                    Tools.WriteLine($"{_arcHistoryJp.Count} .arc already scanned.", ConsoleColor.Green);
                    Console.WriteLine("Only new or updated .arc will be scanned");
                }
            }


            int newScripts = 0;
            int newLines = 0;
            string[] arcs = Directory.GetFiles(Program.jpGameDataPath, "*.*", SearchOption.AllDirectories).Where(f => Path.GetExtension(f) == ".arc").ToArray();

            foreach (string arc in arcs)
            {
                //skip already parsed .arc if they haven't changed size
                string arcName = Path.GetFileName(arc);
                long arcFileSize = new FileInfo(arc).Length;

                if (_arcHistoryJp.ContainsKey(arcName))
                {
                    if (_arcHistoryJp[arcName] == arcFileSize)
                        continue;
                    else
                        _arcHistoryJp.Remove(arcName);
                }

                //ignore ChuBLips stuff
                if (arcName.Contains("_cbl") && Program.isIgnoreCbl)
                {
                    continue;
                }

                Tools.Write($"{arcName}: ", ConsoleColor.Gray);

                //opening the .arc file
                var arcFile = new ArcFile(arc);

                var scriptScannedNb = 0;

                //getting all .ks and their string content
                foreach (var script in arcFile.GetAllScripts())
                {
                    //Console.Write($", [{script.Name}]");
                    for (int i = 0; i < script.Lines.Length; i++)
                    {
                        //Jp doesn't support subtitles, so I don't bother checking for them.
                        //written dialogues always start by "@talk"
                        if (script.Lines[i].StartsWith("@talk", StringComparison.InvariantCultureIgnoreCase))
                        {
                            script.CaptureTalk(i);
                        }

                        //Choice type boxes
                        else if (script.Lines[i].StartsWith("@ChoicesSet", StringComparison.InvariantCultureIgnoreCase))
                        {
                            script.CaptureChoice(i);
                        }
                    }

                    //get all parsed lines and add them to the cache, making sure they are unique
                    var newJpTalks = script.GetJpTalks();

                    if (_jpCache.TryGetValue(script.Name, out var value))
                    {
                        value.AddRange(newJpTalks);
                        _jpCache[script.Name] = value.Distinct().ToList();
                        newLines += _jpCache[script.Name].Count;
                    }
                    else
                    {
                        if (newJpTalks.Count > 0) //avoid empty scripts
                            _jpCache.Add(script.Name, newJpTalks);
                    }

                    scriptScannedNb++;
                    newScripts++;
                }

                Tools.WriteLine($"{scriptScannedNb} scripts", ConsoleColor.Green);
                _arcHistoryJp.Add(arcName, arcFileSize);
                Cache.SaveJson(_arcHistoryJp, arcHistoryJson);
            }

            Console.WriteLine("Saving Jp Cache.");
            Cache.SaveJson(_jpCache, jpCachePath);
            Tools.WriteLine($"{newLines} new/updated lines in {newScripts} new/updated scripts\n\n", ConsoleColor.Green);
        }
    }


    internal partial class ScriptFile
    {
        public string Name { get; set; }
        public string Content { get; set; }
        public string[] Lines { get; set; }
        private List<(string Jp, string Eng)> Talks { get; set; } = [];
        private List<(string Jp, string Eng)> Npcs { get; set; } = [];
        private List<SubtitleData> Subs { get; set; } = [];

        [GeneratedRegex(@"text=""(.*?)""")]
        private static partial Regex SubtitleRegex();

        [GeneratedRegex(@"text=""(.*?)""")]
        private static partial Regex ChoiceRegex();

        internal ScriptFile(string name, string content)
        {
            Name = name;
            Content = content;
            Lines = content.Split('\n')
                .Where(l => !l.StartsWith(";"))
                .Select(l => l.Trim())
                .ToArray();
        }

        internal void CaptureSubtitleFile(int i, ArcFile arcFile)
        {
            //getting the subtitle file and loading it
            int pos;
            if ((pos = Lines[i].IndexOf("file=", StringComparison.InvariantCultureIgnoreCase)) <= 0) return;
            var fileName = Lines[i][(pos + 5)..].Trim();
            var subScript = arcFile.GetScript(fileName);

            //get the first voice played, as it seems to be the starting point of all subtitles
            while (i < Lines.Length && !Lines[i].StartsWith("@PlayVoice"))
            {
                i++;
            }

            var voice = "";
            if ((pos = Lines[i].IndexOf("voice=", StringComparison.InvariantCultureIgnoreCase)) > 0)
            {
                voice = Lines[i].Substring(pos + 6).Replace("wait", "").Trim();
            }

            //parse the subtitle script
            for (var j = 0; j < subScript.Lines.Length; j++)
            {
                (int Start, int End) timing;
                if (!subScript.Lines[j].StartsWith("@talk", StringComparison.InvariantCultureIgnoreCase)) continue;
                // get the timings
                var talkTiming = subScript.Lines[j].Substring("@talk".Length).Trim('[', ']', ' ').Split('-');
                timing.Start = int.Parse(talkTiming[0]);
                timing.End = int.Parse(talkTiming[1]);

                //Capture the JP and ENG text
                j++;
                var sb = new StringBuilder();
                while (!subScript.Lines[j].StartsWith("@hitret", StringComparison.InvariantCultureIgnoreCase))
                {
                    sb.Append(subScript.Lines[j]);
                    j++;
                }

                var line = SplitTranslation(sb.ToString());

                //shove everything in an i18nEx compatible subtitle format
                var subTitleData = new SubtitleData
                {
                    Original = line.Jp,
                    Translation = line.Eng,
                    StartTime = timing.Start,
                    DisplayTime = timing.End - timing.Start,
                    Voice = voice,
                    IsCasino = false
                };
                Subs.Add(subTitleData);
            }
        }

        internal void CaptureSubtitle(int i)
        {
            //Check CaptureSubtitlesFiles() it works nearly the same
            var isCasino = false;
            (string Jp, string Eng) line = (string.Empty, string.Empty);


            //getting text with regex this time as it's nested in "quotes"
            if (Lines[i].Contains("text=", StringComparison.CurrentCultureIgnoreCase))
            {
                var match = SubtitleRegex().Match(Lines[i]);
                line = SplitTranslation(match.Groups[1].Value);
                isCasino = Lines[i].Contains("mode_c", StringComparison.CurrentCultureIgnoreCase);
            }

            while (!Lines[i].Contains("@PlayVoice"))
            {
                i++;
            }

            var voice = "";
            int pos;
            if ((pos = Lines[i].IndexOf("voice=", StringComparison.InvariantCultureIgnoreCase)) > 0)
            {
                voice = Lines[i][(pos + 6)..].Replace("wait", "").Trim();
            }

            var subData = new SubtitleData
            {
                Original = line.Jp,
                Translation = line.Eng,
                IsCasino = isCasino,
                Voice = voice
            };
            
            Subs.Add(subData);
        }

        internal void CaptureTalk(int i)
        {
            //In some cases a NPC name can be specified
            var talkLine = Lines[i];
            int pos;
            if ((pos = talkLine.IndexOf("name=", StringComparison.InvariantCultureIgnoreCase)) > 0)
            {
                var name = talkLine[(pos + 5)..];
                if (!name.StartsWith('['))
                {
                    if (name.Contains("real=", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var realPos = name.IndexOf("real=", StringComparison.CurrentCultureIgnoreCase);
                        name = name[..(realPos - 1)].Replace("\"", "").Trim();
                    }

                    Npcs.Add(SplitTranslation(name.Trim('\"')));
                }
            }

            //Capture the JP text and ENG
            i++;
            var sb = new StringBuilder();
            while (!Lines[i].StartsWith("@", StringComparison.InvariantCultureIgnoreCase))
            {
                sb.Append(Lines[i]);
                i++;
            }

            var line = SplitTranslation(sb.ToString());
            Talks.Add(line);
        }

        internal void CaptureChoice(int i)
        {
            //getting text with regex this time as it's nested in "quotes"
            if (!Lines[i].Contains("text=", StringComparison.CurrentCultureIgnoreCase)) return;
            var match = ChoiceRegex().Match(Lines[i]);
            var line = SplitTranslation(match.Groups[1].Value);
            Talks.Add(line);
        }

        private static (string Jp, string Eng) SplitTranslation(string text)
        {
            int pos;
            if ((pos = text.IndexOf("<e>", StringComparison.InvariantCultureIgnoreCase)) <= 0) return (text.Trim(), string.Empty);
            var japanese = text.Substring(0, pos).Trim();
            var english = text.Substring(pos + 3).Replace("…", "...").Replace("<E>", "").Trim(); //had to add <E> replace because of Kiss <E><E> errors 
            return (japanese, english);

        }

        internal void SaveToCache(string cachePath, bool isNpc)
        {
            string[] content;

            if (isNpc)
            {
                content = Npcs.Distinct()
                    .Where(t => !EngScriptExtraction.shortOfficialCache.Contains($"{t.Jp}\t{t.Eng}") && !string.IsNullOrEmpty(t.Eng))
                    .Select(t => $"{t.Jp}\t{t.Eng}").ToArray();
            }
            else
            {
                content = Talks.Distinct()
                    .Where(t => !EngScriptExtraction.shortOfficialCache.Contains($"{t.Jp}\t{t.Eng}") && !string.IsNullOrEmpty(t.Eng))
                    .Select(t => $"{t.Jp}\t{t.Eng}").ToArray();
            }

            File.AppendAllLines(cachePath, content);
            EngScriptExtraction.shortOfficialCache.UnionWith(content);
        }

        internal void SaveSubtitles()
        {
            if (Subs.Count <= 0) return;


            Tools.MakeFolder(Path.Combine(Program.cacheFolder, "Subtitles"));
            var path = $"{Path.Combine(Program.cacheFolder, "Subtitles", Path.GetFileNameWithoutExtension(Name)!)}.txt";
            var formatedSubs = Subs.Where(s => !string.IsNullOrEmpty(s.Original) || !string.IsNullOrEmpty(s.Translation))
                .Select(s => $"@VoiceSubtitle{JsonConvert.SerializeObject(s)}")
                .ToArray();
            
            File.WriteAllLines(path, formatedSubs);
        }

        internal List<string> GetJpTalks()
        {
            return Talks.Where(t => !string.IsNullOrEmpty(t.Jp))
                .Select(t => t.Jp.Trim())
                .Distinct()
                .ToList();
        }
    }

    internal class ArcFile
    {
        private readonly ArcFileSystem _arc = new();

        internal ArcFile(string path)
        {
            _arc.LoadArc(path);
        }

        internal List<ScriptFile> GetAllScripts()
        {
            return (from KeyValuePair<string, ArcFileEntry> arcEntry in _arc.Files.Where(f => Path.GetExtension(f.Value.Name) == ".ks")
                let pointer = arcEntry.Value.Pointer.Decompress() //Looks like all scripts are compressed, won't hurt if they aren't
                let textData = Encoding.GetEncoding(932).GetString(pointer.Data) //And they are encoded as Shift JIS (codepage=932)
                select new ScriptFile(arcEntry.Value.Name, textData)).ToList();
        }

        internal ScriptFile GetScript(string fileName)
        {
            fileName = $"{fileName}".ToLower();
            //Console.WriteLine($"Trying to get {fileName}");

            var scripts = (from KeyValuePair<string, ArcFileEntry> arcEntry in _arc.Files.Where(f => Path.GetFileNameWithoutExtension(f.Value.Name) == fileName)
                let pointer = arcEntry.Value.Pointer.Decompress()
                let textData = Encoding.GetEncoding(932).GetString(pointer.Data)
                select new ScriptFile(arcEntry.Value.Name, textData)).ToArray();

            return scripts[0];
        }
    }

    internal class SubtitleData
    {
        public int AddDisplayTime = 0;
        public int DisplayTime = -1;
        public bool IsCasino;
        public string Original = string.Empty;
        public int StartTime;
        public string Translation = string.Empty;
        public string Voice = string.Empty;
    }
}