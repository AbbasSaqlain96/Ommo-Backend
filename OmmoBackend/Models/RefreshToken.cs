using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace OmmoBackend.Models
{
    public class RefreshToken
    {
        [Key]
        public int refresh_token_id { get; set; }
        public string refresh_token { get; set; }
        public int user_id { get; set; }
        public DateTime expiration_time { get; set; }
        public bool is_revoked { get; set; }
        public DateTime created_at { get; set; }
        public DateTime revoked_at { get; set; }
    }
}