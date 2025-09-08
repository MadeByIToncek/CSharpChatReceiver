namespace CSharpChatReceiver
{
    public sealed class ChatItemDelete
    {
        internal string targetId;
        internal string message;
        public string GetTargetId()
        {
            return targetId;
        }

        public string GetMessage()
        {
            return message;
        }
    }
}