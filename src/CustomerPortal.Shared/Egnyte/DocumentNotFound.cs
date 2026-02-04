using System;

namespace CustomerPortal.Shared.Egnyte
{
    public class DocumentNotFound : Exception
    {
        public DocumentNotFound() : base() { }
        public DocumentNotFound(string message) : base(message) { }
        public DocumentNotFound(string message, Exception innerException)
            : base(message, innerException) { }
    }
}