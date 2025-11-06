using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

    namespace OmmoBackend.Models
    {
        [Table("agent_settings")]
        public class AgentSettings
        {
            [Key]
            [Column("agent_guid")]
            public Guid AgentGuid { get; set; }

            [Required]
            [Column("agent_name")]
            public string AgentName { get; set; } = string.Empty;

            [Required]
            [Column("who_we_are")]
            public string WhoWeAre { get; set; } = string.Empty;

            [Required]
            [Column("voice_gender")]
            [MaxLength(10)]
            public string VoiceGender { get; set; } = "female";  // default example

            [Required]
            [Column("floor_rpm", TypeName = "numeric(8,3)")]
            public decimal FloorRpm { get; set; }

            [Required]
            [Column("target_rpm", TypeName = "numeric(8,3)")]
            public decimal TargetRpm { get; set; }

            [Required]
            [Column("walkaway_rpm", TypeName = "numeric(8,3)")]
            public decimal WalkawayRpm { get; set; }

            [Required]
            [Column("consent_mode")]
            public bool ConsentMode { get; set; }

            [Required]
            [Column("created_at")]
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

            [Required]
            [Column("updated_at")]
            public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        }
    }

