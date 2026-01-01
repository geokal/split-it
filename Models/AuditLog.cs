using System;
using System.ComponentModel.DataAnnotations;

namespace QuizManager.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Action { get; set; }

        [Required]
        public string Role { get; set; }

        [Required]
        public string Description { get; set; }

        public string? IpAddress { get; set; }

        public string? UserAgent { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Details { get; set; }

        public bool Success { get; set; } = true;
    }
}
