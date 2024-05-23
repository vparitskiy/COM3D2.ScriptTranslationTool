using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace COM3D2.ScriptTranslationTool
{
    internal static class UITranslation
    {
        internal static void Process()
        {
            Tools.MakeFolder(Program.i18nExUIFolder);
            IEnumerable<string> csvs = Directory.EnumerateFiles(Program.japaneseUIFolder, "*.csv*", SearchOption.AllDirectories);
            int csvCount = 1;
            
            foreach (string csv in csvs)
            {
                Tools.WriteLine($"\n-------- {Path.GetFileName(csv)} --------", ConsoleColor.Yellow);

                //reading csv files
                List<CsvLine> csvLines = ParseCSV(csv);

                //let's try to translate each line
                for(int i = 0; i < csvLines.Count; i++)
                {
                    Console.Title = $"Processing: line {i}/{csvLines.Count} from {csvCount}/{csvs.Count()} UI files";

                    var line = csvLines[i];

                    Console.Write(line.Japanese);
                    Tools.Write(" => ", ConsoleColor.Yellow);

                    //Some entries may be empty...
                    if (!String.IsNullOrWhiteSpace(line.Japanese))
                    {
                        // recover translation from caches
                        if (string.IsNullOrEmpty(line.English))
                            Cache.Get(line);

                        // if no translation from cache, ask SugoiTranslator and add to cache, otherwise leave it blanck
                        if (string.IsNullOrEmpty(line.English))
                        {
                            if (Program.isSugoiRunning)
                            {
                                Translate.ToEnglish(line);

                                line.CleanPost();

                                if (line.HasRepeat || line.HasError)
                                {
                                    Cache.AddToError(line);
                                    Tools.WriteLine($"This line returned a faulty translation and was placed in {Program.errorFile}", ConsoleColor.Red);
                                    continue;
                                }

                                Cache.AddTo(line);
                            }
                            else
                            {
                                Tools.WriteLine($"This line wasn't found in any cache and can't be translated since sugoi isn't running", ConsoleColor.Red);
                            }
                        }
                    }


                    Tools.WriteLine(line.English, line.Color);

                    csvLines[i] = line;
                }

                //Now that it's translated, let's rebuild the .csv

                //Ignore empty files
                if (csvLines.Count == 0) continue;

                //Get the new pathcreate folders and write the header
                string newPath = csv.Replace("UI\\Japanese", Program.i18nExUIFolder);
                Tools.MakeFolder(Path.GetDirectoryName(newPath));
                File.WriteAllText(newPath, csvLines[0].ExportHeader());

                //Write content
                List<string> lines = new List<string>();
                foreach(CsvLine line in csvLines)
                {
                    lines.Add(line.ExportLine());
                }

                File.AppendAllLines(newPath, lines);

                csvCount++;
            }
        }

        //This is using CsvTextFieldParser librabry https://github.com/22222/CsvTextFieldParser
        private static List<CsvLine> ParseCSV(string csvFileInput)
        {
            List<CsvLine> csvLines = new List<CsvLine>();

            string fileName = Path.GetFileName(csvFileInput);
            string csvInput = File.ReadAllText(csvFileInput);

            using (var csvReader = new StringReader(csvInput))
            using (var parser = new NotVisualBasic.FileIO.CsvTextFieldParser(csvReader))
            {
                // Save header to rebuild the csv and know the number of columns.
                string[] header = new string[0];
                if (!parser.EndOfData)
                {
                    header = parser.ReadFields();
                }
                //commented code was here to check where csv parsing broke
                //int i = 1;
                while (!parser.EndOfData)
                {
                    //i++;
                    //Tools.Write($" [{i}] ", ConsoleColor.DarkMagenta);
                    var values = parser.ReadFields();

                    CsvLine line = new CsvLine(fileName, header, values);

                    csvLines.Add(line);
                }
            }
            return csvLines;
        }
    }
}
