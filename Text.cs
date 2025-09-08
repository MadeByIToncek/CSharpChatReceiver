namespace CSharpChatReceiver
{
    public sealed class Text
    {
        private readonly string text;
        public Text(string text)
        {
            this.text = text;
        }

        public string GetText()
        {
            return text;
        }
    }
}