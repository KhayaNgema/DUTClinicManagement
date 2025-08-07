using DUTClinicManagement.Data;
using DUTClinicManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DUTClinicManagement.Services
{
    public class ChatAssignmentService
    {
        private readonly UserManager<UserBaseModel> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DUTClinicManagementDbContext _context;

        public ChatAssignmentService(
            UserManager<UserBaseModel> userManager,
            RoleManager<IdentityRole> roleManager,
            DUTClinicManagementDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        private async Task<bool> NurseHasOverlappingBookingAsync(string nurseId, DateTime conversationStartTime)
        {
            if (string.IsNullOrWhiteSpace(nurseId))
                return false;

            var bookings = await _context.Bookings
                .Where(b => b.AssignedUserId == nurseId && b.BookForDate.Date == conversationStartTime.Date)
                .ToListAsync();

            foreach (var booking in bookings)
            {
                if (BookingConflicts(booking.BookForTimeSlot, conversationStartTime))
                    return true;
            }

            return false;
        }

        private bool BookingConflicts(string timeSlot, DateTime currentTime)
        {
            if (string.IsNullOrWhiteSpace(timeSlot))
                return false;

            if (TimeSpan.TryParse(timeSlot.Trim(), out var bookingTime))
            {
                var bookingDateTime = new DateTime(
                    currentTime.Year,
                    currentTime.Month,
                    currentTime.Day,
                    bookingTime.Hours,
                    bookingTime.Minutes,
                    0);

                var timeUntilBooking = bookingDateTime - currentTime;

                return timeUntilBooking.TotalMinutes <= 10 && timeUntilBooking.TotalMinutes >= 0;
            }

            return false;
        }

        public async Task<string> GetAvailableNurseAsync(DateTime conversationStartTime)
        {
            var nurseRole = await _roleManager.FindByNameAsync("Nurse");
            if (nurseRole == null)
                return null;

            var nurses = await _userManager.GetUsersInRoleAsync("Nurse");

            foreach (var nurse in nurses)
            {
                var hasConflict = await NurseHasOverlappingBookingAsync(nurse.Id, conversationStartTime);
                if (!hasConflict)
                {
                    return nurse.Id; // Return first available nurse's ID
                }
            }

            return null; // No available nurse found
        }

        public async Task<int> GetOrCreateConversationAsync(string userId, string userRole)
        {
            if (userRole == "Nurse")
            {
                // Nurses should not create new conversations, only retrieve assigned ones
                var assignedConversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.ResponderId == userId && c.IsOpen);

                if (assignedConversation != null)
                    return assignedConversation.ConversationId;

                throw new InvalidOperationException("Nurses cannot initiate conversations.");
            }

            // Check if patient already has an active conversation
            var existingConversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.PatientId == userId && c.IsOpen);

            if (existingConversation != null)
                return existingConversation.ConversationId;

            // Create a new conversation for the patient
            var availableNurseId = await GetAvailableNurseAsync(DateTime.Now);
            if (availableNurseId == null)
                throw new InvalidOperationException("No available nurse found.");

            var newConversation = new Conversation
            {
                PatientId = userId,
                ResponderId = availableNurseId,
                StartedAt= DateTime.Now,
                IsOpen = true
            };

            _context.Conversations.Add(newConversation);
            await _context.SaveChangesAsync();

            return newConversation.ConversationId;
        }

        public async Task<Conversation> GetConversationByIdAsync(int conversationId)
        {
            return await _context.Conversations
                                 .FirstOrDefaultAsync(c => c.ConversationId == conversationId && c.IsOpen);
        }

        public async Task SaveMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }

    }
}
