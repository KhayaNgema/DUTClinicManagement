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

        public IActionResult Chats()
        {
            var role = User.FindFirstValue(ClaimTypes.Role);
            ViewBag.Role = role;
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
    }
}