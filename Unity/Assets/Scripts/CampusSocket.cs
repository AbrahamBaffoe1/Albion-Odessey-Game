using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace AlbionOdyssey
{
    // Network work stays off Unity's main thread; messages and outcomes return via queues.
    public sealed class CampusSocket : IDisposable
    {
        readonly CancellationTokenSource cancel=new CancellationTokenSource();
        readonly ClientWebSocket socket=new ClientWebSocket();
        readonly SemaphoreSlim writer=new SemaphoreSlim(1,1);
        public readonly ConcurrentQueue<string> Incoming=new ConcurrentQueue<string>();
        public volatile bool Connected,Ended;
        public int CloseCode {get;private set;}
        public async void Connect(string url,string authentication)
        {
            try
            {
                socket.Options.KeepAliveInterval=TimeSpan.FromSeconds(10);
                using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancel.Token))
                {timeout.CancelAfter(45000);await socket.ConnectAsync(new Uri(url),timeout.Token).ConfigureAwait(false);}
                Connected=true;await Write(authentication).ConfigureAwait(false);
                byte[] bytes=new byte[32768];
                while(!cancel.IsCancellationRequested&&socket.State==WebSocketState.Open)
                {
                    var text=new StringBuilder();WebSocketReceiveResult part;
                    do
                    {
                        part=await socket.ReceiveAsync(new ArraySegment<byte>(bytes),cancel.Token).ConfigureAwait(false);
                        if(part.MessageType==WebSocketMessageType.Close){CloseCode=(int)(part.CloseStatus??WebSocketCloseStatus.Empty);return;}
                        if(part.MessageType!=WebSocketMessageType.Text)throw new InvalidOperationException();
                        text.Append(Encoding.UTF8.GetString(bytes,0,part.Count));if(text.Length>65536)throw new InvalidOperationException();
                    }while(!part.EndOfMessage);
                    if(Incoming.Count>32)throw new InvalidOperationException();Incoming.Enqueue(text.ToString());
                }
            }
            catch(OperationCanceledException){}catch{CloseCode=CloseCode==0?1006:CloseCode;}
            finally{Connected=false;Ended=true;}
        }
        async Task Write(string value)
        {
            await writer.WaitAsync(cancel.Token).ConfigureAwait(false);
            try{var bytes=Encoding.UTF8.GetBytes(value);await socket.SendAsync(new ArraySegment<byte>(bytes),WebSocketMessageType.Text,true,cancel.Token).ConfigureAwait(false);}
            finally{writer.Release();}
        }
        public async void Send(string value)
        {
            if(!Connected||Ended||writer.CurrentCount==0)return;
            try{await Write(value).ConfigureAwait(false);}catch{Connected=false;Ended=true;}
        }
        public void Dispose(){if(!cancel.IsCancellationRequested)cancel.Cancel();socket.Abort();socket.Dispose();Connected=false;Ended=true;}
    }
}
