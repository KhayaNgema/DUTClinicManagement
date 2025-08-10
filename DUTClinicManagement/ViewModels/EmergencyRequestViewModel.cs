using DUTClinicManagement.Models;

namespace DUTClinicManagement.ViewModels
{
    public class EmergencyRequestViewModel
    {
        public string PatientId { get; set; }  
        
        public RequestLocation RequestLocation { get; set; }

        public string EmergencyDetails { get; set; }

        public double PatientLatitude { get; set; }

        public double PatientLongitude { get; set; }
    }
}
