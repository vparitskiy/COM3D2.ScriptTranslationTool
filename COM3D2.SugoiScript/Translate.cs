using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace COM3D2.ScriptTranslationTool
{
    internal class Translate
    {

        private static readonly HttpClient client = new HttpClient();


        internal static Line ToJapanese(Line line)
        {
            line.English = TranslateAsync(line.JapanesePrep).Result;

            line = CleanPost(line);

            return line;
        }
        

        /// <summary>
        /// Clean all kind of reccurrent syntax error and put back any [HF] tag when needed
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private static Line CleanPost(Line line)
        {
            // replace MUKU by the corresponding [HF] tags
            if (line.HasTag)
            {
                //line.English = Regex.Replace(line.English, @"the tag placeholder", "TAGPLACEHOLDER", RegexOptions.IgnoreCase);

                Regex rx = new Regex(@"\bMUKU\b", RegexOptions.IgnoreCase);

                for (int i = 0; i < line.Tags.Count; i++)
                {
                    line.English = rx.Replace(line.English, line.Tags[i], 1);
                }

                // remove the from before [HF] tags
                line.English = line.English.Replace("the [", "[");
                line.English = line.English.Replace("The [", "[");
            }

            // unk ? Looks like symbols the translator doesn't know how to handle
            line.English = line.English.Replace("<unk>", "");


            // check for repeating characters
            Match matchChar = Regex.Match(line.English, @"(\w)\1{15,}");
            if (matchChar.Success)
            {
                line.HasRepeat = true;
            }

            // check for repating words
            Match matchWord = Regex.Match(line.English, @"(?<word>\w+)(-(\k<word>)){5,}");
            if (matchWord.Success)
            {
                line.HasRepeat = true;
            }

            // check for server bad request
            if (line.English.Contains("400 Bad Request"))
            {
                line.HasError = true;
            }

            return line;
        }

        /// <summary>
        /// Translate  a line using Sugoi Translator
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static async Task<string> TranslateAsync(string str)
        {
            string json = $"{{\"content\":\"{str}\",\"message\":\"translate sentences\"}}";

            //string json = GetJson(str);

            var response = await client.PostAsync(
                "http://127.0.0.1:14366/",
                new StringContent(json, Encoding.UTF8, "application/json"));

            string responseString = await response.Content.ReadAsStringAsync();

            string parsedString = Regex.Unescape(responseString).Trim('"');

            return parsedString;
        }

        /*
        private static string GetJson(string str)
        {
            TranslationRequest translationRequest = new TranslationRequest
            {
                Content = str,
                Message = "translate sentences"
            };

            string json = JsonSerializer.Serialize(translationRequest);
            Console.WriteLine(json);

            return json;
        }
        */


        public class TranslationRequest
        {
            public string Content { get; set; }
            public string Message { get; set; }
            public TranslationRequest(string content, string message)
            {
                Content = content;
                Message = message;
            }
        }
    }
}
