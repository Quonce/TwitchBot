using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;

namespace TwitchBot
{
    class Program
    {
        public static Dictionary<string, TwitchChatBot> activeChatBots = new Dictionary<string, TwitchChatBot>();
        public static List<string> paidChannels = new List<string>();

        private static string[] commands = new string[] { "./connect [channelName] [channelName]", "./disconnect [channelName] [channelName]", "./addchannel [channelName] [channelName]", "./removechannel [channelName] [channelName]", "./activechannels", "./paidchannels" };

        public static bool isStressTest;

        static void Main(string[] args)
        {
            Console.WriteLine("Twitch Foxx Bot (v0.1.0)");
            Console.WriteLine("Type /connect [channelName] for connecting FoxxChatBot to channel");

            //StreamLabs streamLabs = new StreamLabs(TwitchInfo.StreamLabs_SocketToken, true, "");
            //streamLabs.Connect();

            Load();
            TwitchBotConnect(paidChannels);
            DonationAlerts donationAlerts = new DonationAlerts();
            donationAlerts.Connect();
            ReadCommand(Console.ReadLine());
        }

        public static void TwitchBotConnect(string channel, bool isNew)
        {
            if (activeChatBots.ContainsKey(channel))
                return;

            TwitchChatBot bot = new TwitchChatBot(channel);
            bot.useSetup = isNew;
            bot.Connect();

        }
        public static void TwitchBotConnect(List<string> channels)
        {
            for (int i = 0; i < channels.Count; i++)
            {
                TwitchChatBot bot = new TwitchChatBot(channels[i]);
                bot.Connect();
            }
        }

        public static void TwitchBotDisconnect(string channel)
        {
            if (!activeChatBots.ContainsKey(channel))
                return;

            activeChatBots[channel].Disconnect();
            activeChatBots.Remove(channel);
        }
        public static void TwitchBotDisconnect(List<string> channels)
        {
            for (int i = 0; i < channels.Count; i++)
            {
                activeChatBots[channels[i]].Disconnect();
                activeChatBots.Remove(channels[i]);
            }
        }

        public static void AddPaidChannel(string channelName)
        {
            if (paidChannels.Contains(channelName.ToLower()))
                return;

            Console.WriteLine("Paid Channel Added: " + channelName);
            paidChannels.Add(channelName.ToLower());
            Save();
            TwitchBotConnect(channelName, true);

        }
        public static void RemovePaidChannel(string channelName)
        {
            if (!paidChannels.Contains(channelName.ToLower()))
                return;

            Console.WriteLine("Paid Channel Removed: " + channelName);
            paidChannels.Remove(channelName.ToLower());
            Save();
            TwitchBotDisconnect(channelName);
        }

        public static void Save()
        {
            var paidChannelsJson = new JavaScriptSerializer().Serialize(paidChannels);

            System.IO.Directory.CreateDirectory(Environment.CurrentDirectory+@"\Data"+@"\");
            System.IO.File.Delete(Environment.CurrentDirectory+@"\Data"+@"\PaidChannels.txt");
            System.IO.File.WriteAllText(Environment.CurrentDirectory+@"\Data"+@"\PaidChannels.txt", paidChannelsJson);
        }
        public static void Load()
        {
            if (!System.IO.Directory.Exists(Environment.CurrentDirectory+@"\Data"+@"\"))
                return;

            if (System.IO.File.Exists(Environment.CurrentDirectory+@"\Data"+@"\PaidChannels.txt"))
            {
                string paidChannelsJson = System.IO.File.ReadAllText(Environment.CurrentDirectory+@"\Data"+@"\PaidChannels.txt");
                paidChannels = new JavaScriptSerializer().Deserialize<List<string>>(paidChannelsJson);
            }
        }

        static void ReadCommand(string commandLine)
        {
            commandLine = commandLine.ToLower();

            bool isCommand = false;
            if (commandLine.StartsWith("/"))
                isCommand = true;
            if (isCommand)
            {
                commandLine = commandLine.Replace("/","");
                string[] splited = commandLine.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                List<string> arguments = splited.ToList();
                string command = arguments[0];
                arguments.RemoveAt(0);

                switch (command)
                {
                    case "help":
                        Console.WriteLine("");
                        Console.WriteLine("Programm Commands:");
                        for (int i = 0; i < commands.Length; i++)
                            Console.WriteLine(commands[i]);
                        Console.WriteLine("");
                        break;

                    case "connect":
                        if(arguments.Count > 0)
                            TwitchBotConnect(arguments[0], false);
                        break;

                    case "disconnect":
                        if (arguments.Count > 0)
                            TwitchBotDisconnect(arguments);
                        break;

                    case "addchannel":
                        if (arguments.Count > 0)
                            AddPaidChannel(arguments[0]);
                        break;
                    case "removechannel":
                        if (arguments.Count > 0)
                            RemovePaidChannel(arguments[0]);
                        break;

                    case "activechannels":
                        Console.WriteLine("");
                        Console.WriteLine("Active Channels:");
                        foreach(KeyValuePair<string, TwitchChatBot> channel in activeChatBots)
                        {
                            Console.WriteLine(channel.Key);
                        }
                        Console.WriteLine("");
                        break;

                    case "paidchannels":
                        Console.WriteLine("");
                        Console.WriteLine("Paid Channels:");
                        for (int i = 0; i < paidChannels.Count; i++)
                            Console.WriteLine(paidChannels[i]);
                        Console.WriteLine("");
                        break;
                    case "stresstest":
                        isStressTest = true;
                        break;
                    case "throw":
                        try
                        {
                            activeChatBots[arguments[0]].ThrowStone(arguments[1], arguments[2]);
                        }
                        catch { }
                        break;
                    case "give":
                        try
                        {
                            activeChatBots[arguments[0]].GiveItem(arguments[1], arguments[2], 1);
                        }
                        catch { }
                        break;
                }
            }
            ReadCommand(Console.ReadLine());
        }
    }
}
