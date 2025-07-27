using CSharpChatReceiver.records;
using CSharpChatReceiver.utils;
using Newtonsoft.Json.Linq;

namespace CSharpChatReceiver;

public class Requests {
    
    public static LivePageData FetchLivePage(String id) {
        return Parser.GetLivePageData(HttpManager.GetHttpString("https://www.youtube.com/watch?v=" + id));
    }
    public static ChatData FetchChat(LivePageData data) {
        //System.out.println(data);
        String url = "https://www.youtube.com/youtubei/v1/live_chat/get_live_chat?key=" + data.ApiKey;
        var body = new {
            context = new {
                client = new {
                    clientVersion = data.ClientVersion,
                    clientName = "WEB"
                }
            },
            continuation = data.Continuation
        };
        JObject res = HttpManager.PostHttpjsonObject(url, body);
        //System.out.println(res.toString(4));
        return Parser.ParseChatData(res);
    }
}