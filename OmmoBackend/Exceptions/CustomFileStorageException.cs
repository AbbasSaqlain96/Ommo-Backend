using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Exceptions
{
    public class CustomFileStorageException : Exception
    {
        public CustomFileStorageException(string message, Exception innerException) : base(message, innerException) { }
    }
}