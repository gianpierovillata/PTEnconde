using Microsoft.AspNetCore.SignalR;

namespace EncodeAssistant.EncodeApi.Hubs
{
    /// <summary>
    /// Hub de SignalR de la bandeja. Todos los clientes (pestañas del operador)
    /// se suscriben a los eventos globales: "NuevoMensaje" y "ContactoActualizado".
    /// </summary>
    public class InboxHub : Hub
    {
    }
}
