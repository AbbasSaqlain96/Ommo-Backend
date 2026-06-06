namespace OmmoBackend.Exceptions
{
    public class CustomFileStorageException : Exception
    {
        public CustomFileStorageException(string message, Exception innerException) : base(message, innerException) { }
    }
}