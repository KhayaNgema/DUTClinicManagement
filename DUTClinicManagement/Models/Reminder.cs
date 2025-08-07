using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUTClinicManagement.Models
{
    public class Reminder
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ReminderId { get; set; }
        
        public int BookingId { get; set; }
        public virtual FollowUpAppointment FollowUpAppointment { get; set; }

        public DateTime SentDate { get; set; }

        public string ReminderMessage { get; set; }

        public DateTime ExpiryDate { get; set; }

        public ReminderStatus Status { get; set; }
    } 

    public enum ReminderStatus
    { 
       Sent,
       Read,
       Deleted
    }

}
