using DUTClinicManagement.Data;
using DUTClinicManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DUTClinicManagement.Services
{
    public class ParamedicAssignmentService
    {
        private readonly DUTClinicManagementDbContext _context;

        public ParamedicAssignmentService(DUTClinicManagementDbContext context)
        {
            _context = context;
        }

        public async Task<string> AssignSingleAvailableParamedicAsync()
        {
            var availableParamedics = await _context.Paramedics
                .Where(p => p.IsAvalable)
                .ToListAsync();

            if (availableParamedics.Count == 1)
            {
                return availableParamedics[0].Id;
            }

            return null;
        }
    }
}
