using System.Text.RegularExpressions;
using CSharpChatReceiver.records;
using Newtonsoft.Json.Linq;

namespace CSharpChatReceiver;

public class Parser {
    public static LivePageData GetLivePageData(string str) {
        string liveId, apiKey, clientVersion, continuation;

        var pattern = "<link rel=\"canonical\" href=\"https://www.youtube.com/watch\\?v=(.+?)\">";
        Match match = Regex.Match(str, pattern);

        if (match.Success) {
            liveId = match.Groups[1].Value;
        }
        else {
            throw new ParseException("Unable to find liveID!");
        }

        pattern = "['\"]isReplay['\"]:\\s*(true)";
        match = Regex.Match(str, pattern);

        if (match.Success) {
            throw new ParseException("This video is a replay!");
        }

        pattern = "['\"]INNERTUBE_API_KEY['\"]:\\s*['\"](.+?)['\"]";
        match = Regex.Match(str, pattern);

        if (match.Success) {
            apiKey = match.Groups[1].Value;
        }
        else {
            throw new ParseException("Unable to find apiKey!");
        }

        pattern = "['\"]clientVersion['\"]:\\s*['\"]([\\d.]+?)['\"]";
        match = Regex.Match(str, pattern);

        if (match.Success) {
            clientVersion = match.Groups[1].Value;
        }
        else {
            throw new ParseException("Unable to find clientVersion!");
        }

        pattern = "['\"]continuation['\"]:\\s*['\"](.+?)['\"]";
        match = Regex.Match(str, pattern);

        if (match.Success) {
            continuation = match.Groups[1].Value;
        }
        else {
            throw new ParseException("Unable to find continuation!");
        }

        return new LivePageData(liveId, apiKey, clientVersion, continuation);
    }

    public static ChatData ParseChatData(JObject data) {
        List<ChatItem> chatItems = new() { };

        JArray? arr = data.GetValue("continuationContents")?.Value<JObject>()?.GetValue("liveChatContinuation")
            ?.Value<JObject>()?
            .GetValue("actions")?.Value<JArray>();
        if (arr != null) {
            chatItems = arr
                .Select(x => (JObject) x)
                .Select(ParseActionToChatItem)
                .Where(x => x!=null)
                .Cast<ChatItem>()
                .ToList();
        }
        
        JObject? continuationData = data.GetValue("continuationContents")?
            .Value<JObject>()?
            .GetValue("liveChatContinuation")?
            .Value<JObject>()?
            .GetValue("continuations")?
            .Value<JArray>()?[0]
            .Value<JObject>();

        var continuation = "";
        if (continuationData != null) {
            if (continuationData.ContainsKey("invalidationContinuationData")) {
                continuation = continuationData.GetValue("invalidationContinuationData")?.Value<JObject>()?
                    .GetValue("continuation")?.Value<string>();
            } else if (continuationData.ContainsKey("timedContinuationData")) {
                continuation = continuationData.GetValue("timedContinuationData")?.Value<JObject>()?
                    .GetValue("continuation")?.Value<string>();
            }
        };

        return new ChatData(chatItems, continuation);
    }

