namespace DUTClinicManagement.Models
{
    public class Nurse : UserBaseModel
    {
        public string LicenseNumber { get; set; }
        public DateTime LicenseExpiryDate { get; set; }

        public bool IsOnDuty { get; set; }

        public string Qualification { get; set; }
    }
}
