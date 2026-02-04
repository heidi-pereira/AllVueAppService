using System;

namespace CustomerPortal.Services
{
    public class ProjectNotFound : Exception
    {
        public ProjectNotFound() : base() { }
        public ProjectNotFound(string message) : base(message) { }
        public ProjectNotFound(string message, Exception innerException)
            : base(message, innerException) { }
    }
}