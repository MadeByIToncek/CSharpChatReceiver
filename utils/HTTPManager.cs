using System.Net.Http.Json;
using Newtonsoft.Json.Linq;

namespace CSharpChatReceiver.utils;

public class HttpManager {
    // HttpClient lifecycle management best practices:
    // https://learn.microsoft.com/dotnet/fundamentals/networking/http/httpclient-guidelines#recommended-use
    private static readonly HttpClient SharedClient = new()
    {
        BaseAddress = new Uri("https://jsonplaceholder.typicode.com"),
    };
    public static string GetHttpString(string url) {
        return SharedClient.GetStringAsync(url).Result;
    }

    public static JObject PostHttpjsonObject(string url, object body) {
        JsonContent content = JsonContent.Create(body);
        HttpResponseMessage res = SharedClient.PostAsync(url, content).Result;
        var respb = res.Content.ReadAsStringAsync().Result;
        return JObject.Parse(respb);
    }
}