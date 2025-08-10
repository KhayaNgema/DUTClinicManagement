using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUTClinicManagement.Models
{
    public class EmergencyRequest
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestId { get; set; }

        public string PatientId { get; set; }
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; }

        public ICollection<string> AssignedParamedicIds { get; set; }
        public virtual Paramedic Paramedic { get; set; }


        public string EmergencyDetails { get; set; }


        public DateTime RequestTime { get; set; }

        public DateTime ModifiedDateTime { get; set; }

        public string ModifiedById { get; set; }
        [ForeignKey("ModifiedById")]

        public virtual  UserBaseModel ModifiedBy { get; set; }


        public RequestStatus? RequestStatus { get; set; }

        public RequestLocation RequestLocation { get; set; }

        public Priority? Priority { get; set; }

        public string ReferenceNumber { get; set; }

        public double PatientLatitude { get; set; }

        public double PatientLongitude { get; set; }
        public double ParamedicLatitude { get; set; }

        public double ParamedicLongitude { get; set; }
    }

    public enum RequestStatus
    {
        Pending,
        Assigned,
        Departed,
        Arrived,
        Completed
    }


    public enum Priority
    {
        [Display(Name = "Very High")]
        VeryHigh,

        [Display(Name = "High")]
        High,

        [Display(Name = "Medium")]
        Medium,

        [Display(Name = "Low")]
        Low
    }

}
