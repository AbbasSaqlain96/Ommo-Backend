using System.ComponentModel.DataAnnotations;

namespace OmmoBackend.Models
{
    public class CustomPackageRequest
    {
        [Key]
        public int custom_package_request_id { get; set; }
        public int company_id { get; set; }
        public string email { get; set; }
        public int est_minutes { get; set; }
        public int concurrency { get; set; }
        public string message { get; set; }
        public int allowed_users { get; set; }
        public DateTime created_at { get; set; }
    }
}