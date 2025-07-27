namespace CSharpChatReceiver.records;

public record ChatItem(string Id,
    ChatItem.Author ChatAuthor,
    string Message,
    DateTime Timestamp) {
    public record Author(string Name,
        ImageItem? Thumbnail,
        string ChannelId,
        Badge? Badge,
        bool IsVerified,
        bool IsOwner,
        bool IsModerator) {
    }
    
    public record Badge(ImageItem Thumbnail,
        string Label) { }

    public record ImageItem (string Url,
        string Alt){ }
}