namespace DUTClinicManagement.Models
{
    public class Paramedic : UserBaseModel
    {
        public string Education {  get; set; }

        public int YearsOfExperience { get; set; }

        public string LicenseNumber { get; set; }

        public bool IsAvalable { get; set; }
    }
}
