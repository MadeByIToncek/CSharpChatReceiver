namespace CSharpChatReceiver.records;

public record ChatData(List<ChatItem> ChatItems, string? Continuation) {
    
}