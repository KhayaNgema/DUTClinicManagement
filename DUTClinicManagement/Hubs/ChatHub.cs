using DUTClinicManagement.Data;
using DUTClinicManagement.Models;
using DUTClinicManagement.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

public class ChatHub : Hub
{
    private readonly ChatAssignmentService _chatAssignmentService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ChatHub(ChatAssignmentService chatAssignmentService, IHttpContextAccessor httpContextAccessor)
    {
        _chatAssignmentService = chatAssignmentService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<int> GetOrCreateConversation()
    {
        var user = _httpContextAccessor.HttpContext.User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = user.FindFirstValue(ClaimTypes.Role); // Make sure Role claim is set correctly

        if (string.IsNullOrEmpty(userId))
            throw new HubException("User not authenticated");

        var conversationId = await _chatAssignmentService.GetOrCreateConversationAsync(userId, userRole);
        return conversationId;
    }

    public override async Task OnConnectedAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = user?.FindFirstValue(ClaimTypes.Role);

        int conversationId;

        try
        {
            conversationId = await _chatAssignmentService.GetOrCreateConversationAsync(userId, userRole);
        }
        catch (Exception)
        {
            // Handle no conversation for nurse etc.
            conversationId = 0;
        }

        if (conversationId != 0)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        }

        await base.OnConnectedAsync();
    }

    public async Task SendMessageToDepartment(int conversationId, string message)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userRole = user?.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrEmpty(userId))
            throw new HubException("User not authenticated");

        // Verify user is participant in the conversation
        var conversation = await _chatAssignmentService.GetConversationByIdAsync(conversationId);

        if (conversation == null)
            throw new HubException("Conversation not found");

        bool isParticipant = userRole == "Nurse" ? conversation.ResponderId == userId
                          : conversation.PatientId == userId;

        if (!isParticipant)
            throw new HubException("You are not a participant of this conversation.");

        // Create and save message to DB
        var newMessage = new Message
        {
            ConversationId = conversationId,
            SenderId = userId,
            Content = message,
            SentAt = DateTime.UtcNow
        };

        await _chatAssignmentService.SaveMessageAsync(newMessage);

        // Broadcast message to group
        await Clients.Group(conversationId.ToString())
                     .SendAsync("ReceiveMessage", userId, message);
    }


}
