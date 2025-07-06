using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Agent2Agent.Web.Hubs
{
    public class ChatHub : Hub
    {
        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task NotifyTyping(string user)
        {
            await Clients.Others.SendAsync("UserTyping", user);
        }

        public async Task UploadFile(string user, string fileName, byte[] fileData)
        {
            await Clients.All.SendAsync("ReceiveFile", user, fileName, fileData);
        }
    }
}
