using Quobject.SocketIoClientDotNet.Client;
using System;

namespace TwitchBot
{
    class DonationAlerts
    {
        public Action<object> onDonation;
        private string uri = "http://socket.donationalerts.ru";
        private int port = 3001;
        string token = "RURFttu1Y6la2tuqnxS6";

        public void Connect(string token)
        {
            this.token = token;
            Connect();
        }
        public void Connect()
        {
            Console.WriteLine("DonationAlerts connecting...");
            onDonation += Donation;
            var socket = IO.Socket(uri, new IO.Options() { Port = port });
            socket.Emit("add-user", new string[] { token, "minor" });
            socket.On("add-user", onDonation);
        }

        private void Donation(object info)
        {
            if (info == null)
            {
                Console.WriteLine("info is null");
                throw new ArgumentNullException(nameof(info));
            }
            Console.WriteLine(info);
            Console.WriteLine(info.ToString());
            throw new NotImplementedException();
        }
    }
}
