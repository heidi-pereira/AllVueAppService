using System;
using System.Runtime.Serialization;

namespace DashboardBuilder
{
    [Serializable]
    public class DisplayHelpException : Exception
    {
        public DisplayHelpException() : base("") 
        {
        }

        public DisplayHelpException(string message) : base(message)
        {
        }

        public DisplayHelpException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DisplayHelpException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}