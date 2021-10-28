using System;
using System.Runtime.Serialization;

namespace MinecraftWorldConverter
{
    public class MCWorldException : Exception
    {
        public MCWorldException() : base()
        {
            
        }

        public MCWorldException(string message) : base(message)
        {
            
        }
        
        public MCWorldException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
        
        public MCWorldException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            
        }
    }
}