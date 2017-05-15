using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using WebSocket4Net;

namespace Net.DDP.Client
{
    public class DDPClient:IClient
    {
        private DDPConnector _connector;
        private int _uniqueId;
        private ResultQueue _queueHandler;

        public event SocketErrorEventHandler SocketError;

        public DDPClient(IDataSubscriber subscriber)
        {
            this._connector = new DDPConnector(this);
            this._connector.SocketError += new SocketErrorEventHandler(_connector_Error);
            this._queueHandler = new ResultQueue(subscriber);
            _uniqueId = 1;
        }

        public void AddItem(string jsonItem)
        {
            _queueHandler.AddItem(jsonItem);
        }

        /// <summary>
        /// Creates a new connection.
        /// Returns true if a connection was successfully opened to the url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public bool Connect(string url, bool useSsl = false)
        {
            return _connector.Connect(url, useSsl: useSsl);
        }

        public int Call(string methodName, params object[] args)
        {
            int id = this.NextId();
            string json = JsonConvert.SerializeObject(new
            {
                msg = "method",
                method = methodName,
                @params = args,
                id = id.ToString()
            });
            _connector.Send(json);
            return id;
        }

        public void Close()
        {
            _connector.Close();
        }

        public int Subscribe(string subscribeTo, params object[] args)
        {
            int id = this.NextId();
            string json = JsonConvert.SerializeObject(new
            {
                msg = "sub",
                name = subscribeTo,
                @params = args,
                id = id.ToString()
            });
            _connector.Send(json);
            return id;
        }

        public WebSocketState State
        {
            get { return this._connector.State; }
        }

       
        private int NextId()
        {
            return _uniqueId++;
        }

        public int GetCurrentRequestId()
        {
            return _uniqueId;
        }

        /// <summary>
        /// Propogate socket error
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void _connector_Error(object sender, SocketErrorEventArgs e)
        {
            OnError(e);
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
}
