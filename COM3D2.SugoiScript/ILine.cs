using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace COM3D2.ScriptTranslationTool
{
    internal interface ILine
    {
        string FileName { get; set; }
        string Japanese { get; set; }
        string JapanesePrep { get; set; }
        string English { get; set; }
        ConsoleColor Color { get; set; }
        bool HasRepeat { get; set; }
        bool HasError { get; set; }
    }


    internal class ScriptLine : ILine
    {
        public string FileName { get; set; }
        public string Japanese { get; set; }
        public string JapanesePrep { get; set; }
        public string English { get; set; }
        public ConsoleColor Color { get; set; }
        public bool HasRepeat { get; set; } = false;
        public bool HasError { get; set; } = false;
        internal bool HasTag { get; set; } = false;
        internal List<string> Tags { get; set; } = new List<string>();

        internal ScriptLine(string fileName, string japanese)
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


        /// <summary>
        /// Clean all kind of reccurrent syntax error and put back any [HF] tag when needed
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        internal void CleanPost()
        {
            // replace MUKU by the corresponding [HF] tags
            if (this.HasTag)
            {
                //line.English = Regex.Replace(line.English, @"the tag placeholder", "TAGPLACEHOLDER", RegexOptions.IgnoreCase);

                Regex rx = new Regex(@"\bMUKU\b", RegexOptions.IgnoreCase);

                for (int i = 0; i < this.Tags.Count; i++)
                {
                    this.English = rx.Replace(this.English, this.Tags[i], 1);
                }

                // remove the from before [HF] tags
                this.English = this.English.Replace("the [", "[");
                this.English = this.English.Replace("The [", "[");
            }

            // unk ? Looks like symbols the translator doesn't know how to handle
            this.English = this.English.Replace("<unk>", "");


            // check for repeating characters
            Match matchChar = Regex.Match(this.English, @"(\w)\1{15,}");
            if (matchChar.Success)
            {
                this.HasRepeat = true;
            }

            // check for repating words
            Match matchWord = Regex.Match(this.English, @"(?<word>\w+)(-(\k<word>)){5,}");
            if (matchWord.Success)
            {
                this.HasRepeat = true;
            }

            // check for server bad request
            if (this.English.Contains("400 Bad Request"))
            {
                this.HasError = true;
            }
        }
    }

    internal class CsvLine : ILine
    {
        public string FileName { get; set; }
        public string Japanese { get; set; }
        public string JapanesePrep { get; set; }
        public string English { get; set; }
        public ConsoleColor Color { get; set; }
        public bool HasRepeat { get; set; }
        public bool HasError { get; set; }

        public string Key { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string ChSimple { get; set; } = string.Empty;
        public string ChTraditional { get; set; } = string.Empty;
        public string[] Header { get; set; }

        private readonly string[] matchString = new string[] {"|info", "|name" };

        public CsvLine(string fileName, string[] header, string[] values)
        {
            FileName = fileName;
            Header = header;

            Key = values[0];
            Type = values[1];
            Description = values[2];
            Japanese = values[3];
            English = values[4];

            /* I consider that if the Key contains |info / |name then the entry must be translated,
             * as it always seems to be the case. */
            if (matchString.Any(Key.Contains))
                English = string.Empty;


            if (values.Length > 6)
            {
                ChSimple = values[5];
                ChTraditional = values[6];
            }

            //Might change if I need to clean Japanese before translation
            JapanesePrep = Japanese;
        }

        /// <summary>
        /// Clean all kind of reccurrent syntax error and put back any [HF] tag when needed
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        internal void CleanPost()
        {
            // unk ? Looks like symbols the translator doesn't know how to handle
            this.English = this.English.Replace("<unk>", "");


            // check for repeating characters
            Match matchChar = Regex.Match(this.English, @"(\w)\1{15,}");
            if (matchChar.Success)
            {
                this.HasRepeat = true;
            }

            // check for repating words
            Match matchWord = Regex.Match(this.English, @"(?<word>\w+)(-(\k<word>)){5,}");
            if (matchWord.Success)
            {
                this.HasRepeat = true;
            }

            // check for server bad request
            if (this.English.Contains("400 Bad Request"))
            {
                this.HasError = true;
            }
        }

        public string ExportHeader()
        {
            string headerString = "";

            for (int i = 0; i < Header.Length; i++)
            {
                headerString = $"{headerString},{Header[i]}";
            }
            //remove first comma
            headerString = headerString.Remove(0, 1);
            //add eol
            headerString += "\n";

            return headerString;
        }

        public string ExportLine()
        {
            string line;

            //Don't export faulty translations, also add quotes, even if they don't exist in the original csv.
            if (HasError ||HasRepeat)
                line = $"\"{Key}\",{Type},{Description},\"{Japanese}\",";
            else
                line = $"\"{Key}\",{Type},{Description},\"{Japanese}\",\"{English}\"";

            if (Header.Length == 7)
                line = $"{line},\"{ChSimple}\",\"{ChTraditional}\"";

            return line ;
        }
    }
}
