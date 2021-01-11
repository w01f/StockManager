using System;

namespace StockManager.Infrastructure.Connectors.Socket.Connection.HitBtc
{
    class MessageReceivedEventArgs : EventArgs
    {
        public string Message { get; }

        public MessageReceivedEventArgs(string message)
        {
            Message = message;
        }

    }
}
