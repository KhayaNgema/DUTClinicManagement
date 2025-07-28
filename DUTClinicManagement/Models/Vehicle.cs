using System.ComponentModel.DataAnnotations.Schema;

namespace DUTClinicManagement.Models
{
    public class Vehicle
    {
        public int VehicleId { get; set; }

        public VehicleType Type { get; set; }
        public VehicleMake Make { get; set; }
        public string Model { get; set; }
        public string RegistrationNumber { get; set; }

        public int DeliveryGuyId { get; set; }
        [ForeignKey("DeliveryGuyId")]
        public virtual DeliveryGuy DeliveryGuy { get; set; }
    }
}
