using Microsoft.AspNetCore.SignalR;

namespace app.Hubs;

/// <summary>
/// Hub SignalR para empujar eventos de PublisherComputo a la vista del usuario.
/// Cada cliente se une a un grupo identificado por su SessionId, así los eventos
/// llegan solo al usuario que disparó la resolución.
/// </summary>
public class ProgresoHub : Hub
{
    public async Task Suscribir(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }
}
