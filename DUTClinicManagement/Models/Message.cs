using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUTClinicManagement.Models
{
    public class Message
    {
        public int MessageId { get; set; }

        [Required]
        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } = null!;

        [Required]
        public string SenderId { get; set; }
        [ForeignKey("SenderId")]
        public UserBaseModel Sender { get; set; } = null!;

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = null!;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}
