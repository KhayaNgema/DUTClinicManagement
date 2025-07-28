using System.ComponentModel.DataAnnotations;

namespace DUTClinicManagement.Models
{
    public enum UnitOfMeasure
    {
        [Display(Name = "mg")]
        Milligram,

        [Display(Name = "g")]
        Gram,

        [Display(Name = "ml")]
        Milliliter,

        [Display(Name = "L")]
        Liter,

        [Display(Name = "mcg")]
        Microgram,

        [Display(Name = "IU")]
        InternationalUnit,

        [Display(Name = "Tablet")]
        Tablet,

        [Display(Name = "Capsule")]
        Capsule
    }
}
