using System.ComponentModel.DataAnnotations;

namespace DUTClinicManagement.Models
{
    public enum PrescriptionType
    {
        [Display(Name = "Once-off")]
        OnceOff,

        [Display(Name = "Recurring")]
        Recurring
    }
}
