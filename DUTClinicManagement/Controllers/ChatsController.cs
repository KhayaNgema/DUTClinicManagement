using DUTClinicManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DUTClinicManagement.Controllers
{
    [Authorize]
    public class ChatsController : Controller
    {
        private readonly DUTClinicManagementDbContext _context;

        public ChatsController(DUTClinicManagementDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Chats()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            ViewBag.Role = role;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var chat = await _context.Conversations
                .Where(c => c.PatientId == userId && c.IsOpen)
                .FirstOrDefaultAsync();

            var messages = await _context.Messages
                .Where(m => m.SenderId != userId)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
                _context.Update(message);
            }

            await _context.SaveChangesAsync();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetConversationHistory(int conversationId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var conversation = await _context.Conversations
                .Include(c => c.Messages)
                .Include(c => c.Patient)
                .Include(c => c.Responder)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (conversation == null)
                return Json(new object[0]);

            if (conversation.PatientId != userId && conversation.ResponderId != userId)
                return Forbid();

            var messages = conversation.Messages
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    senderId = m.SenderId,
                    content = m.Content,
                    sentAt = m.SentAt.ToString("g"), 
                    senderName = m.SenderId == userId ? "You" :
                        (m.SenderId == conversation.PatientId
                            ? $"{conversation.Patient.FirstName} {conversation.Patient.LastName}"
                            : (m.SenderId == conversation.ResponderId
                                ? $"{conversation.Responder.FirstName} {conversation.Responder.LastName}"
                                : "Unknown"))
                });

            return Json(messages);
        }

        [HttpGet]
        public async Task<IActionResult> ChatEnded()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> EndChat()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var conversation = await _context.Conversations
                .Where(c => (c.PatientId == userId || c.ResponderId == userId) && c.IsOpen)
                .FirstOrDefaultAsync();

            conversation.IsOpen = false;

            _context.Update(conversation);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(ChatEnded));
        }
    }
}