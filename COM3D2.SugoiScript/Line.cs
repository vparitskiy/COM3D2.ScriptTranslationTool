using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace COM3D2.ScriptTranslationTool
{
    internal class Line
    {
        internal string FileName { get; set; }
        internal string Japanese { get; set; }
        internal string JapanesePrep { get; set; }
        internal string English { get; set; }
        internal ConsoleColor Color { get; set; }
        internal bool HasRepeat { get; set; } = false;
        internal bool HasError { get; set; } = false;
        internal bool HasTag { get; set; } = false;
        internal List<string> Tags { get; set; } = new List<string>();

        internal Line(string fileName, string japanese)
        {
            FileName = fileName;
            Japanese = japanese.Trim();


            // check for name tags [HF], [HF2], ... 
            Regex rx = new Regex(@"\[.*?\]", RegexOptions.Compiled);

            MatchCollection matches = rx.Matches(Japanese);

            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    Tags.Add(match.Groups[0].Value);
                }

                JapanesePrep = Regex.Replace(Japanese, @"\[.*?\]", "MUKU");

                HasTag = true;
            }
            else
            {
                JapanesePrep = Japanese;
            }

            // for the rare lines having quotes
            JapanesePrep = JapanesePrep.Replace("\"", "\\\"");

            // remove ♀ symbol because it messes up sugoi
            JapanesePrep = JapanesePrep.Replace("♀", "");

            // remove repeating "ーー" and "……" (not needed with no_repeat_ngram_size=3  in flaskServer.py)
            //JapanesePrep = Regex.Replace(JapanesePrep, @"(ー)\1+?", "ー");
            //JapanesePrep = Regex.Replace(JapanesePrep, @"(…)\1+", "…");
        }
    }
}
