using System.ComponentModel.DataAnnotations;

namespace DUTClinicManagement.Models
{
    public enum CollectionInterval
    {
        [Display(Name = "Day(s)")]
        Day,

        [Display(Name = "Week(s)")]
        Week,

        [Display(Name = "Month(s)")]
        Month,

        [Display(Name = "Year(s)")]
        Year
    }
}
