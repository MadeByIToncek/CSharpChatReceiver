namespace CSharpChatReceiver
{
    public sealed class LiveBroadcastDetails
    {
        public bool isLiveNow;
        public string startTimestamp;
        public string endTimestamp;
        public bool GetLiveNow()
        {
            return isLiveNow;
        }

        public string GetStartTimestamp()
        {
            return startTimestamp;
        }

        public string GetEndTimestamp()
        {
            return endTimestamp;
        }

        public string ToString()
        {
            return "LiveBroadcastDetails{" + "isLiveNow=" + isLiveNow + ", startTimestamp='" + startTimestamp + '\'' + ", endTimestamp='" + endTimestamp + '\'' + '}';
        }
    }
}