using System;

namespace PlayniteExtensions.Common;

public class NotAuthenticatedException : Exception
{
    public NotAuthenticatedException() : base("Not authenticated.") { }
    public NotAuthenticatedException(string message) : base(message) { }
    public NotAuthenticatedException(string message, Exception innerException) : base(message, innerException) { }
}
