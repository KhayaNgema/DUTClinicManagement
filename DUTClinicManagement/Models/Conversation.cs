using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUTClinicManagement.Models
{
    public class Conversation
    {
        public int ConversationId { get; set; }

        [Required]
        public string PatientId { get; set; }
        [ForeignKey("PatientId")]
        public Patient Patient { get; set; } = null!;

        public string? ResponderId { get; set; }
        [ForeignKey("ResponderId")]
        public UserBaseModel? Responder { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public bool IsOpen { get; set; } = true;

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
