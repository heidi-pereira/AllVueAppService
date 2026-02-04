using System;

namespace CustomerPortal.Services
{
    public class NoProjectAccess : Exception
    {
        public NoProjectAccess() : base() { }
        public NoProjectAccess(string message) : base(message) { }
        public NoProjectAccess(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
