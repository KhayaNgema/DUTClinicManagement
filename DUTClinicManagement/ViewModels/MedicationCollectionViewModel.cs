using DUTClinicManagement.Models;

namespace DUTClinicManagement.ViewModels
{
    public class MedicationCollectionViewModel
    {
        public int MedicationPescriptionId { get; set; }

        public Booking Booking { get; set; }

        public ICollection<Medication> PrescribedMedication { get; set; }

        public DateTime? NextCollectionDate { get; set; }
        public DateTime? LastCollectionDate { get; set; }
        public MedicationPescriptionStatus Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public Province Province { get; set; }  
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }
}
