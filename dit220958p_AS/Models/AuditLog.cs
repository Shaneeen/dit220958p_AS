using System;

namespace dit220958p_AS.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string? UserId { get; set; }      // User's unique ID
        public string UserEmail { get; set; }   // User's email
        public string Action { get; set; }      // Action performed (e.g., Login, Logout)
        public string IPAddress { get; set; }   // IP Address of the user
        public DateTime Timestamp { get; set; } // When the action occurred
        public string Details { get; set; }     // Optional: Additional details
    }
}
