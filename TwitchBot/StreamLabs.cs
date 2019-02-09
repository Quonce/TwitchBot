using System;
using System.Linq;
using StreamLabsDotNet.Client;
using StreamLabsDotNet.Api;
using Microsoft.Extensions.Logging;

namespace TwitchBot
{
    class StreamLabs
    {
        private string socketToken = "";

        private bool isDeveloper;
        private string channelName;
        
        public StreamLabs(string token, bool isDeveloper,string channelName)
        {
            socketToken = token;
            this.isDeveloper = isDeveloper;
            this.channelName = channelName;
        }

        public void Connect()
        {
            Console.WriteLine($"");
            Console.WriteLine("Connecting to StreamLabs");

            ILogger<StreamLabsApiBase> logger;
            logger = new LoggerFactory().CreateLogger<StreamLabsApiBase>();

            Client streamLabsClient = new Client();
            streamLabsClient.Connect(socketToken);

            streamLabsClient.OnConnected += StreamLabsClient_OnConnected;
            streamLabsClient.OnError += StreamLabsClient_OnError;
            streamLabsClient.OnDonation += StreamLabsClient_OnDonation;
            streamLabsClient.OnDisconnected += StreamLabsClient_OnDisconnected;
        }

        

        private void StreamLabsClient_OnConnected(object sender, bool e)
        {
            Console.WriteLine($"StreamLabs - Connection Successful ({e})");
        }
        private void StreamLabsClient_OnError(object sender, string e)
        {
            Console.WriteLine("StreamLabs - Error!! " + e);
        }
        private void StreamLabsClient_OnDonation(object sender, StreamLabsDotNet.Client.Models.StreamlabsEvent<StreamLabsDotNet.Client.Models.DonationMessage> e)
        {
            Console.WriteLine($"");
            Console.WriteLine($"Donation Accept");
            Console.WriteLine($"isDeveloper {isDeveloper}");
            if (isDeveloper)
            {
                for (int i = 0; i < e.Message.Length; i++)
                {
                    Console.WriteLine($"Donation from: {e.Message[i].From}. {e.Message[i].FormattedAmount}");
                    Console.WriteLine($"Message: {e.Message[i].Message}");
                    Program.AddPaidChannel(e.Message[i].From);
                }
                Console.WriteLine($"");
            }
            else
            {
                for (int i = 0; i < e.Message.Length; i++)
                {
                    Console.WriteLine($"Try send donation");
                    Program.activeChatBots[channelName].Donation(e.Message[i].From, e.Message[i].Amount, e.Message[i].Currency, e.Message[i].FormattedAmount, e.Message[i].Message);
                }
            }

        }
        private void StreamLabsClient_OnDisconnected(object sender, bool e)
        {
            Console.WriteLine("StreamLabs - Disconnected");
        }

    }
}
