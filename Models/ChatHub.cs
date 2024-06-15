using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

public class ChatHub : Hub
{
    private static readonly ConcurrentDictionary<string, string> UserConnections = new ConcurrentDictionary<string, string>();

    public async Task RegisterUser(string username)
    {
        var connectionId = Context.ConnectionId;
        UserConnections[username] = connectionId;
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        var username = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
        if (username != null)
        {
            UserConnections.TryRemove(username, out _);
        }
        await base.OnDisconnectedAsync(exception);
    }

    // Group chat methods
    public async Task SendMessage(string group, string user, string message)
    {
        await Clients.Group(group).SendAsync("ReceiveMessage", user, message);
    }

    public async Task SendFile(string group, string user, string fileName, string fileUrl)
    {
        await Clients.Group(group).SendAsync("ReceiveFile", user, fileName, fileUrl);
    }

    public async Task JoinGroup(string group)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
    }

    public async Task LeaveGroup(string group)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }

    public async Task NotifyTypingGroup(string group, string user)
    {
        await Clients.Group(group).SendAsync("UserTyping", user);
    }

    // Private chat methods
    public async Task SendPrivateMessage(string sender, string recipient, string message)
    {
        var recipientConnectionId = GetConnectionIdByUsername(recipient);

        if (!string.IsNullOrEmpty(recipientConnectionId))
        {
            await Clients.Client(recipientConnectionId).SendAsync("ReceivePrivateMessage", sender, recipient, message);
        }
        await Clients.Caller.SendAsync("ReceivePrivateMessage", sender, recipient, message);
    }

    public async Task SendPrivateFile(string sender, string recipient, string fileName, string fileUrl)
    {
        var recipientConnectionId = GetConnectionIdByUsername(recipient);

        if (!string.IsNullOrEmpty(recipientConnectionId))
        {
            await Clients.Client(recipientConnectionId).SendAsync("ReceivePrivateFile", sender, recipient, fileName, fileUrl);
        }
        await Clients.Caller.SendAsync("ReceivePrivateFile", sender, recipient, fileName, fileUrl);
    }

    public async Task NotifyTypingPrivate(string sender, string recipient)
    {
        var recipientConnectionId = GetConnectionIdByUsername(recipient);

        if (!string.IsNullOrEmpty(recipientConnectionId))
        {
            await Clients.Client(recipientConnectionId).SendAsync("UserTypingPrivate", sender);
        }
    }

    private string GetConnectionIdByUsername(string username)
    {
        UserConnections.TryGetValue(username, out var connectionId);
        return connectionId;
    }

    public async Task DeleteMessage(string groupName, string messageId)
    {
        await Clients.Group(groupName).SendAsync("DeleteMessage", messageId);
    }

    public async Task DeletePrivateMessage(string sender, string recipient, string messageId)
    {
        await Clients.User(recipient).SendAsync("DeleteMessage", messageId);
        await Clients.User(sender).SendAsync("DeleteMessage", messageId);
    }
}