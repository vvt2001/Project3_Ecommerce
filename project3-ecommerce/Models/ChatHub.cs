using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace project3_ecommerce.Models
{
    public class ChatHub : Hub
    {
        MessageEntities db1 = new MessageEntities();
        AccountEntities db2 = new AccountEntities();
        public void Hello()
        {
            Clients.All.hello();
        }
        public void SendGroup(string name, string message, string groupName)
        {
            var sender = db2.Accounts.Where(x => x.Username == name).FirstOrDefault();
            var receiver = new Account();
            if(sender.Username != groupName)
            {
                receiver.Username = groupName;
            }
            else
            {
                //receiver.Username = (admin username);
                receiver.Username = "vvt";
            }
            receiver = db2.Accounts.Where(x => x.Username == receiver.Username).FirstOrDefault();

            var messageInfo = new Message
            {
                Date = DateTime.Now,
                SenderName = name,
                SenderID = sender.ID,
                ReceiverName = receiver.Username,
                ReceiverID = receiver.ID,
                Message1 = message,
                GroupName = groupName,
            };
            db1.Messages.Add(messageInfo);
            db1.SaveChanges();

            Clients.Group(groupName).addNewMessageToPage(name, message);
        }
        public Task JoinRoom(string groupName)
        {
            var messages = db1.Messages.Where(x => x.GroupName == groupName);
            foreach(var item in messages)
            {
                Clients.Group(groupName).addNewMessageToPage(item.SenderName, item.Message1);
            }
            return Groups.Add(Context.ConnectionId, groupName);
        }

        public Task LeaveRoom(string roomName)
        {
            return Groups.Remove(Context.ConnectionId, roomName);
        }
    }
}