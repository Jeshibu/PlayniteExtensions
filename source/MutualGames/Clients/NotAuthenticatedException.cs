using System;
using System.Data;

namespace MutualGames.Clients
{
    public class NotAuthenticatedException : Exception
    {
        public NotAuthenticatedException() : base() { }
        public NotAuthenticatedException(string message) : base(message) { }
        public NotAuthenticatedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