    private static ChatItem? ParseActionToChatItem(JObject action) {
        // Console.WriteLine(action.ToString());
        JObject? item = action.GetValue("addChatItemAction")?.Value<JObject>()?.GetValue("item")?.Value<JObject>()?.GetValue("liveChatTextMessageRenderer")?.Value<JObject>();
        if(item == null){return null;}
        
        JArray? messages = item.GetValue("message")?.Value<JObject>()?.GetValue("runs")?.Value<JArray>();
        var msg = "";
        if (messages != null) {
            msg = ParseMessages(messages
                .Select(x => (JObject) x)
                .Select(ConvertToString)
                .Where(x => x != null)
                .Cast<string>()
                .ToList());
        }
        
        var authorName = item.ContainsKey("authorName") ? item.GetValue("authorName")?.Value<JObject>()?.GetValue("simpleText")?.Value<string>() == null? "<unable to parse>": item.GetValue("authorName")?.Value<JObject>()?.GetValue("simpleText")?.Value<string>() : "<unable to parse>";
        var authorIDnullable = item.GetValue("authorExternalChannelId")?.Value<string>();
        var authorId = authorIDnullable ?? "<unable to parse>";

        ChatItem.ImageItem pfp = ParseThumbnailToImageItem(item.GetValue("authorPhoto")?.Value<JObject>()?.GetValue("thumbnails").Value<JArray>()
            .Select(x => (JObject) x)
            .Select(x => x.GetValue("url")?.Value<string>())
            .Where(x => x != null)
            .Cast<string>()
            .ToList(), authorName ?? "<unable to parse>");

        ChatItem.Badge? badge = null;
        bool isVerified = false, isOwner = false, isModerator = false;
        
        if(item.ContainsKey("authorBadges")) {
            foreach (var o in item.GetValue("authorBadges")
                         .Select(x => (JObject) x)
                         .ToList()) {
                if (o.ContainsKey("customThumbnail")) {
                    badge = new ChatItem.Badge(
                        
                        ParseThumbnailToImageItem(o.GetValue("customThumbnail")?.Value<JObject>()?.GetValue("thumbnails")?.Value<JArray>()
                            .Select(x => (JObject) x)
                            .Select(x=>x.GetValue("url").Value<string>())
                            .Where(x => x != null)
                            .Cast<string>()
                            .ToList(),o.GetValue("tooltip")?.Value<string>() ?? "<unable to parse>"),
                        o.GetValue("tooltip")?.Value<string>() ?? "<unable to parse>"
                    );
                } else if (o.ContainsKey("icon")){
                    switch (o.GetValue("icon")?.Value<JObject>()?.GetValue("iconType")?.Value<string>()) {
                        case "OWNER":
                            isOwner = true;
                            break;
                        case "VERIFIED":
                            isVerified = true;
                            break;
                        case "MODERATOR":
                            isModerator = true;
                            break;
                    }
                }
            }
        }
        ChatItem.Author author = new(authorName ?? "<unable to parse>", pfp, authorId, badge, isVerified, isOwner, isModerator);
        
        return new ChatItem(item.GetValue("id")?.Value<string>() ?? "<undefined?>", author, msg, DateTime.UnixEpoch.AddMilliseconds(item.GetValue("timestampUsec")?.Value<long>()/1000L ?? 1L));
    }

    private static ChatItem.ImageItem? ParseThumbnailToImageItem(List<string>? data, string authorName) {
        if(data == null || data.Count == 0){return null;}
        var url = data[^1];
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute)) {
            return new ChatItem.ImageItem(url, authorName);
        }
        else {
            return null;
        }
    }

    private static string ParseMessages(List<string> toList) {
        var msg = "";
        toList.ForEach(x => msg += x + " ");
        return msg;
    }

    private static string? ConvertToString(JObject o) {
        if (o.ContainsKey("text")) {
            return o.GetValue("text")?.Value<string>();
        }

        if (o.ContainsKey("emoji")) {
            if (o.GetValue("emoji").Contains("isCustomEmoji")) {
                return ":" + o.GetValue("emoji")?.Value<JObject>()?
                    .GetValue("image")?.Value<JObject>()?
                    .GetValue("accessibility")?.Value<JObject>()?
                    .GetValue("accessibilityData")?.Value<JObject>()?
                    .GetValue("label")?.Value<string>() + ":";
            }

            return o.GetValue("emoji")?.Value<JObject>()?
                .GetValue("image")?.Value<JObject>()?
                .GetValue("accessibility")?.Value<JObject>()?
                .GetValue("accessibilityData")?.Value<JObject>()?
                .GetValue("label")?.Value<string>();
        }

        return null;
    }
}

public class ParseException : Exception {
    public ParseException(string s) {
        throw new Exception(s);
    }
}