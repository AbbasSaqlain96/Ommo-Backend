using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Helpers.Enums
{
    public enum AccessLevel
    {
        read_only = 1,
        read_write = 2,
        none = 0
    }
}