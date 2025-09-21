namespace OmmoBackend.Middlewares
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class NotificationMetadata : Attribute
    {
        public string Module { get; }
        public string Component { get; }
        public int AccessLevel { get; }

        public NotificationMetadata(string module, string component, int accessLevel)
        {
            Module = module;
            Component = component;
            AccessLevel = accessLevel;
        }
    }
}
