using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace App.Real.Services;

public sealed class NameIdentifierUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}




