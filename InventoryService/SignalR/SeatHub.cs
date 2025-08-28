using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace InventoryService.SignalR
{
    public class SeatHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            // Khi client join vào show cụ thể
            var showId = Context.GetHttpContext()?.Request.Query["showId"];
            if (!string.IsNullOrEmpty(showId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"show-{showId}");
            }

            await base.OnConnectedAsync();
        }
    }

}
