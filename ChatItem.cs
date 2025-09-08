using System.Collections.Generic;
using System.IO;

namespace CSharpChatReceiver
{
    public class ChatItem
    {
        protected internal ChatItemType type;
        protected internal string? authorName;
        protected internal string? authorChannelID;
        protected internal string? message;
        protected internal List<object>? messageExtended;
        protected internal string? authorIconURL;
        protected internal string? id;
        protected internal long timestamp;
        protected internal List<AuthorType> authorType;
        protected internal string? memberBadgeIconURL;
        //For paid message
        protected internal int bodyBackgroundColor;
        protected internal int bodyTextColor;
        protected internal int headerBackgroundColor;
        protected internal int headerTextColor;
        protected internal int authorNameTextColor;
        protected internal string? purchaseAmount;
        //For paid sticker
        protected internal string? stickerIconURL;
        protected internal int backgroundColor;
        //For ticker paid message
        protected internal int endBackgroundColor;
        protected internal int durationSec;
        protected internal int fullDurationSec;
        //If moderator enabled
        protected internal string? contextMenuParams;
        protected internal string? pinToTopParams;
        protected internal string? chatDeleteParams; // can be executed by author too
        protected internal string? timeBanParams;
        protected internal string? userBanParams;

        public string? userUnbanParams;
        //Connected chat
        private YouTubeLiveChat liveChat;
        /// <summary>
        /// </summary>
        /// <remarks>@deprecated{@link #ChatItem(YouTubeLiveChat liveChat)}</remarks>
        protected internal ChatItem() : this(null)
        {
        }

        protected internal ChatItem(YouTubeLiveChat liveChat)
        {
            this.authorType = new List<AuthorType>();
            this.authorType.Add(AuthorType.NORMAL);
            this.type = ChatItemType.MESSAGE;
            this.liveChat = liveChat;
        }

        /// <summary>
        /// Get type of this item.
        /// </summary>
        /// <returns>Type of this item</returns>
        public virtual ChatItemType GetType()
        {
            return this.type;
        }

        /// <summary>
        /// Get author name.
        /// </summary>
        /// <returns>Author name</returns>
        public virtual string? GetAuthorName()
        {
            return this.authorName;
        }

        /// <summary>
        /// Get author's channel id.
        /// </summary>
        /// <returns>Author's channel id</returns>
        public virtual string? GetAuthorChannelID()
        {
            return this.authorChannelID;
        }

        /// <summary>
        /// Get message in String
        /// </summary>
        /// <returns>Message</returns>
        public virtual string? GetMessage()
        {
            return this.message;
        }

        /// <summary>
        /// Get list of extended messages.
        /// This list contains Text or Emoji.
        /// </summary>
        /// <returns>List of extended messages</returns>
        public virtual List<object>? GetMessageExtended()
        {
            return this.messageExtended;
        }

        /// <summary>
        /// Get author's icon url.
        /// </summary>
        /// <returns>Author's icon url</returns>
        public virtual string? GetAuthorIconURL()
        {
            return this.authorIconURL;
        }

        /// <summary>
        /// Get id.
        /// </summary>
        /// <returns>Id</returns>
        public virtual string? GetId()
        {
            return this.id;
        }

        /// <summary>
        /// Get timestamp of this item.
        /// </summary>
        /// <returns>Timestamp in UNIX time</returns>
        public virtual long GetTimestamp()
        {
            return this.timestamp;
        }

        /// <summary>
        /// Get author types in List.
        /// </summary>
        /// <returns>List of AuthorType</returns>
        public virtual List<AuthorType> GetAuthorType()
        {
            return this.authorType;
        }

        /// <summary>
        /// Is this message's author verified?
        /// </summary>
        /// <returns>If this message's author is verified, returns true.</returns>
        public virtual bool IsAuthorVerified()
        {
            return this.authorType.Contains(AuthorType.VERIFIED);
        }

        /// <summary>
        /// Is this message's author owner?
        /// </summary>
        /// <returns>If this message's author is owner, returns true.</returns>
        public virtual bool IsAuthorOwner()
        {
            return this.authorType.Contains(AuthorType.OWNER);
        }

        /// <summary>
        /// Is this message's author moderator?
        /// </summary>
        /// <returns>If this message's author is moderator, returns true.</returns>
        public virtual bool IsAuthorModerator()
        {
            return this.authorType.Contains(AuthorType.MODERATOR);
        }

        /// <summary>
        /// Is this message's author member?
        /// </summary>
        /// <returns>If this message's author is member, returns true.</returns>
        public virtual bool IsAuthorMember()
        {
            return this.authorType.Contains(AuthorType.MEMBER);
        }

        /// <summary>
        /// Get member badge icon url.
        /// You can use if isAuthorMember() == true
        /// </summary>
        /// <returns>Member badge icon url</returns>
        public virtual string? GetMemberBadgeIconURL()
        {
            return this.memberBadgeIconURL;
        }

        /// <summary>
        /// Get background color of body in int.
        /// </summary>
        /// <returns>Color in int</returns>
        public virtual int GetBodyBackgroundColor()
        {
            return this.bodyBackgroundColor;
        }

        /// <summary>
        /// Get text color of background in int.
        /// </summary>
        /// <returns>Color in int</returns>
        public virtual int GetBodyTextColor()
        {
            return this.bodyTextColor;
        }

