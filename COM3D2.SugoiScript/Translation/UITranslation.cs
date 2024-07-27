using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace COM3D2.ScriptTranslationTool
{
    internal static class UITranslation
    {
        internal static void Process()
        {
            Tools.MakeFolder(Program.i18NExUiFolder);
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

                    var currentLine = csvLines[i];

                    //Some entries may be empty...
                    if (String.IsNullOrWhiteSpace(currentLine.Japanese))
                        continue;

                    Console.Write(currentLine.Japanese);
                    Tools.Write(" => ", ConsoleColor.Yellow);

                    // recover translation from caches
                    // if (string.IsNullOrEmpty(currentLine.English))
                    //    Cache.Get(currentLine);

                    //Translate if needed/possible
                    if (Program.isSugoiRunning && (string.IsNullOrEmpty(currentLine.English) || (Program.forcedTranslation && string.IsNullOrEmpty(currentLine.MachineTranslation))))
                    {
                        currentLine.GetTranslation();
                        //ignore faulty returns
                        if (currentLine.HasRepeat || currentLine.HasError)
                        {
                            Cache.AddToError(currentLine);
                            Tools.WriteLine($"This line returned a faulty translation and was placed in {Program.errorFile}", ConsoleColor.Red);
                            continue;
                        }  
                    }
                    else if (string.IsNullOrEmpty(currentLine.English))
                    {
                        Tools.WriteLine($"This line wasn't found in any cache and can't be translated since sugoi isn't running", ConsoleColor.Red);
                        continue;
                    }

                    Tools.WriteLine(currentLine.English, currentLine.Color);

                    csvLines[i] = currentLine;
                }

                //Now that it's translated, let's rebuild the .csv

                //Ignore empty files
                if (csvLines.Count == 0) continue;

                //Get the new pathcreate folders and write the header
                string newPath = csv.Replace("UI\\Japanese", Program.i18NExUiFolder);
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
