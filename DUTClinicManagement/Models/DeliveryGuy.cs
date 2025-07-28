namespace DUTClinicManagement.Models
{
    public class DeliveryGuy: UserBaseModel
    {
        public string DriverLicenseNumber { get; set; }
        public DateTime LicenseExpiryDate { get; set; }

        public bool IsAvailable { get; set; }
        public virtual Vehicle Vehicle { get; set; }
        //public virtual ICollection<DeliveryTask> DeliveryTasks { get; set; }
    }
}