        /// <summary>
        /// Get header background color in int.
        /// </summary>
        /// <returns>Color in int</returns>
        public virtual int GetHeaderBackgroundColor()
        {
            return this.headerBackgroundColor;
        }

        /// <summary>
        /// Get header color in int.
        /// </summary>
        /// <returns>Color in int</returns>
        public virtual int GetHeaderTextColor()
        {
            return this.headerTextColor;
        }

        /// <summary>
        /// Get purchase amount of this paid message
        /// </summary>
        /// <returns>Amount of money(example ￥100)</returns>
        public virtual string? GetPurchaseAmount()
        {
            return this.purchaseAmount;
        }

        /// <summary>
        /// Get text color of drawing author name in int.
        /// </summary>
        /// <returns>Color in int</returns>
        public virtual int GetAuthorNameTextColor()
        {
            return this.authorNameTextColor;
        }

        public virtual int GetEndBackgroundColor()
        {
            return this.endBackgroundColor;
        }

        /// <summary>
        /// Get elapsed time from starting viewing this paid message in seconds.
        /// You can use if getType() == PAID_MESSAGE
        /// </summary>
        /// <returns>Elapsed time from starting viewing this paid message in seconds</returns>
        public virtual int GetDurationSec()
        {
            return this.durationSec;
        }

        /// <summary>
        /// Get full duration of paid message viewing in seconds.
        /// You can use if getType() == PAID_MESSAGE
        /// </summary>
        /// <returns>Full duration of paid message viewing in seconds</returns>
        public virtual int GetFullDurationSec()
        {
            return this.fullDurationSec;
        }

        /// <summary>
        /// Get sticker icon url.
        /// You can use if getType() == PAID_STICKER
        /// </summary>
        /// <returns>Sticker icon url</returns>
        public virtual string? GetStickerIconURL()
        {
            return stickerIconURL;
        }

        /// <summary>
        /// Get background color in int.
        /// You can use if getType() == PAID_STICKER
        /// </summary>
        /// <returns>Background color in int</returns>
        public virtual int GetBackgroundColor()
        {
            return backgroundColor;
        }

        public virtual string ToString()
        {
            return "ChatItem{" + "type=" + type + ", authorName='" + authorName + '\'' + ", authorChannelID='" + authorChannelID + '\'' + ", message='" + message + '\'' + ", messageExtended=" + messageExtended + ", iconURL='" + authorIconURL + '\'' + ", id='" + id + '\'' + ", timestamp=" + timestamp + ", authorType=" + authorType + ", memberBadgeIconURL='" + memberBadgeIconURL + '\'' + ", bodyBackgroundColor=" + bodyBackgroundColor + ", bodyTextColor=" + bodyTextColor + ", headerBackgroundColor=" + headerBackgroundColor + ", headerTextColor=" + headerTextColor + ", authorNameTextColor=" + authorNameTextColor + ", purchaseAmount='" + purchaseAmount + '\'' + ", stickerIconURL='" + stickerIconURL + '\'' + ", backgroundColor=" + backgroundColor + ", endBackgroundColor=" + endBackgroundColor + ", durationSec=" + durationSec + ", fullDurationSec=" + fullDurationSec + '}';
        }

        /// <summary>
        /// Delete this chat.
        /// You need to set user data using setUserData() before calling this method.
        /// User must be either author of chat, moderator or owner.
        /// </summary>
        /// <exception cref="IOException">Http request error</exception>
        /// <exception cref="IllegalStateException">The IDs are not set or permission denied error</exception>
        public virtual void Delete()
        {
            liveChat.DeleteMessage(this);
        }

        /// <summary>
        /// Ban chat author for 300 seconds (+ delete chat).
        /// You need to set user data using setUserData() before calling this method.
        /// User must be either moderator or owner.
        /// </summary>
        /// <exception cref="IOException">Http request error</exception>
        /// <exception cref="IllegalStateException">The IDs are not set or permission denied error</exception>
        public virtual void TimeoutAuthor()
        {
            liveChat.BanAuthorTemporarily(this);
        }

        /// <summary>
        /// Ban chat author permanently from the channel (+ delete chat).
        /// You need to set user data using setUserData() before calling this method.
        /// User must be either moderator or owner.
        /// <br>
        /// <b>**Use with cautions!!**</b> It is recommended to store these banned ChatItem so you can unban later.
        /// </summary>
        /// <exception cref="IOException">Http request error</exception>
        /// <exception cref="IllegalStateException">The IDs are not set or permission denied error</exception>
        public virtual void BanAuthor()
        {
            liveChat.BanUserPermanently(this);
        }

        /// <summary>
        /// Unban chat author who was permanently banned from the channel (deleted chat won't be recovered).
        /// You need to set user data using setUserData() before calling this method.
        /// User must be either moderator or owner.
        /// </summary>
        /// <exception cref="IOException">Http request error</exception>
        /// <exception cref="IllegalStateException">The IDs are not set or permission denied error</exception>
        public virtual void UnbanAuthor()
        {
            liveChat.UnbanUser(this);
        }

        /// <summary>
        /// Pin this chat as banner.
        /// You need to set user data using setUserData() before calling this method.
        /// User must be either moderator or owner.
        /// </summary>
        /// <exception cref="IOException">Http request error</exception>
        /// <exception cref="IllegalStateException">The IDs are not set or permission denied error</exception>
        public virtual void PinAsBanner()
        {
            liveChat.PinMessage(this);
        }
    }
}