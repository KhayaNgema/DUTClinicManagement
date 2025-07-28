
using DUTClinicManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DUTClinicManagement.Interfaces
{
    public interface IPaymentService
    {
        bool ValidatePayment(Payment payment);
        bool ProcessPayment(Payment payment);
    }

}