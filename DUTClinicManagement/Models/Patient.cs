using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DUTClinicManagement.Models
{
    public class Patient : UserBaseModel
    {
        [StringLength(10, ErrorMessage = "Blood group cannot exceed 10 characters.")]
        public BloodType? BloodType { get; set; }

        public string FaceId { get; set; }
    }
}
