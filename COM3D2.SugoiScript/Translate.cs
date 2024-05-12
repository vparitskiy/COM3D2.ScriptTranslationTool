using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace COM3D2.ScriptTranslationTool
{
    internal class Translate
    {

        private static readonly HttpClient client = new HttpClient();


        internal static ILine ToEnglish(ILine line)
        {
            line.English = TranslateAsync(line.JapanesePrep).Result;

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
