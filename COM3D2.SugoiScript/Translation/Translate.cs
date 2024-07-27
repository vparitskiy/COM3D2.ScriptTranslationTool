using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace COM3D2.ScriptTranslationTool;

internal static class Translate
{
    private const string Address = "http://127.0.0.1:14366/";
    private static readonly HttpClient Client = new();


    internal static void ToEnglish(ILine line)
    {
        line.MachineTranslation = TranslateAsync(line.JapanesePrep).Result;
    }

    internal static string ToEnglish(string text)
    {
        return TranslateAsync(text).Result;
    }


    /// <summary>
    /// Translate  a line using Sugoi Translator
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private static async Task<string> TranslateAsync(string str)
    {
        var json = $"{{\"content\":\"{str}\",\"message\":\"translate sentences\"}}";

        //string json = GetJson(str);

        var response = await Client.PostAsync(
            Address,
            new StringContent(json, Encoding.UTF8, "application/json"));

        var responseString = await response.Content.ReadAsStringAsync();

        var parsedString = Regex.Unescape(responseString).Trim('"');

        return parsedString;
    }
}