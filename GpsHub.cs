using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace DACS
{
    public class GpsHub : Hub
    {
        public async Task SendLocationUpdate(string plate, double lat, double lng, double speed)
        {
            await Clients.All.SendAsync("ReceiveLocationUpdate", plate, lat, lng, speed);
        }
    }
}
