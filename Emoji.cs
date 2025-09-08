using System.Collections.Generic;

namespace CSharpChatReceiver
{
    public class Emoji
    {
        protected internal string emojiId;
        protected internal List<string> shortcuts;
        protected internal List<string> searchTerms;
        protected internal string iconURL;
        protected internal bool isCustomEmoji;
        public virtual string GetEmojiId()
        {
            return emojiId;
        }

        public virtual List<string> GetShortcuts()
        {
            return shortcuts;
        }

        public virtual List<string> GetSearchTerms()
        {
            return searchTerms;
        }

        public virtual string GetIconURL()
        {
            return iconURL;
        }

        public virtual bool IsCustomEmoji()
        {
            return isCustomEmoji;
        }

        public virtual string ToString()
        {
            return "Emoji{" + "emojiId='" + emojiId + '\'' + ", shortcuts=" + shortcuts + ", searchTerms=" + searchTerms + ", iconURL='" + iconURL + '\'' + ", isCustomEmoji=" + isCustomEmoji + '}';
        }
    }
}