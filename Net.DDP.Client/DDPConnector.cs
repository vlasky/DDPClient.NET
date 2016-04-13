using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebSocket4Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Net.DDP.Client
{
    internal class DDPConnector
    {
        private WebSocket _socket;
        private string _url = string.Empty;
        private int _isWait = 0;
        private IClient _client;

        public event SocketErrorEventHandler SocketError;

        private bool _keepAlive;

        public DDPConnector(IClient client)
        {
            this._client = client;
        }

        /// <summary>
        /// Creates a new socket connection.
        /// Returns true if a connection was successfully opened to the url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="keepAlive"></param>
        /// <returns></returns>
        public bool Connect(string url, bool keepAlive = true)
        {
            _keepAlive = keepAlive;
            _url = "ws://" + url + "/websocket";
            try
            {
                _socket = new WebSocket(_url);
                _socket.MessageReceived += new EventHandler<MessageReceivedEventArgs>(_socket_MessageReceived);
                _socket.Opened += new EventHandler(_socket_Opened);
                _socket.Error += new EventHandler<SuperSocket.ClientEngine.ErrorEventArgs>(_socket_Error);
                _socket.Closed += new EventHandler(_socket_Closed);
                _socket.Open();
                _isWait = 1;
                this._wait();

                if (State != WebSocketState.Open)
                {
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public WebSocketState State
        {
            get { return this._socket == null ? WebSocketState.None : _socket.State; }
        }

        public void Close()
        {
            _socket.Close();
        }

        public void Send(string message)
        {
            _socket.Send(message);
        }

        void _socket_Opened(object sender, EventArgs e)
        {
            _socket.Send("{\"msg\": \"connect\",\"version\":\"1\",\"support\":[\"1\", \"pre1\"]}");
            _isWait = 0;
        }

        void _socket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            _isWait = 0;
            SocketErrorEventArgs args = new SocketErrorEventArgs(e.Exception);
            OnError(args);
        }

        void _socket_Closed(object sender, EventArgs e)
        { }

        void _socket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (!_handle_Ping(e.Message))
            {
                this._client.AddItem(e.Message);
            }
        }

        bool _handle_Ping(string message)
        {
            if (_keepAlive && message.Equals("{\"msg\":\"ping\"}"))
            {
                _socket.Send("{\"msg\":\"pong\"}");
                return true;
            }
            return false;
        }

        private void _wait()
        {
            while (_isWait != 0)
            {
                System.Threading.Thread.Sleep(100);
            }
        }

        protected virtual void OnError(SocketErrorEventArgs e)
        {
            SocketErrorEventHandler temp = SocketError;
            if (temp != null)
            {
                SocketError(this, e);
            }
        }
    }

    public delegate void SocketErrorEventHandler(object sender, SocketErrorEventArgs e);

    public class SocketErrorEventArgs: EventArgs
    {
        public string Message;
        public Exception InnerException;
        public SocketErrorEventArgs(string Message): base()
        {
            this.Message = Message;
            this.InnerException = null;
        }
        public SocketErrorEventArgs(Exception e):base()
        {
            this.Message = e.Message;
            this.InnerException = e;
        }
    }
}
