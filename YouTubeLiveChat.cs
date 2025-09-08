using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CSharpChatReceiver
{
    public class YouTubeLiveChat
    {
        /// <summary>
        /// This is user agent used by YouTubeLiveChat.
        /// You can edit this.
        /// </summary>
        public static string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/98.0.4758.102 Safari/537.36,gzip(gfe)";
        private static readonly string liveChatApi = "https://www.youtube.com/youtubei/v1/live_chat/get_live_chat?key="; // view live chat
        private static readonly string liveChatReplayApi = "https://www.youtube.com/youtubei/v1/live_chat/get_live_chat_replay?key="; // view chat replay
        private static readonly string liveChatSendMessageApi = "https://www.youtube.com/youtubei/v1/live_chat/send_message?key="; // send chat
        private static readonly string liveChatContextMenuApi = "https://www.youtube.com/youtubei/v1/live_chat/get_item_context_menu?key="; // get chat item menu
        private static readonly string liveChatModerateApi = "https://studio.youtube.com/youtubei/v1/live_chat/moderate?key="; // moderation (delete, ban, unban)
        private static readonly string liveChatActionApi = "https://studio.youtube.com/youtubei/v1/live_chat/live_chat_action?key="; // tools (pin)
        private static readonly string liveStreamInfoApi = "https://www.youtube.com/watch?v="; // stream info
        private string videoId;
        private string channelId;
        private string? continuation;
        private bool isReplay;
        private readonly bool isTopChatOnly;
        private string? visitorData;
        private ChatItem bannerItem;
        private readonly List<ChatItem> chatItems;
        private readonly List<ChatItem> chatItemTickerPaidMessages;
        private readonly List<ChatItemDelete> chatItemDeletes;
        private string? clientVersion;
        private bool isInitDataAvailable;
        private string apiKey;
        private string datasyncId;
        private int commentCounter;
        private string clientMessageId;
        private string _params;
        private Dictionary<string, string> cookie;
        private SHA1 sha1;
        /// <summary>
        /// Initialize YouTubeLiveChat
        /// </summary>
        /// <param name="id">Id used in YouTube</param>
        /// <param name="isTopChatOnly">Is this top chat only mode</param>
        /// <param name="type">The type of id (VIDEO or CHANNEL)</param>
        /// <exception cref="IOException">Http request error</exception>
        /// <exception cref="IllegalArgumentException">Video id is incorrect</exception>
        public YouTubeLiveChat(string id, bool isTopChatOnly = true, IdType type = IdType.VIDEO)
        {
            this.isTopChatOnly = isTopChatOnly;
            this.visitorData = "";
            this.chatItems = new List<ChatItem>();
            this.chatItemTickerPaidMessages = new List<ChatItem>();
            this.chatItemDeletes = new List<ChatItemDelete>();
            this.commentCounter = 0;
            this.clientMessageId = Util.GenerateClientMessageId();
            try
            {
                this.GetInitialData(id, type);
            }
            catch (IOException exception)
            {
                throw new IOException(exception.Message, exception);
            }

            if (this.continuation == null)
            {
                throw new ArgumentException("Invalid " + type.ToString().ToLower() + " id:" + id);
            }
        }

        /// <summary>
        /// Reset this. If you have an error, try to call this.
        /// You don't need to call setLocale() again.
        /// </summary>
        /// <exception cref="IOException">Http request error</exception>
        public virtual void Reset()
        {
            this.visitorData = "";
            this.chatItems.Clear();
            this.chatItemTickerPaidMessages.Clear();
            this.chatItemDeletes.Clear();
            this.commentCounter = 0;
            this.clientMessageId = Util.GenerateClientMessageId();
            try
            {
                this.GetInitialData(this.videoId, IdType.VIDEO);
            }
            catch (IOException exception)
            {
                throw new IOException(exception.Message,exception);
            }
        }

        /// <summary>
        /// Update chat data
        /// </summary>
        /// <exception cref="IOException">Http request error</exception>
        public virtual void Update()
        {
            this.Update(0);
        }

        /// <summary>
        /// Update chat data with offset
        /// </summary>
        /// <param name="offsetInMs">Offset in milliseconds</param>
        /// <exception cref="IOException">Http request error</exception>
        public virtual void Update(long offsetInMs)
        {
            if (this.isInitDataAvailable)
            {
                this.isInitDataAvailable = false;
                return;
            }

            this.chatItems.Clear();
            this.chatItemTickerPaidMessages.Clear();
            this.chatItemDeletes.Clear();
            try
            {

                //Get live actions
                if (this.continuation == null)
                {
                    throw new IOException("continuation is null! Please call reset().");
                }

                string pageContent = Util.GetPageContentWithJson((this.isReplay ? liveChatReplayApi : liveChatApi) + this.apiKey, this.GetPayload(offsetInMs), this.GetHeader());
                Dictionary<string, object> json = Util.ToJSON(pageContent);
                if (string.IsNullOrEmpty(this.visitorData))
                {
                    this.visitorData = Util.GetJSONValueString(Util.GetJSONMap(json, "responseContext"), "visitorData");
                }


                //Get clientVersion
                IList<object> serviceTrackingParams = Util.GetJSONList(json, "serviceTrackingParams", "responseContext");
                if (serviceTrackingParams != null)
                {
                    foreach (object ser in serviceTrackingParams)
                    {
                        Dictionary<string, object> service = (Dictionary<string, object>)ser;
                        string? serviceName = Util.GetJSONValueString(service, "service");
                        if (serviceName != null && serviceName.Equals("CSI"))
                        {
                            IList<object> @params = Util.GetJSONList(service, "params");
                            if (@params != null)
                            {
                                foreach (object par in @params)
                                {
                                    Dictionary<string, object> param = (Dictionary<string, object>)par;
                                    string? key = Util.GetJSONValueString(param, "key");
                                    if (key != null && key.Equals("cver"))
                                    {
                                        this.clientVersion = Util.GetJSONValueString(param, "value");
                                    }
                                }
                            }
                        }
                    }
                }


                //Parse actions and update continuation
                Dictionary<string, object> liveChatContinuation = Util.GetJSONMap(json, "continuationContents", "liveChatContinuation");
                if (this.isReplay)
                {
                    if (liveChatContinuation != null)
                    {
                        IList<object> actions = Util.GetJSONList(liveChatContinuation, "actions");
                        if (actions != null)
                        {
                            this.ParseActions(actions);
                        }
                    }

                    IList<object> continuations = Util.GetJSONList(liveChatContinuation, "continuations");

                    //Update continuation
                    if (continuations != null)
                    {
                        foreach (object co in continuations)
                        {
                            Dictionary<string, object> continuation = (Dictionary<string, object>)co;
                            string? value = Util.GetJSONValueString(Util.GetJSONMap(continuation, "liveChatReplayContinuationData"), "continuation");
                            if (value != null)
                            {
                                this.continuation = value;
                            }
                        }
                    }
                }
                else
                {
                    if (liveChatContinuation != null)
                    {
                        IList<object> actions = Util.GetJSONList(liveChatContinuation, "actions");
                        if (actions != null)
                        {
                            this.ParseActions(actions);
                        }

                        IList<object> continuations = Util.GetJSONList(liveChatContinuation, "continuations");
                        if (continuations != null)
                        {
                            foreach (object co in continuations)
                            {
                                Dictionary<string, object> continuation = (Dictionary<string, object>)co;
                                this.continuation = Util.GetJSONValueString(Util.GetJSONMap(continuation, "invalidationContinuationData"), "continuation");
                                if (this.continuation == null)
                                {
                                    this.continuation = Util.GetJSONValueString(Util.GetJSONMap(continuation, "timedContinuationData"), "continuation");
                                }

                                if (this.continuation == null)
                                {
                                    this.continuation = Util.GetJSONValueString(Util.GetJSONMap(continuation, "reloadContinuationData"), "continuation");
                                }
                            }
                        }
                    }
                }
            }
            catch (IOException exception)
            {
                throw new IOException("Can't get youtube live chat!", exception);
            }
        }

        /// <summary>
        /// Send a message to this live chat
        /// You need to set user data using setUserData() before calling this method
        /// </summary>
        /// <param name="message">Chat message to send</param>
        /// <exception cref="IOException">Http request error</exception>
        /// <exception cref="IllegalStateException">The IDs are not set error</exception>
        public virtual void SendMessage(string message)
        {
            if (this.isReplay)
            {
                throw new InvalidOperationException("This live is replay! You can send a message if this live isn't replay.");
            }

            if (this.IsIDsMissing())
            {
                throw new InvalidOperationException("You need to set user data using setUserData()");
            }

            try
            {
                if (this.datasyncId == null)
                {
                    throw new IOException("datasyncId is null! Please call reset() or set user data.");
                }

                if (this._params == null)
                {
                    throw new InvalidOperationException("params is null! You may not set appropriate Cookie. Please call reset().");
                }

                Util.SendHttpRequestWithJson(liveChatSendMessageApi + this.apiKey, this.GetPayloadToSendMessage(message), this.GetHeader());
            }
            catch (IOException exception)
            {
                throw new IOException("Couldn't send a message!", exception);
            }
        }

        public virtual void DeleteMessage(ChatItem chatItem)
        {
            if (this.isReplay)
            {
                throw new InvalidOperationException("This live is replay! You can delete a chat if this live isn't replay.");
            }

            try
            {
                if (this.datasyncId == null)
                {
                    throw new IOException("datasyncId is null! Please call reset() or set user data.");
                }

                if (chatItem.chatDeleteParams == null)
                {
                    if (this.IsIDsMissing())
                    {
                        throw new InvalidOperationException("You need to set user data using setUserData()");
                    }

                    GetContextMenu(chatItem);
                    if (chatItem.chatDeleteParams == null)
                    {
                        throw new InvalidOperationException("chatDeleteParams is null! Check if you have permission or use setUserData() first.");
                    }
                }

                Util.SendHttpRequestWithJson(liveChatModerateApi + this.apiKey, this.GetPayloadClient(chatItem.chatDeleteParams), this.GetHeader());
            }
            catch (IOException exception)
            {
                throw new IOException("Couldn't delete chat!", exception);
            }
        }

        public virtual void BanAuthorTemporarily(ChatItem chatItem)
        {
            if (this.isReplay)
            {
                throw new InvalidOperationException("This live is replay! You can ban a user if this live isn't replay.");
            }

            try
            {
                if (this.datasyncId == null)
                {
                    throw new IOException("datasyncId is null! Please call reset() or set user data.");
                }

                if (chatItem.timeBanParams == null)
                {
                    if (this.IsIDsMissing())
                    {
                        throw new InvalidOperationException("You need to set user data using setUserData()");
                    }

                    GetContextMenu(chatItem);
                    if (chatItem.timeBanParams == null)
                    {
                        throw new InvalidOperationException("timeBanParams is null! Check if you have permission or use setUserData() first.");
                    }
                }

                Util.SendHttpRequestWithJson(liveChatModerateApi + this.apiKey, this.GetPayloadClient(chatItem.timeBanParams), this.GetHeader());
            }
            catch (IOException exception)
            {
                throw new IOException("Couldn't ban user!", exception);
            }
        }

        public virtual void BanUserPermanently(ChatItem chatItem)
        {
            if (this.isReplay)
            {
                throw new InvalidOperationException("This live is replay! You can ban a user if this live isn't replay.");
            }

            if (this.IsIDsMissing())
            {
                throw new InvalidOperationException("You need to set user data using setUserData()");
            }

            try
            {
                if (this.datasyncId == null)
                {
                    throw new IOException("datasyncId is null! Please call reset() or set user data.");
                }

                if (chatItem.userBanParams == null)
                {
                    GetContextMenu(chatItem);
                    if (chatItem.userBanParams == null)
                    {
                        throw new InvalidOperationException("userBanParams is null! Check if you have permission or use setUserData() first.");
                    }
                }

                Util.SendHttpRequestWithJson(liveChatModerateApi + this.apiKey, this.GetPayloadClient(chatItem.userBanParams), this.GetHeader());
            }
            catch (IOException exception)
            {
                throw new IOException("Couldn't ban user!", exception);
            }
        }

        public virtual void UnbanUser(ChatItem chatItem)
        {
            if (this.isReplay)
            {
                throw new InvalidOperationException("This live is replay! You can ban a user if this live isn't replay.");
            }

            if (this.IsIDsMissing())
            {
                throw new InvalidOperationException("You need to set user data using setUserData()");
            }

            try
            {
                if (this.datasyncId == null)
                {
                    throw new IOException("datasyncId is null! Please call reset() or set user data.");
                }

                if (chatItem.userUnbanParams == null)
                {
                    GetContextMenu(chatItem);
                    if (chatItem.userUnbanParams == null)
                    {
                        throw new InvalidOperationException("userUnbanParams is null! Check if you have permission or use setUserData() first.");
                    }
                }

                Util.SendHttpRequestWithJson(liveChatModerateApi + this.apiKey, this.GetPayloadClient(chatItem.userUnbanParams), this.GetHeader());
            }
            catch (IOException exception)
            {
                throw new IOException("Couldn't unban user!", exception);
            }
        }

        public virtual void PinMessage(ChatItem chatItem)
        {
            if (this.isReplay)
            {
                throw new InvalidOperationException("This live is replay! You can pin a chat if this live isn't replay.");
            }

            try
            {
                if (this.datasyncId == null)
                {
                    throw new IOException("datasyncId is null! Please call reset() or set user data.");
                }

                if (chatItem.pinToTopParams == null)
                {
                    if (this.IsIDsMissing())
                    {
                        throw new InvalidOperationException("You need to set user data using setUserData()");
                    }

                    GetContextMenu(chatItem);
                    if (chatItem.pinToTopParams == null)
                    {
                        throw new InvalidOperationException("pinToTopParams is null! Check if you have permission or use setUserData() first.");
                    }
                }

                Util.SendHttpRequestWithJson(liveChatActionApi + this.apiKey, this.GetPayloadClient(chatItem.pinToTopParams), this.GetHeader());
            }
            catch (IOException exception)
            {
                throw new IOException("Couldn't pin chat!", exception);
            }
        }

        /// <summary>
        /// Set user data.
        /// Cookies can be found in Chrome Devtools(F12) 'Network' tab, 'get_live_chat' request.
        /// You need all cookies.
        /// </summary>
        /// <param name="cookie">Cookie</param>
        /// <exception cref="IOException">Http request error</exception>
        public virtual void SetUserData(Dictionary<string, string> cookie)
        {
            this.cookie = cookie;
            this.Reset();
        }

        /// <summary>
        /// Set user data.
        /// Cookies can be found in Chrome Devtools(F12) 'Network' tab, 'get_live_chat' request.
        /// You need all cookies.
        /// </summary>
        /// <param name="cookie">Cookie</param>
        /// <exception cref="IOException">Http request error</exception>
        public virtual void SetUserData(string cookie)
        {
            String[] cookies = cookie.Split(";");
            this.cookie = new Dictionary<string, string>();
            foreach (string c in cookies)
            {
                this.cookie[c[..c.IndexOf("=", StringComparison.Ordinal)].Trim()] =  c[(c.IndexOf("=", StringComparison.Ordinal) + 1)..].Trim();
            }

            this.Reset();
        }

        private void ParseActions(IList<object> json)
        {
            foreach (object i in json)
            {
                Dictionary<string, object> actions = (Dictionary<string, object>)i;
                Dictionary<string, object> addChatItemAction = Util.GetJSONMap(actions, "addChatItemAction");

                //For replay
                if (addChatItemAction == null)
                {
                    Dictionary<string, object> replayChatItemAction = Util.GetJSONMap(actions, "replayChatItemAction");
                    if (replayChatItemAction != null)
                    {
                        IList<object> acts = Util.GetJSONList(replayChatItemAction, "actions");
                        if (acts != null)
                        {
                            ParseActions(acts);
                        }
                    }
                }

                if (addChatItemAction != null)
                {
                    ChatItem chatItem = null;
                    Dictionary<string, object> item = Util.GetJSONMap(addChatItemAction, "item");
                    if (item != null)
                    {
                        chatItem = new ChatItem(this);
                        this.ParseChatItem(chatItem, item);
                    }

                    if (chatItem != null && chatItem.id != null)
                    {
                        this.chatItems.Add(chatItem);
                    }
                }


                //Pinned message
                Dictionary<string, object> contents = Util.GetJSONMap(actions, "addBannerToLiveChatCommand", "bannerRenderer", "liveChatBannerRenderer", "contents");
                if (contents != null)
                {
                    ChatItem chatItem = new ChatItem(this);
                    this.ParseChatItem(chatItem, contents);
                    this.bannerItem = chatItem;
                }

                Dictionary<string, object> markChatItemAsDeletedAction = Util.GetJSONMap(actions, "markChatItemAsDeletedAction");
                if (markChatItemAsDeletedAction != null)
                {
                    ChatItemDelete chatItemDelete = new ChatItemDelete();
                    chatItemDelete.message = this.ParseMessage(Util.GetJSONMap(markChatItemAsDeletedAction, "deletedStateMessage"), new List<object>());
                    chatItemDelete.targetId = Util.GetJSONValueString(markChatItemAsDeletedAction, "targetItemId");
                    this.chatItemDeletes.Add(chatItemDelete);
                }
            }
        }

        private void ParseChatItem(ChatItem chatItem, Dictionary<string, object> action)
        {
            Dictionary<string, object> liveChatTextMessageRenderer = Util.GetJSONMap(action, "liveChatTextMessageRenderer");
            Dictionary<string, object> liveChatPaidMessageRenderer = Util.GetJSONMap(action, "liveChatPaidMessageRenderer");
            Dictionary<string, object> liveChatPaidStickerRenderer = Util.GetJSONMap(action, "liveChatPaidStickerRenderer");
            Dictionary<string, object> liveChatMembershipItemRenderer = Util.GetJSONMap(action, "liveChatMembershipItemRenderer");
            if (liveChatTextMessageRenderer == null && liveChatPaidMessageRenderer != null)
            {
                liveChatTextMessageRenderer = liveChatPaidMessageRenderer;
            }

            if (liveChatTextMessageRenderer == null && liveChatPaidStickerRenderer != null)
            {
                liveChatTextMessageRenderer = liveChatPaidStickerRenderer;
            }

            if (liveChatTextMessageRenderer == null && liveChatMembershipItemRenderer != null)
            {
                liveChatTextMessageRenderer = liveChatMembershipItemRenderer;
            }

            if (liveChatTextMessageRenderer != null)
            {
                chatItem.authorName = Util.GetJSONValueString(Util.GetJSONMap(liveChatTextMessageRenderer, "authorName"), "simpleText");
                chatItem.id = Util.GetJSONValueString(liveChatTextMessageRenderer, "id");
                chatItem.authorChannelID = Util.GetJSONValueString(liveChatTextMessageRenderer, "authorExternalChannelId");
                Dictionary<string, object> message = Util.GetJSONMap(liveChatTextMessageRenderer, "message");
                chatItem.messageExtended = new List<object>();
                chatItem.message = ParseMessage(message, chatItem.messageExtended);
                IList<object> authorPhotoThumbnails = Util.GetJSONList(liveChatTextMessageRenderer, "thumbnails", "authorPhoto");
                if (authorPhotoThumbnails != null)
                {
                    chatItem.authorIconURL = this.GetJSONThumbnailURL(authorPhotoThumbnails);
                }

                string? timestampStr = Util.GetJSONValueString(liveChatTextMessageRenderer, "timestampUsec");
                if (timestampStr != null)
                {
                    chatItem.timestamp = long.Parse(timestampStr);
                }

                IList<object> authorBadges = Util.GetJSONList(liveChatTextMessageRenderer, "authorBadges");
                if (authorBadges != null)
                {
                    foreach (object au in authorBadges)
                    {
                        Dictionary<string, object> authorBadge = (Dictionary<string, object>)au;
                        Dictionary<string, object> liveChatAuthorBadgeRenderer = Util.GetJSONMap(authorBadge, "liveChatAuthorBadgeRenderer");
                        if (liveChatAuthorBadgeRenderer != null)
                        {
                            string? type = Util.GetJSONValueString(Util.GetJSONMap(liveChatAuthorBadgeRenderer, "icon"), "iconType");
                            if (type != null)
                            {
                                switch (type)
                                {
                                    case "VERIFIED":
                                        chatItem.authorType.Add(AuthorType.VERIFIED);
                                        break;
                                    case "OWNER":
                                        chatItem.authorType.Add(AuthorType.OWNER);
                                        break;
                                    case "MODERATOR":
                                        chatItem.authorType.Add(AuthorType.MODERATOR);
                                        break;
                                }
                            }

                            Dictionary<string, object> customThumbnail = Util.GetJSONMap(liveChatAuthorBadgeRenderer, "customThumbnail");
                            if (customThumbnail != null)
                            {
                                chatItem.authorType.Add(AuthorType.MEMBER);
                                IList<object> thumbnails = (IList<object>)customThumbnail["thumbnails"];
                                chatItem.memberBadgeIconURL = this.GetJSONThumbnailURL(thumbnails);
                            }
                        }
                    }
                }


                // Context Menu Params
                string? contextMenuParams = Util.GetJSONValueString(Util.GetJSONMap(liveChatTextMessageRenderer, "contextMenuEndpoint", "liveChatItemContextMenuEndpoint"), "params");
                if (contextMenuParams != null)
                {
                    chatItem.contextMenuParams = contextMenuParams;
                }
            }

            if (action.ContainsKey("liveChatViewerEngagementMessageRenderer"))
            {
                Dictionary<string, object> liveChatViewerEngagementMessageRenderer = (Dictionary<string, object>)action["liveChatViewerEngagementMessageRenderer"];
                chatItem.authorName = "YouTube";
                chatItem.authorChannelID = "user/YouTube";
                chatItem.authorType.Add(AuthorType.YOUTUBE);
                chatItem.id = Util.GetJSONValueString(liveChatViewerEngagementMessageRenderer, "id");
                chatItem.messageExtended = new List<object>();
                chatItem.message = this.ParseMessage(Util.GetJSONMap(liveChatViewerEngagementMessageRenderer, "message"), chatItem.messageExtended);
                string? timestampStr = Util.GetJSONValueString(liveChatViewerEngagementMessageRenderer, "timestampUsec");
                if (timestampStr != null)
                {
                    chatItem.timestamp = long.Parse(timestampStr);
                }
            }

            if (liveChatPaidMessageRenderer != null)
            {
                chatItem.bodyBackgroundColor = Util.GetJSONValueInt(liveChatPaidMessageRenderer, "bodyBackgroundColor");
                chatItem.bodyTextColor = Util.GetJSONValueInt(liveChatPaidMessageRenderer, "bodyBackgroundColor");
                chatItem.headerBackgroundColor = Util.GetJSONValueInt(liveChatPaidMessageRenderer, "bodyBackgroundColor");
                chatItem.headerTextColor = Util.GetJSONValueInt(liveChatPaidMessageRenderer, "bodyBackgroundColor");
                chatItem.authorNameTextColor = Util.GetJSONValueInt(liveChatPaidMessageRenderer, "authorNameTextColor");
                chatItem.purchaseAmount = Util.GetJSONValueString(Util.GetJSONMap(liveChatPaidMessageRenderer, "purchaseAmountText"), "simpleText");
                chatItem.type = ChatItemType.PAID_MESSAGE;
            }

            if (liveChatPaidStickerRenderer != null)
            {
                chatItem.backgroundColor = Util.GetJSONValueInt(liveChatPaidStickerRenderer, "backgroundColor");
                chatItem.purchaseAmount = Util.GetJSONValueString(Util.GetJSONMap(liveChatPaidStickerRenderer, "purchaseAmountText"), "simpleText");
                IList<object> thumbnails = Util.GetJSONList(liveChatPaidStickerRenderer, "thumbnails", "sticker");
                if (thumbnails != null)
                {
                    chatItem.stickerIconURL = this.GetJSONThumbnailURL(thumbnails);
                }

                chatItem.type = ChatItemType.PAID_STICKER;
            }

            Dictionary<string, object> liveChatTickerPaidMessageItemRenderer = Util.GetJSONMap(action, "liveChatTickerPaidMessageItemRenderer");
            if (liveChatTickerPaidMessageItemRenderer != null)
            {
                Dictionary<string, object> renderer = Util.GetJSONMap(liveChatPaidMessageRenderer, "showItemEndpoint", "showLiveChatItemEndpoint", "renderer");
                this.ParseChatItem(chatItem, renderer);
                chatItem.endBackgroundColor = Util.GetJSONValueInt(liveChatPaidMessageRenderer, "endBackgroundColor");
                chatItem.durationSec = Util.GetJSONValueInt(liveChatPaidMessageRenderer, "durationSec");
                chatItem.fullDurationSec = Util.GetJSONValueInt(liveChatPaidMessageRenderer, "fullDurationSec");
                chatItem.type = ChatItemType.TICKER_PAID_MESSAGE;
            }

            if (liveChatMembershipItemRenderer != null)
            {
                chatItem.messageExtended = new List<object>();
                chatItem.message = this.ParseMessage(Util.GetJSONMap(liveChatMembershipItemRenderer, "headerSubtext"), chatItem.messageExtended);
                chatItem.type = ChatItemType.NEW_MEMBER_MESSAGE;
            }
        }

        private string? GetJSONThumbnailURL(IList<object> thumbnails)
        {
            long size = 0;
            string? url = null;
            foreach (object tObj in thumbnails)
            {
                Dictionary<string, object> thumbnail = (Dictionary<string, object>)tObj;
                long width = Util.GetJSONValueLong(thumbnail, "width");
                string? u = Util.GetJSONValueString(thumbnail, "url");
                if (u != null)
                {
                    if (size <= width)
                    {
                        size = width;
                        url = u;
                    }
                }
            }

            return url;
        }

        private string? ParseMessage(Dictionary<string, object> message, IList<object> messageExtended)
        {
            StringBuilder text = new StringBuilder();
            IList<object> runs = Util.GetJSONList(message, "runs");
            if (runs != null)
            {
                text = new StringBuilder();
                foreach (object runObj in runs)
                {
                    Dictionary<string, object> run = (Dictionary<string, object>)runObj;
                    if (run.ContainsKey("text"))
                    {
                        text.Append(run["text"].ToString());
                        messageExtended.Add(new Text(run["text"].ToString()));
                    }

                    Dictionary<string, object> emojiMap = Util.GetJSONMap(run, "emoji");
                    if (emojiMap != null)
                    {
                        Emoji emoji = new Emoji();
                        emoji.emojiId = Util.GetJSONValueString(emojiMap, "emojiId");
                        IList<object> shortcutsList = Util.GetJSONList(emojiMap, "shortcuts");
                        List<string> shortcuts = new();
                        if (shortcutsList != null)
                        {
                            foreach (object s in shortcutsList)
                            {
                                shortcuts.Add(s.ToString());
                            }
                        }

                        emoji.shortcuts = shortcuts;
                        if (shortcuts.Any())
                            text.Append(" ").Append(shortcuts[0]).Append(" ");
                        IList<object> searchTermsList = Util.GetJSONList(emojiMap, "searchTerms");
                        List<string> searchTerms = new ();
                        if (searchTermsList != null)
                        {
                            foreach (object s in searchTermsList)
                            {
                                searchTerms.Add(s.ToString());
                            }
                        }

                        emoji.searchTerms = searchTerms;
                        IList<object> thumbnails = Util.GetJSONList(emojiMap, "thumbnails", "image");
                        if (thumbnails != null)
                        {
                            emoji.iconURL = this.GetJSONThumbnailURL(thumbnails);
                        }

                        emoji.isCustomEmoji = Util.GetJSONValueBoolean(emojiMap, "isCustomEmoji");
                        messageExtended.Add(emoji);
                    }
                }
            }

            return text.Length == 0 ? null : text.ToString();
        }

        /// <summary>
        /// Get video id
        /// </summary>
        /// <returns>Video id</returns>
        public virtual string GetVideoId()
        {
            return this.videoId;
        }

        /// <summary>
        /// Get channel id of this live.
        /// </summary>
        /// <returns>Channel id</returns>
        public virtual string GetChannelId()
        {
            return this.channelId;
        }

        /// <summary>
        /// Check this live replay is replay.
        /// </summary>
        /// <returns>If this live is replay, returns true.</returns>
        public virtual bool IsReplay()
        {
            return this.isReplay;
        }

        /// <summary>
        /// Get pinned message
        /// </summary>
        /// <returns>ChatItem</returns>
        public virtual ChatItem GetBannerItem()
        {
            return this.bannerItem;
        }

        /// <summary>
        /// Get list of ChatItem
        /// </summary>
        /// <returns>List of ChatItem</returns>
        public virtual List<ChatItem> GetChatItems()
        {
            return this.chatItems.ToArray().ToList();
        }

        /// <summary>
        /// Get list of ChatItemDelete
        /// </summary>
        /// <returns>List of ChatItemDelete</returns>
        public virtual List<ChatItemDelete> GetChatItemDeletes()
        {
            return (List<ChatItemDelete>)this.chatItemDeletes.ToArray().ToList();
        }

        /// <summary>
        /// Get list of ChatItem(type=TICKER_PAID_MESSAGE)
        /// </summary>
        /// <returns>List of ChatItem</returns>
        public virtual List<ChatItem> GetChatTickerPaidMessages()
        {
            return (List<ChatItem>)this.chatItemTickerPaidMessages.ToArray().ToList();
        }

        private void GetInitialData(string id, IdType type)
        {
            this.isInitDataAvailable = true;
            {
                string html = "";
                if (type == IdType.VIDEO)
                {
                    this.videoId = id;
                    html = Util.GetPageContent("https://www.youtube.com/watch?v=" + id, GetHeader());
                    string pattern = "\"channelId\":\"([^\"]*)\",\"isOwnerViewing\"";
                    this.channelId = Regex.Match(html, pattern).Groups[1].Value;
                }
                else if (type == IdType.CHANNEL)
                {
                    this.channelId = id;
                    html = Util.GetPageContent("https://www.youtube.com/channel/" + id + "/live", GetHeader());
                    string pattern = "\"updatedMetadataEndpoint\":\\{\"videoId\":\"([^\"]*)";
                    this.channelId = Regex.Match(html, pattern).Groups[1].Value;
                }

                Matcher isReplayMatcher = Pattern.Compile("\"isReplay\":([^,]*)").Matcher(html);
                if (isReplayMatcher.Find())
                {
                    this.isReplay = Boolean.ParseBoolean(isReplayMatcher.Group(1));
                }

                Matcher topOnlyContinuationMatcher = Pattern.Compile("\"selected\":true,\"continuation\":\\{\"reloadContinuationData\":\\{\"continuation\":\"([^\"]*)").Matcher(html);
                if (topOnlyContinuationMatcher.Find())
                {
                    this.continuation = topOnlyContinuationMatcher.Group(1);
                }

                if (!this.isTopChatOnly)
                {
                    Matcher allContinuationMatcher = Pattern.Compile("\"selected\":false,\"continuation\":\\{\"reloadContinuationData\":\\{\"continuation\":\"([^\"]*)").Matcher(html);
                    if (allContinuationMatcher.Find())
                    {
                        this.continuation = allContinuationMatcher.Group(1);
                    }
                }

                Matcher innertubeApiKeyMatcher = Pattern.Compile("\"innertubeApiKey\":\"([^\"]*)\"").Matcher(html);
                if (innertubeApiKeyMatcher.Find())
                {
                    this.apiKey = innertubeApiKeyMatcher.Group(1);
                }

                Matcher datasyncIdMatcher = Pattern.Compile("\"datasyncId\":\"([^|]*)\\|\\|.*\"").Matcher(html);
                if (datasyncIdMatcher.Find())
                {
                    this.datasyncId = datasyncIdMatcher.Group(1);
                }

                if (this.isReplay)
                {
                    html = Util.GetPageContent("https://www.youtube.com/live_chat_replay?continuation=" + this.continuation + "", new HashMap());
                    string initJson = Objects.RequireNonNull(html).Substring(html.IndexOf("window[\"ytInitialData\"] = ") + "window[\"ytInitialData\"] = ".Length());
                    initJson = initJson.Substring(0, initJson.IndexOf(";</script>"));
                    Dictionary<string, object> json = Util.ToJSON(initJson);
                    Dictionary<string, object> timedContinuationData = Util.GetJSONMap(json, "continuationContents", "liveChatContinuation", "continuations", 0, "liveChatReplayContinuationData");
                    if (timedContinuationData != null)
                    {
                        this.continuation = timedContinuationData["continuation"].ToString();
                    }

                    IList<object> actions = Util.GetJSONList(json, "actions", "continuationContents", "liveChatContinuation");
                    if (actions != null)
                    {
                        this.ParseActions(actions);
                    }
                }
                else
                {
                    html = Util.GetPageContent("https://www.youtube.com/live_chat?continuation=" + this.continuation + "", GetHeader());
                    string initJson = Objects.RequireNonNull(html).Substring(html.IndexOf("window[\"ytInitialData\"] = ") + "window[\"ytInitialData\"] = ".Length());
                    initJson = initJson.Substring(0, initJson.IndexOf(";</script>"));
                    Dictionary<string, object> json = Util.ToJSON(initJson);
                    Dictionary<string, object> sendLiveChatMessageEndpoint = Util.GetJSONMap(json, "continuationContents", "liveChatContinuation", "actionPanel", "liveChatMessageInputRenderer", "sendButton", "buttonRenderer", "serviceEndpoint", "sendLiveChatMessageEndpoint");
                    if (sendLiveChatMessageEndpoint != null)
                    {
                        this._params = sendLiveChatMessageEndpoint["params"].ToString();
                    }

                    this.isInitDataAvailable = false;
                }
            }
        }

        private string? GetClientVersion()
        {
            if (this.clientVersion != null)
            {
                return this.clientVersion;
            }

            SimpleDateFormat format = new SimpleDateFormat("yyyyMMdd");
            return "2." + format.Format(new Date(System.CurrentTimeMillis() - (24 * 60 * 1000))) + ".06.00";
        }

        private string GetPayload(long offsetInMs)
        {
            if (offsetInMs < 0)
            {
                offsetInMs = 0;
            }

            Dictionary<string, object> json = new LinkedHashMap();
            Dictionary<string, object> context = new LinkedHashMap();
            Dictionary<string, object> client = new LinkedHashMap();
            json.Put("context", context);
            context.Put("client", client);
            client.Put("visitorData", this.visitorData);
            client.Put("userAgent", userAgent);
            client.Put("clientName", "WEB");
            client.Put("clientVersion", this.GetClientVersion());
            client.Put("gl", this.locale.GetCountry());
            client.Put("hl", this.locale.GetLanguage());
            json.Put("continuation", this.continuation);
            if (this.isReplay)
            {
                LinkedHashMap<string, object> state = new LinkedHashMap();
                state.Put("playerOffsetMs", String.ValueOf(offsetInMs));
                json.Put("currentPlayerState", state);
            }

            return Util.ToJSON(json);
        }

        private string GetPayloadToSendMessage(string message)
        {
            Dictionary<string, object> json = new LinkedHashMap();
            Dictionary<string, object> context = new LinkedHashMap();
            Dictionary<string, object> user = new LinkedHashMap();
            Dictionary<string, object> richMessage = new LinkedHashMap();
            Dictionary<string, object> textSegments = new LinkedHashMap();
            Dictionary<string, object> client = new LinkedHashMap();
            if (this.commentCounter >= Integer.MAX_VALUE - 1)
            {
                this.commentCounter = 0;
            }

            json.Put("clientMessageId", this.clientMessageId + this.commentCounter++);
            json.Put("context", context);
            context.Put("client", client);
            client.Put("clientName", "WEB");
            client.Put("clientVersion", this.GetClientVersion());
            context.Put("user", user);
            user.Put("onBehalfOfUser", datasyncId);
            json.Put("params", this._params);
            json.Put("richMessage", richMessage);
            richMessage.Put("textSegments", textSegments);
            textSegments.Put("text", message);
            return Util.ToJSON(json);
        }

        private string GetPayloadClient(string? @params)
        {
            Dictionary<string, object> json = new LinkedHashMap();
            Dictionary<string, object> context = new LinkedHashMap();
            Dictionary<string, object> user = new LinkedHashMap();
            Dictionary<string, object> client = new LinkedHashMap();
            if (this.commentCounter >= Integer.MAX_VALUE - 1)
            {
                this.commentCounter = 0;
            }

            json.Put("context", context);
            context.Put("client", client);
            client.Put("clientName", "WEB");
            client.Put("clientVersion", this.GetClientVersion());
            context.Put("user", user);
            user.Put("onBehalfOfUser", datasyncId);
            json.Put("params", @params);
            return Util.ToJSON(json);
        }

        private Dictionary<string, string> GetHeader()
        {
            HashMap<string, string> header = new HashMap();
            if (this.IsIDsMissing())
                return header;
            string time = System.CurrentTimeMillis() / 1000 + "";
            string origin = "https://www.youtube.com";

            // Find SAPISID
            string SAPISID = this.cookie != null && this.cookie.ContainsKey("SAPISID") ? this.cookie["SAPISID"] : "";
            string hash = time + " " + SAPISID + " " + origin;
            byte[] sha1_result = this.GetSHA1Engine().Digest(hash.GetBytes());
            header.Put("Authorization", "SAPISIDHASH " + time + "_" + String.Format("%040x", new BigInteger(1, sha1_result)));
            header.Put("X-Origin", origin);
            header.Put("Origin", origin);
            if (this.cookie != null)
            {
                StringBuilder cookie = new StringBuilder();
                foreach (Map.Entry<String, String> c in this.cookie.EntrySet())
                {
                    cookie.Append(c.GetKey()).Append("=").Append(c.GetValue()).Append(";");
                }

                header.Put("Cookie", cookie.ToString());
            }

            return header;
        }

        public virtual void GetContextMenu(ChatItem chatItem)
        {
            try
            {
                string rawJson = Util.GetPageContentWithJson(liveChatContextMenuApi + apiKey + "&params=" + chatItem.contextMenuParams, GetPayloadToSendMessage(""), GetHeader());
                Dictionary<string, object> json = Util.ToJSON(Objects.RequireNonNull(rawJson));
                IList<object> items = Util.GetJSONList(json, "items", "liveChatItemContextMenuSupportedRenderers", "menuRenderer");
                if (items != null)
                {
                    foreach (object obj in items)
                    {
                        Dictionary<string, object> item = (Dictionary<string, object>)obj;
                        Dictionary<string, object> menuServiceItemRenderer = Util.GetJSONMap(item, "menuServiceItemRenderer");
                        if (menuServiceItemRenderer != null)
                        {
                            string? iconType = Util.GetJSONValueString(Util.GetJSONMap(menuServiceItemRenderer, "icon"), "iconType");
                            if (iconType != null)
                            {
                                switch (iconType)
                                {
                                    case "KEEP":
                                        chatItem.pinToTopParams = Util.GetJSONValueString(Util.GetJSONMap(menuServiceItemRenderer, "serviceEndpoint", "liveChatActionEndpoint"), "params");
                                        break;
                                    case "DELETE":
                                        chatItem.chatDeleteParams = Util.GetJSONValueString(Util.GetJSONMap(menuServiceItemRenderer, "serviceEndpoint", "moderateLiveChatEndpoint"), "params");
                                        break;
                                    case "HOURGLASS":
                                        chatItem.timeBanParams = Util.GetJSONValueString(Util.GetJSONMap(menuServiceItemRenderer, "serviceEndpoint", "moderateLiveChatEndpoint"), "params");
                                        break;
                                    case "REMOVE_CIRCLE":
                                        chatItem.userBanParams = Util.GetJSONValueString(Util.GetJSONMap(menuServiceItemRenderer, "serviceEndpoint", "moderateLiveChatEndpoint"), "params");
                                        break;
                                    case "ADD_CIRCLE":
                                        chatItem.userUnbanParams = Util.GetJSONValueString(Util.GetJSONMap(menuServiceItemRenderer, "serviceEndpoint", "moderateLiveChatEndpoint"), "params");
                                        break;
                                    case "FLAG":
                                    case "ADD_MODERATOR":
                                    case "REMOVE_MODERATOR":
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (IOException e)
            {
                e.PrintStackTrace();
            }
        }

        private MessageDigest GetSHA1Engine()
        {
            if (this.sha1 == null)
            {
                try
                {
                    this.sha1 = MessageDigest.GetInstance("SHA-1");
                }
                catch (NoSuchAlgorithmException e)
                {
                    e.PrintStackTrace();
                }
            }

            return this.sha1;
        }

        /// <summary>
        /// Get video id from url
        /// </summary>
        /// <param name="url">Full url(example https://www.youtube.com/watch?v=Aw5b1sa0w)</param>
        /// <returns>Video id</returns>
        /// <exception cref="IllegalArgumentException">URL format is incorrect</exception>
        public static string GetVideoIdFromURL(string url)
        {
            string id = url;
            if (!id.Contains("?") && !id.Contains(".com/") && !id.Contains(".be/") && !id.Contains("/") && !id.Contains("&"))
            {
                return id;
            }

            if (id.Contains("youtube.com/watch?"))
            {
                while (id.Contains("v="))
                {
                    id = id.Substring(id.IndexOf("v=") - 1);
                    if (id.StartsWith("?") || id.StartsWith("&"))
                    {
                        id = id.Substring(3);
                        if (id.Contains("&"))
                        {
                            id = id.Substring(0, id.IndexOf("&"));
                        }

                        if (!id.Contains("?"))
                        {
                            return id;
                        }
                    }
                    else
                    {
                        id = id.Substring(3);
                    }
                }
            }

            if (id.Contains("youtube.com/embed/"))
            {
                id = id.Substring(id.IndexOf("embed/") + 6);
                if (id.Contains("?"))
                {
                    id = id.Substring(0, id.IndexOf("?"));
                }

                return id;
            }

            if (id.Contains("youtu.be/"))
            {
                id = id.Substring(id.IndexOf("youtu.be/") + 9);
                if (id.Contains("?"))
                {
                    id = id.Substring(0, id.IndexOf("?"));
                }

                return id;
            }

            throw new ArgumentException(url);
        }

        /// <summary>
        /// Get channel id from url
        /// </summary>
        /// <param name="url">Full url(example https://www.youtube.com/channel/USWmbkAWEKOG43WAnbw)</param>
        /// <returns>Channel id</returns>
        /// <exception cref="IllegalArgumentException">URL format is incorrect</exception>
        public static string GetChannelIdFromURL(string url)
        {
            string id = url;
            if (!id.Contains("?") && !id.Contains(".com/") && !id.Contains(".be/") && !id.Contains("/") && !id.Contains("&"))
            {
                return id;
            }

            if (id.Contains("youtube.com/"))
            {
                if (!id.Contains("channel/") && (id.StartsWith("http://") || id.StartsWith("https://")))
                {
                    try
                    {
                        string html = Util.GetPageContent(id, new HashMap());
                        Matcher matcher = Pattern.Compile("<meta itemprop=\"identifier\" content=\"([^\"]*)\"").Matcher(html);
                        if (matcher.Find())
                        {
                            return matcher.Group(1);
                        }
                    }
                    catch (IOException ignore)
                    {
                    }
                }

                if (id.Contains("channel/"))
                {
                    id = id.Substring(id.IndexOf("channel/") + 8);
                    if (id.Contains("?"))
                    {
                        id = id.Substring(0, id.IndexOf("?"));
                    }

                    return id;
                }
            }

            throw new ArgumentException(url);
        }

        private bool IsIDsMissing()
        {
            return this.cookie == null;
        }

        /// <summary>
        /// Get broadcast info
        /// </summary>
        /// <returns>LiveBroadcastDetails obj</returns>
        /// <exception cref="IOException">Couldn't get broadcast info</exception>
        public virtual LiveBroadcastDetails GetBroadcastInfo()
        {
            try
            {
                string url = liveStreamInfoApi + this.videoId + "&hl=en&pbj=1";
                HashMap<string, string> header = new HashMap();
                header.Put("x-youtube-client-name", "1");
                header.Put("x-youtube-client-version", GetClientVersion());
                string response = Util.GetPageContent(url, header);
                JsonElement jsonElement = JsonParser.ParseString(Objects.RequireNonNull(response)).GetAsJsonObject();
                JsonElement liveBroadcastDetails = Util.SearchJsonElementByKey("liveBroadcastDetails", jsonElement);
                return gson.FromJson(liveBroadcastDetails, typeof(LiveBroadcastDetails));
            }
            catch (IOException exception)
            {
                throw new IOException("Couldn't get broadcast info!", exception);
            }
            catch (NullReferenceException exception)
            {
                throw new IOException("Couldn't get broadcast info!", exception);
            }
        }
    }
}