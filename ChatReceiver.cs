using System.Timers;
using CSharpChatReceiver.records;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace CSharpChatReceiver;

public class ChatReceiver {
    private LivePageData? _data;
    private string _liveId;
    
    public event EventHandler? StartupEvent;
    public event EventHandler? ShutdownEvent;
    public event EventHandler? MessageEvent;
    public event EventHandler? ErrorEvent;

    public ChatReceiver(string liveId, long pollingInterval = 1000) {
        this._liveId = liveId;
    }

    public void Execute() {
        if (_data == null) {
            OnErrorEvent("Not found any options!");
            Stop();
            return;
        }

        ChatData chat = Requests.FetchChat(data: _data);
        foreach (var item in chat.ChatItems) {
            OnMessageEvent(item);
        }

        if(chat.Continuation != null) _data = _data with { Continuation = chat.Continuation };
    }

    public void Start() {
        _data = Requests.FetchLivePage(_liveId);
        _liveId = _data.LiveId;
            
        OnStartupEvent();
    }

    public void Stop() {
        OnShutdownEvent();
    }

    protected virtual void OnStartupEvent() {
        StartupEvent?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnShutdownEvent() {
        ShutdownEvent?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnMessageEvent(ChatItem item) {
        MessageEvent?.Invoke(this, new MessageEventArgs(item));
    }

    protected virtual void OnErrorEvent(string msg) {
        ErrorEvent?.Invoke(this, new ErrorEventArgs(msg));
    }

    
}
public class ErrorEventArgs : EventArgs {
    public ErrorEventArgs(string msg) {
        Msg = msg;
    }
    public string Msg { get; init; }
    public void Deconstruct(out string msg) {
        msg = this.Msg;
    }
}
public class MessageEventArgs : EventArgs {
    public MessageEventArgs(ChatItem item) {
        Item = item;
    }
    public ChatItem Item { get; init; }
    public void Deconstruct(out ChatItem item) {
        item = this.Item;
    }
}