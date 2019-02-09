using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Script.Serialization;
using TwitchLib.Api;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace TwitchBot
{
    internal class TwitchChatBot : IDisposable
    {
        readonly ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUserName, TwitchInfo.BotToken);
        TwitchClient twitchClient = new TwitchClient();

        [DataContract]
        private class ItemClass
        {
            public int count;
            public int price;
            public int countLimit;
            public string itemDescription = "";

            public ItemClass() { }
            public ItemClass(int _count, int _countLimit, int _price)
            {
                count = _count;
                countLimit = _countLimit;
                price = _price;
            }
            public ItemClass(int _count, int _countLimit, int _price, string _description)
            {
                count = _count;
                countLimit = _countLimit;
                price = _price;
                itemDescription = _description;
            }
        }

        [DataContract]
        private class InventoryClass
        {
            public Dictionary<string, ItemClass> items = new Dictionary<string, ItemClass>();
        }

        [DataContract]
        private class UserClass
        {
            public int maxHealth = 100;
            public int curHealth = 100;

            public InventoryClass inventory = new InventoryClass();
        }

        [DataContract]
        private class ChatClass
        {
            public bool isStoneActive = false;
            public int maxStones = 1;
            public int stoneDamage = 35;
            public int searchStoneTime = 210;
            public int healthRegenTime = 180;
            public int defaultMaxHealth = 100;

            public int helloTimerDelay = 7200;
            public int updateChatUsersDelay = 300;
            public int giveCoinDelay = 60;
            public int rewardPerRub = 5;

            public string[] helloMessages = {
            "Здравствуй, комрад",
            "Привет! друг мой",
            "Доброго времени суток!",
            "Привет, Бро!",
            "Хаюшки!",
            "Как жизнь?",
            "Салют!",
            "Моё почтение.",
            "В кои-то веки!",
            "Вот так встреча!",
            "Всегда рады Вам",
            "Глубокое почтение",
            "Горячий привет!",
            "Горячо приветствую",
            "Доброго здоровья!",
            "Доброе утро!",
            "Добро пожаловать!",
            "Добрый вечер!",
            "Добрый день!",
            "Дозвольте приветствовать",
            "Душевно рад",
            "Душою рад Вас видеть",
            "Желаю здравствовать!",
            "Здравия желаю",
            "Здравствуйте!",
            "Какая встреча!",
            "Какие гости!",
            "Моё почтение!",
            "Позвольте Вас приветствовать",
            "Почтение моё",
            "Приветствую Вас",
            "Приятный вечер!",
            "Приятный день!",
            "Рад Вам",
            "Рад Вас видеть",
            "Рад Вас видеть в добром здравии",
            "Рад Вас приветствовать",
            "Рад Вас слышать",
            "Рад пожать Вашу руку",
            "Разрешите Вас приветствовать",
            "С возвращением!",
            "С выздоровлением!",
            "С добрым утром!",
            "Сердечно приветствую Вас!",
            "Сердечно рад Вам",
            "Сердечный поклон Вам",
            "Сердечный привет Вам",
            "Сколько лет, сколько зим!",
            "Тысячу лет Вас не видел!"
        };
            public List<string> helloExample = new List<string>{
            "здравст ",
            "привет ",
            "пливет ",
            "драсьте ",
            "hello ",
            "hi ",
            "добрый день",
            "добрый вечер",
            "доброе утро",
            "прив ",
            "шалом ",
            "драсьте ",
            "дарова "
        };
            public bool useHelloMessages = false;

            public List<string> moderatorsList = new List<string>{ "pashafoxx" };
            public List<string> moderatorsCommands = new List<string> { "timeout", "clear", "ban", "unban", "slow", "slowoff", "subscribers", "subscribersoff", "r9kbeta", "r9kbetaoff", "ignore", "unignore", "disconnect" };

            public Dictionary<string, string> customFeedback = new Dictionary<string, string>();

            public Dictionary<string, ItemClass> store = new Dictionary<string, ItemClass>();

            public string streamLabs_SocketToken = "";

            public string automessage = $"(つ◉益◉)つcxxx[]:::::::::::> CurseLit Покупайте предметы в [!store] CurseLit {WhiteSpace()+WhiteSpace()} TwitchLit В минуту вам начисляется 1¢ TwitchLit {WhiteSpace()+ WhiteSpace()} <3 Так же монетки даются за донаты <3 ";
            public int automessageDelay = 1500;
        }

        private class ChattersClass
        {
            public class Chatters
            {
                public string[] vips, moderators, staff, admins, global_mods, viewers;
            }
            public Chatters chatters;
        }

        private ChatClass chatSettings = new ChatClass();
        private Dictionary<string, UserClass> users = new Dictionary<string, UserClass>();

        private List<string> chatUsers = new List<string>();
        private List<string> chatModers = new List<string>();
        
        private string previousHelloMessage;


        TwitchAPI api;
        private Timer timerForCoins = new Timer();
        private Timer timerForUsersUpdate = new Timer();
        private Timer timerForAutoMessage = new Timer();

        private Dictionary<string, Timer> timerForHelloMessage = new Dictionary<string, Timer>();
        private Dictionary<string, Timer> timerForRegenerate = new Dictionary<string, Timer>();

        private string channelName;
        public bool useSetup;
        StreamLabs streamLabs;

        public TwitchChatBot(string ChannelName)
        {
            channelName = ChannelName;
        }
        public TwitchChatBot()
        {
        }
        #region Twitch Connection Stuff

        private void Save()
        {
           // Console.WriteLine($"[{ channelName}] Saving...");
            JavaScriptSerializer chatJson = new JavaScriptSerializer();
            JavaScriptSerializer usersJson = new JavaScriptSerializer();

            chatJson.MaxJsonLength = 1000000000;
            usersJson.MaxJsonLength = 1000000000;

            var chatJsonString = chatJson.Serialize(chatSettings);
            var usersJsonString = usersJson.Serialize(users);

            System.IO.Directory.CreateDirectory(Environment.CurrentDirectory+@"\Data" + @"\ChannelsData\" + channelName);

            System.IO.File.Delete(Environment.CurrentDirectory+@"\Data" + @"\ChannelsData\" + channelName + @"\ChatSettings.txt");
            System.IO.File.Delete(Environment.CurrentDirectory+@"\Data" + @"\ChannelsData\" + channelName + @"\Users.txt");

            System.IO.File.WriteAllText(Environment.CurrentDirectory+@"\Data" + @"\ChannelsData\" + channelName + @"\ChatSettings.txt", chatJsonString);
            System.IO.File.WriteAllText(Environment.CurrentDirectory+@"\Data" + @"\ChannelsData\" + channelName + @"\Users.txt", usersJsonString);
            //Console.WriteLine($"[{ channelName}] Succsessfully");
          //  Console.WriteLine("");
        }
        private void Load()
        {
            if (!System.IO.Directory.Exists(Environment.CurrentDirectory+@"\Data"+@"\ChannelsData\"+channelName))
                return;

            Console.WriteLine($"[{ channelName}] Begin Loading!");


            JavaScriptSerializer chatJson = new JavaScriptSerializer();
            JavaScriptSerializer usersJson = new JavaScriptSerializer();

            chatJson.MaxJsonLength = 1000000000;
            usersJson.MaxJsonLength = 1000000000;
            if (System.IO.File.Exists(Environment.CurrentDirectory+@"\Data"+@"\ChannelsData\" + channelName + @"\ChatSettings.txt"))
            {
                string chatJsonString = System.IO.File.ReadAllText(Environment.CurrentDirectory+@"\Data"+@"\ChannelsData\" + channelName + @"\ChatSettings.txt");
                chatSettings = chatJson.Deserialize<ChatClass>(chatJsonString);
            }
            if (System.IO.File.Exists(Environment.CurrentDirectory+@"\Data"+@"\ChannelsData\" + channelName + @"\Users.txt"))
            {
                string usersJsonString = System.IO.File.ReadAllText(Environment.CurrentDirectory+@"\Data"+@"\ChannelsData\" + channelName + @"\Users.txt");
                users = usersJson.Deserialize<Dictionary<string, UserClass>>(usersJsonString);
            }

            foreach (KeyValuePair<string, UserClass> user in users)
                user.Value.curHealth = user.Value.maxHealth;

            Console.WriteLine($"[{ channelName}] Data Succsessfully Loaded!");
        }

        internal void Connect()
        {
            Console.WriteLine($"Connecting to channel [{channelName}]" );

            twitchClient.Initialize(credentials, channelName);

            twitchClient.OnLog += Client_OnLog;
            twitchClient.OnConnectionError += Client_OnConnectionError;
            twitchClient.OnConnected += Client_OnConnected;
            twitchClient.OnMessageReceived += TwitchClient_OnMessageReceived;
            twitchClient.OnWhisperReceived += TwitchClient_OnWhisperReceived;
            twitchClient.OnChatCommandReceived += TwitchClient_OnChatCommandReceived;
            twitchClient.OnDisconnected += TwitchClient_OnDisconnected;

            chatSettings = new ChatClass();
            twitchClient.Connect();

            api = new TwitchAPI();
            api.Settings.ClientId = TwitchInfo.ClientId;
            api.Settings.AccessToken = TwitchInfo.BotToken;

            Load();

            StreamLabsConnect();
            GetActiveUsersList();
        }

        public void DefaultSetup()
        {
            
            if (!chatSettings.customFeedback.ContainsKey("money"))
                chatSettings.customFeedback.Add("money", "[w]У тебя [coins]¢!");

            AddStoreItem(TwitchInfo.BotUserName, "камень", 300, 1, "");
            AddStoreItem(TwitchInfo.BotUserName, "аптечка", 150, 5, "");
            AddStoreItem(TwitchInfo.BotUserName, "коробка", 15, 10, "[user] спрятался в коробку. И думает, что его никто не видит LUL");
            AddStoreItem(TwitchInfo.BotUserName, "обнимашки", 5, 100, " <3 [randomuser] нежно обнимает [user] <3 ");
            AddStoreItem(TwitchInfo.BotUserName, "музыка", 50, 100, "[user] хочет заказать музыку на стрим. Дайте ему слово.");
            /*
            twitchClient.SendMessage(channelName, $"");
            twitchClient.SendMessage(channelName, $"");
            twitchClient.SendMessage(channelName, $"");
            twitchClient.SendMessage(channelName, $"");
            twitchClient.SendMessage(channelName, $"");
            twitchClient.SendMessage(channelName, $"");
            */
        }

        internal void Disconnect()
        {
            Console.WriteLine($"[{ channelName}] Disconnecting...");
            twitchClient.Disconnect();
        }

        private void StreamLabsConnect()
        {
            try
            {
                if (chatSettings.streamLabs_SocketToken != "")
                {
                    streamLabs = new StreamLabs(chatSettings.streamLabs_SocketToken, false, channelName);
                    streamLabs.Connect();
                }
            }
            catch { }
        }


        private void Client_OnConnected(object sender, TwitchLib.Client.Events.OnConnectedArgs e)
        {
            Console.WriteLine($"[{ channelName}] Connected! Channel: {channelName}");
            Console.WriteLine($"[{ channelName}] Username: " + twitchClient.TwitchUsername);
            Console.WriteLine("");

            timerForCoins.Interval = chatSettings.giveCoinDelay * 1000;
            timerForCoins.Elapsed += TimerForCoins_Elapsed;
            timerForCoins.Start();

            timerForUsersUpdate.Interval = chatSettings.updateChatUsersDelay * 1000;
            timerForUsersUpdate.Elapsed += TimerForUsersUpdate_Elapsed;
            timerForUsersUpdate.Start();

            timerForAutoMessage.Interval = chatSettings.automessageDelay * 1000;
            timerForAutoMessage.Elapsed += TimerForAutoMessage_Elapsed;
            timerForAutoMessage.Start();

            FollowChannel();

            if (api.V5.Streams.BroadcasterOnlineAsync(GetUserId(channelName).Result).Result)
                Console.WriteLine("Channel Online!");
            else
                Console.WriteLine("Channel Offline");

            Program.activeChatBots.Add(channelName, this);

            if (useSetup)
            {
                DefaultSetup();
            }
        }

        private void TimerForAutoMessage_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!isChannelOnline())
                return;
            twitchClient.SendMessage(channelName, $"/me {FormateTags(channelName ,chatSettings.automessage)}");
        }

        private async void FollowChannel()
        {
            TwitchLib.Api.Core.Models.Undocumented.RecentEvents.Recent recent = new TwitchLib.Api.Core.Models.Undocumented.RecentEvents.Recent();
            await api.V5.Users.FollowChannelAsync(GetUserId(TwitchInfo.BotUserName).Result, GetUserId(channelName).Result);
        }

        private void TimerForUsersUpdate_Elapsed(object sender, ElapsedEventArgs e)
        {
            GetActiveUsersList();
        }

        private void TimerForCoins_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (!isChannelOnline())
                return;
            foreach (KeyValuePair<string, UserClass> player in users)
                if (player.Value.inventory.items.ContainsKey("coins"))
                    player.Value.inventory.items["coins"].count++;
                else
                    player.Value.inventory.items.Add("coins", new ItemClass(1, 100000000, 1));
            Console.WriteLine("Coins added!");
        }

        private bool isChannelOnline()
        {
            return api.V5.Streams.BroadcasterOnlineAsync(GetUserId(channelName).Result).Result;
        }

        private void Client_OnConnectionError(object sender, TwitchLib.Client.Events.OnConnectionErrorArgs e)
        {
            Console.WriteLine("");
            Console.WriteLine($"[{ channelName}] ERROR!! {e.Error}");
            Console.WriteLine("");
        }
        private void TwitchClient_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Console.WriteLine($"[{ channelName}] Disconnected.");
        }

        private void TwitchClient_OnChatCommandReceived(object sender, TwitchLib.Client.Events.OnChatCommandReceivedArgs e)
        {

            string command = e.Command.CommandText;
            command = command.ToLower();

            string arg = e.Command.ArgumentsAsString;
            arg = FormateNickname(arg);

            string nickname = FormateNickname(e.Command.ChatMessage.DisplayName);

            switch (command)
            {
                case "stone":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        if (chatSettings.isStoneActive)
                            StoneGame_End(nickname);
                        else
                            StoneGame_Start(nickname);
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "/me У тебя недостаточно прав, прости друг");
                    }
                    Save();
                    break;

                case "throw":
                    ThrowStone(e.Command.ChatMessage.Username, arg);
                    break;

                case "hellomod":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        chatSettings.useHelloMessages = !chatSettings.useHelloMessages;

                        //Console.WriteLine($"[{ channelName}] CHANGE HelloMod to:  " + chatSettings.useHelloMessages);
                        //Console.WriteLine("");
                        
                        if (chatSettings.useHelloMessages)
                            twitchClient.SendWhisper(nickname, "/me Режим приветствия включен");
                        else
                            twitchClient.SendWhisper(nickname, "/me Режим приветствия выключен");
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    Save();
                    break;

                case "hellodelay":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        command = command.Replace("!hellodelay ", "");
                        int newDelay;
                        if (!Int32.TryParse(command, out newDelay))
                            return;

                        chatSettings.helloTimerDelay = newDelay;

                        //Console.WriteLine($"[{ channelName}] CHANGE HelloDelay to: " + newDelay);
                        twitchClient.SendWhisper(nickname, $"/me Время ожидания, до повторного приветствия, установлено на {(float)newDelay / 60f} минут");
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    //Console.WriteLine("");
                    Save();
                    break;

                case "save":
                    
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        Save();
                        twitchClient.SendWhisper(nickname, "/me Сохранено.");
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    break;
                case "load":

                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        Load();
                        twitchClient.SendWhisper(nickname, "/me Загружено.");
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    break;
                case "health":
                    //Console.WriteLine($"[{ channelName}] {e.Command.ChatMessage.DisplayName} asking for Health");
                    UserClass player = GetPlayer(e.Command.ChatMessage.Username);
                    if (player != null)
                    {
                        if (player.curHealth != player.maxHealth)
                        {
                            //Console.WriteLine("hp != maxHp");
                            twitchClient.SendWhisper(nickname, $"/me В данный момент у тебя {player.curHealth} <3 .");
                        }
                        else
                        {
                            //Console.WriteLine("100 hp");
                            twitchClient.SendWhisper(nickname, $"/me Ты полностью здоров! У тебя {player.curHealth} <3 .");
                        }
                    }
                    else
                    {
                        //Console.WriteLine("player = null. Какого хуя?");
                    }
                    //Console.WriteLine("");
                    break;

                case "damage":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        int newDamage;
                        if (!Int32.TryParse(e.Command.ArgumentsAsString, out newDamage))
                            return;

                        chatSettings.stoneDamage = newDamage;
                        twitchClient.SendWhisper(nickname, $"/me Урон камнем установлен на {newDamage} HP.");
                        //Console.WriteLine($"[{ channelName}] Damage changed to {newDamage}");
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    //Console.WriteLine("");
                    Save();
                    break;

                case "searchtime":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        int newTime;
                        if (!Int32.TryParse(e.Command.ArgumentsAsString, out newTime))
                            return;

                        chatSettings.searchStoneTime = newTime;
                        twitchClient.SendWhisper(nickname, $"/me Время поиска камня установлено на {newTime} сек.");
                        //Console.WriteLine($"[{ channelName}] SearchTime changed to {newTime}");
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    //Console.WriteLine("");
                    Save();
                    break;
                case "regentime":
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        int newTime;
                        if (!Int32.TryParse(e.Command.ArgumentsAsString, out newTime))
                            return;

                        chatSettings.healthRegenTime = newTime;
                        twitchClient.SendWhisper(nickname, $"/me Время регенерации здоровья установлено на {newTime} сек.");
                        //Console.WriteLine($"[{ channelName}] RegenTime changed to {newTime}");
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    //Console.WriteLine("");
                    Save();
                    break;

                case "help":
                    //Console.WriteLine($"[{ channelName}] {e.Command.ChatMessage.DisplayName} asking for Help");
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.DisplayName, $"/me Список всех команд: https://vk.com/vi.soft?w=wall-169269770_3 OhMyDog");
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.DisplayName, $"/me Список доступных команд FoxxChatBot:{line()}!throw [nickname]{line()}!health{line()}!customCommandList{line()}");
                    }
                    //Console.WriteLine("");
                    break;

                case "addcommand":
                    //Console.WriteLine($"[{ channelName}] addcommand: {e.Command.ArgumentsAsList[0]}");
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        
                        if (e.Command.ArgumentsAsList.Count >= 2)
                        {

                            string feedback = "";
                            for (int i = 1; i < e.Command.ArgumentsAsList.Count; i++)
                                feedback += e.Command.ArgumentsAsList[i] + " ";

                            if (!chatSettings.customFeedback.ContainsKey(e.Command.ArgumentsAsList[0].ToLower()))
                            {
                                twitchClient.SendWhisper(nickname, $"/me Команда добавлена.");
                                chatSettings.customFeedback.Add(e.Command.ArgumentsAsList[0].ToLower(), feedback);
                            }
                            else
                                twitchClient.SendWhisper(nickname, $"/me Такая команда уже есть.");

                        }
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    Save();
                    //Console.WriteLine("");
                    break;

                case "list":
                    //Console.WriteLine($"[{ channelName}] Ask For Users List");
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        DebugUsersList();
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    //Console.WriteLine("");
                    break;

                case "addmoder":
                    //Console.WriteLine($"[{ channelName}] Adding new Modder");
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        string moder = FormateNickname(e.Command.ArgumentsAsString);
                        if (chatSettings.moderatorsList.Contains(moder))
                            twitchClient.SendWhisper(nickname, "Такой модератор уже добавлен!");
                        else
                        {
                            chatSettings.moderatorsList.Add(moder);
                            twitchClient.SendWhisper(nickname, "Модератор успешно добавлен!");
                        }

                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    Save();
                    //Console.WriteLine("");
                    break;

                case "removemoder":
                    //Console.WriteLine($"[{ channelName}] Remove Moder");
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        string moder = FormateNickname(e.Command.ArgumentsAsString);
                        if (!chatSettings.moderatorsList.Contains(moder))
                            twitchClient.SendWhisper(nickname, "Такого модератора нету в списке!");
                        else
                        {
                            chatSettings.moderatorsList.Remove(moder);
                            twitchClient.SendWhisper(nickname, "Модератор успешно удалён!");
                        }
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    Save();
                    //Console.WriteLine("");
                    break;

                case "removecommand":
                    //Console.WriteLine($"[{ channelName}] Try Remove Command");
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        if (e.Command.ArgumentsAsList.Count >= 1)
                        {

                            if (chatSettings.customFeedback.ContainsKey(e.Command.ArgumentsAsList[0]))
                            {
                                chatSettings.customFeedback.Remove(e.Command.ArgumentsAsList[0]);
                                twitchClient.SendWhisper(nickname, $"/me Команда удалена.");
                            }
                            else
                                twitchClient.SendWhisper(nickname, $"/me Такой команды нету в списке.");

                        }
                        else
                        {
                            twitchClient.SendWhisper(nickname, $"/me Впишите команду, которую хотите удалить.");
                        }
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    Save();
                    //Console.WriteLine("");
                    break;

                case "customcommandlist":
                    //Console.WriteLine($"[{ channelName}] Ask For Command List");
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        string commandsList = "";

                        foreach (KeyValuePair<string, string> feedbackMessage in chatSettings.customFeedback)
                        {
                            commandsList += feedbackMessage.Key + "/";
                        }

                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "Список команд с ответом от бота: " + commandsList);
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    //Console.WriteLine("");
                    break;

                case "moderlist":
                    //Console.WriteLine($"[{ channelName}] Ask For Moders List");
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        string modersList = "";

                        foreach (string moder in chatSettings.moderatorsList)
                        {
                            modersList += moder + "/";
                        }

                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "Список всех скрытых модераторов: " + modersList);
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    //Console.WriteLine("");
                    break;

                case "rule":
                    //Console.WriteLine($"[{ channelName}] Ask Stone Rule");
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        StoneGame_Rule();
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    //Console.WriteLine("");
                    break;

                case "use":

                    UserClass user = GetPlayer(e.Command.ChatMessage.Username);
                    if (user == null)
                        break;

                    if (user.inventory.items.ContainsKey(arg))
                    {
                        if(user.inventory.items[arg].count > 0)
                        {
                            user.inventory.items[arg].count--;
                            if (!string.IsNullOrEmpty(user.inventory.items[arg].itemDescription))
                            {
                                bool isPublicMessage = !user.inventory.items[arg].itemDescription.Contains("[w]");
                                string message = FormateTags(e.Command.ChatMessage.Username, user.inventory.items[arg].itemDescription);

                                if (isPublicMessage)
                                {
                                    twitchClient.SendMessage(channelName, $"/me " + message);
                                }
                                else
                                {
                                    twitchClient.SendWhisper(e.Command.ChatMessage.Username, $"/me " + message);
                                }
                            }
                            switch (arg)
                            {
                                case "аптечка":
                                    HealUser(e.Command.ChatMessage.DisplayName);
                                    break;

                                case "камень":
                                    ThrowStone(nickname, FormateTags(nickname, "[randomuser]"));
                                    break;

                                case "coins":
                                    string randomname = FormateTags(nickname, "[randomuser]");
                                    GiveItem(randomname, "coins", 1);
                                    twitchClient.SendMessage(channelName, $"/me @{e.Command.ChatMessage.DisplayName} отдаёт свою монетку случайному прохожему ({randomname}). Магазин: !store");
                                    break;
                            }
                        }
                        else
                        {
                            twitchClient.SendWhisper(nickname, $"/me @{e.Command.ChatMessage.DisplayName} у тебя нету такого предмета RuleFive . Инвентарь: !inventory");
                        }
                    }
                    else
                    {
                        twitchClient.SendWhisper(nickname, $"/me @{e.Command.ChatMessage.DisplayName} у тебя нету такого предмета RuleFive . Инвентарь: !inventory");
                    }
                    Save();
                    break;

                case "heal":
                    //Console.WriteLine($"[{ channelName}] try heal {arg}");
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        HealUser(arg);
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    Save();
                    //Console.WriteLine("");
                    break;

                case "give":
                    //Console.WriteLine($"[{ channelName}] try give item {arg}");
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {

                        List<string> args = e.Command.ArgumentsAsList;
                        int amount = 1;
                        if (args.Count == 3)
                            Int32.TryParse(args[2], out amount);

                        if (args[1].ToLower() == "coins" && e.Command.ChatMessage.Username != "pashafoxx")
                        {
                            twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                            return;
                        }
                        GiveItem(args[0], args[1], amount);
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    Save();
                    //Console.WriteLine("");
                    break;

                case "store":
                    Store(e.Command.ChatMessage.Username);
                    break;

                case "inventory":
                    Inventory(e.Command.ChatMessage.Username);
                    break;

                case "addstoreitem":
                    //Console.WriteLine($"[{ channelName}] try give item {arg}");
                    if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                    {
                        List<string> args = e.Command.ArgumentsAsList;
                        if (args.Count == 2)
                        {
                            int price;
                            if (Int32.TryParse(args[1], out price))
                                AddStoreItem(nickname, args[0], price, DefaultLimit(), "");
                        }
                        if (args.Count == 3)
                        {
                            int price = 0;
                            int limit = 0;
                            if (Int32.TryParse(args[1], out price) && Int32.TryParse(args[2], out limit))
                                AddStoreItem(nickname, args[0], price, limit, "");
                        }
                        if (args.Count >= 4)
                        {
                            int price = 0;
                            int limit = 0;
                            string description = e.Command.ArgumentsAsString;
                            description = description.Replace(arg[0] + "", "");
                            description = description.Replace(arg[1] + "", "");
                            description = description.Replace(arg[2] + "", "");
                            if (Int32.TryParse(args[1], out price) && Int32.TryParse(args[2], out limit))
                                AddStoreItem(nickname, args[0], price, limit, description);
                        }
                        Save();
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    //Console.WriteLine("");
                    break;

                case "buy":
                    List<string> arguments = e.Command.ArgumentsAsList;
                    if (arguments.Count == 1)
                    {
                        Buy(e.Command.ChatMessage.Username, arguments[0], 1);
                        Save();
                    }
                    if (arguments.Count == 2)
                    {
                        int count;
                        if (Int32.TryParse(arguments[1], out count))
                            Buy(e.Command.ChatMessage.Username, arguments[0], count);
                        Save();
                    }
                    break;

                case "sockettoken":
                    //Console.WriteLine($"[{ channelName}] try give item {arg}");
                    if (e.Command.ChatMessage.Username == "pashafoxx")
                    {
                        chatSettings.streamLabs_SocketToken = e.Command.ArgumentsAsString;
                        StreamLabsConnect();
                        Save();
                    }
                    else
                    {
                        twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                    }
                    //Console.WriteLine("");
                    break;
            }

            if (chatSettings.moderatorsCommands.Contains(e.Command.CommandText))
            {
                if (e.Command.ChatMessage.IsModerator || e.Command.ChatMessage.IsBroadcaster || chatSettings.moderatorsList.Contains(e.Command.ChatMessage.Username))
                {
                    string arguments = e.Command.ArgumentsAsString;
                    arguments = FormateNickname(arguments);

                    twitchClient.SendMessage(channelName, $"/{e.Command.CommandText} {arguments}");
                }
                else
                {
                    twitchClient.SendWhisper(e.Command.ChatMessage.Username, "У тебя недостаточно прав, прости друг");
                }
                return;
            }

            if (chatSettings.customFeedback.ContainsKey(e.Command.CommandText.ToLower()))
            {

                bool isPublic = !chatSettings.customFeedback[e.Command.CommandText.ToLower()].Contains("[w]");

                string message = FormateTags(e.Command.ChatMessage.DisplayName, chatSettings.customFeedback[e.Command.CommandText.ToLower()]);
                if (isPublic)
                    twitchClient.SendMessage(channelName, message);
                else
                    twitchClient.SendWhisper(e.Command.ChatMessage.Username, message);

                return;
            }
        }


        private void TwitchClient_OnWhisperReceived(object sender, TwitchLib.Client.Events.OnWhisperReceivedArgs e)
        {
            string[] arguments = (e.WhisperMessage.Message.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries));
            string nickname = e.WhisperMessage.Username;

            switch (arguments[0])
            {
                case "sockettoken":
                    if (nickname != channelName)
                        break;
                    if (Program.activeChatBots.ContainsKey(nickname))
                    {
                        Program.activeChatBots[nickname].chatSettings.streamLabs_SocketToken = arguments[1];
                        Program.activeChatBots[nickname].StreamLabsConnect();
                        Program.activeChatBots[nickname].Save();
                    }
                    break;
            }
        }

        private string FormateTags(string nickname ,string message)
        {
            message = message.Replace("[user]", nickname);
            message = message.Replace("[w]", "");
            if (chatUsers.Count != 0)
                message = message.Replace("[randomuser]", chatUsers[new Random().Next(0, chatUsers.Count - 1)]);
            UserClass user = GetPlayer(nickname.ToLower());
            if (user != null)
            {
                foreach (KeyValuePair<string, ItemClass> item in user.inventory.items)
                {
                    message = message.Replace($"[{item.Key}]", "" + item.Value.count);
                }
                foreach (KeyValuePair<string, ItemClass> item in chatSettings.store)
                {
                    if (user.inventory.items.ContainsKey(item.Key))
                        message = message.Replace($"[{item.Key}]", "" + user.inventory.items[item.Key].count);
                    else
                        message = message.Replace($"[{item.Key}]", "0");

                }
            }
            return message;
        }

        private void Buy(string nickname, string item, int amount)
        {
            item = item.ToLower();
            nickname = FormateNickname(nickname);
            UserClass user = GetPlayer(nickname.ToLower());
            if (user == null)
                return;
            if (!user.inventory.items.ContainsKey("coins"))
            {
                NoEnoughMoney(nickname);
                return;
            }
            if (!chatSettings.store.ContainsKey(item))
            {
                NoItemInStore(nickname, item);
                return;
            }
            if (user.inventory.items.ContainsKey(item))
            {
                if(user.inventory.items[item].count + amount > chatSettings.store[item].countLimit)
                {
                    ItemCountLimit(nickname, item);
                    return;
                }
            }
            else
            {
                if (amount > chatSettings.store[item].countLimit)
                {
                    ItemCountLimit(nickname, item);
                    return;
                }
            }

            if (user.inventory.items["coins"].count >= (chatSettings.store[item].price * amount))
            {
                user.inventory.items["coins"].count -= chatSettings.store[item].price * amount;
                GiveItem(nickname, item, amount, chatSettings.store[item].countLimit, chatSettings.store[item].itemDescription);
            }
            else
            {
                NoEnoughMoney(nickname);
            }

        }

        private void NoItemInStore(string nickname, string item)
        {
            twitchClient.SendWhisper(nickname, $"/me {FirstUpper(item)} не продаётся! Магазин: !store ");
        }

        private void ItemCountLimit(string nickname, string item)
        {
            twitchClient.SendWhisper(nickname, $"/me У {FirstUpper(item)} установлен лимит по количеству, вы не можете купить столько. Инвентарь: !inventory ");
        }

        private void NoEnoughMoney(string nickname)
        {
            twitchClient.SendWhisper(nickname, $"/me {nickname} у тебя недостаточно монеток! Жди или донать <3 ");
        }

        private void Store(string nickname)
        {
            nickname = FormateNickname(nickname);
            string message = "Магазин: ";

            foreach (KeyValuePair<string, ItemClass> item in chatSettings.store)
            {
                message += $" CurseLit {FirstUpper(item.Key)} ({item.Value.price}¢) {WhiteSpace()} ";
            }

            message += WhiteSpace()+ " Узнать свой баланс: !money. Для покупки предмета введите в чат: !buy [item]";

            twitchClient.SendWhisper(nickname.ToLower(), message);
        }
        private void AddStoreItem(string nickname, string item, int price, int countLimit, string description)
        {
            item = item.ToLower();
            if (!chatSettings.store.ContainsKey(item))
            {
                chatSettings.store.Add(item, new ItemClass(1, countLimit, price, description));
                twitchClient.SendWhisper(nickname, $"/me Товар добавлен! Магазин: !store");
            }
            else
            {
                twitchClient.SendWhisper(nickname, $"/me Товар уже добавлен! Магазин: !store");
            }
        }
        private void GiveItem(string nickname, string item, int amount, int amountlimit, string description)
        {
            nickname = FormateNickname(nickname);
            UserClass user = GetPlayer(nickname.ToLower());
                GiveItem(user, nickname, item, amount, amountlimit, description);
        }

        private string FormateNickname(string nickname)
        {
            nickname = nickname.ToLower();
            nickname = nickname.Replace("@", "");
            return nickname;
        }

        public void AutoMessageChange(string message)
        {
            chatSettings.automessage = message;
        }

        public void StealItem(string nickname, string item, int amount)
        {
            nickname = FormateNickname(nickname);
            UserClass user = GetPlayer(nickname.ToLower());
            if(user != null)
            {
                if (user.inventory.items.ContainsKey(item))
                {
                    if(user.inventory.items[item].count >= amount)
                    {
                        user.inventory.items[item].count -= amount;
                        //успешно украдено
                    }
                    else
                    {
                        //У игрока недостаточно пердметов
                    }
                }
                else
                {
                    //у игрока нету предмета
                }
            }
            else
            {
                //игрок не найден
            }
        }

        public void GiveItem(string nickname, string item, int amount)
        {
            nickname = FormateNickname(nickname);
            UserClass user = GetPlayer(nickname.ToLower());
            if (user != null)
            {
                int amountlimit;
                if (user.inventory.items.ContainsKey(item))
                    amountlimit = user.inventory.items[item].countLimit;
                else
                    amountlimit = DefaultLimit();
                if (item == "coins")
                    amountlimit = 100000000;
                GiveItem(user, nickname, item, amount, amountlimit, "");
            }
        }
        private void GiveItem(UserClass user, string nickname, string item, int amount, int amountlimit, string description)
        {
            nickname = FormateNickname(nickname);
            if (user != null)
            {
                if (user.inventory.items.ContainsKey(item))
                {
                    user.inventory.items[item].count += amount;
                }
                else
                {
                    int price = 0;
                    if (chatSettings.store.ContainsKey(item))
                        price = chatSettings.store[item].price;
                    user.inventory.items.Add(item, new ItemClass(amount, amountlimit, price, description));
                }
                twitchClient.SendWhisper(nickname, $"/me Ты получаешь - {amount} {item} BloodTrail . Инвентарь: !inventory");
            }
        }
        private void Inventory(string nickname)
        {
            nickname = FormateNickname(nickname);
            string message = "Инвентарь: ";
            UserClass user = GetPlayer(nickname.ToLower());
            if (user == null)
                return;

            foreach(KeyValuePair<string, ItemClass> item in user.inventory.items)
            {
                if (item.Key == "coins")
                    continue;
                message += $"TwitchLit {FirstUpper(item.Key)} ( {item.Value.count} / {item.Value.countLimit} ) {WhiteSpace()} ";
            }
            message += " Купи больше предметов в магазине: !store. Использовать предмет: !use [item]";
            twitchClient.SendWhisper(nickname.ToLower(), message);
        }

        public static string WhiteSpace()
        {
            return " ‏‏ ‏ ‏ ‏ ‏ ‏ ‏ ‏ ‏ ‏";
        }

        public void ThrowStone(string From, string To)
        {
            if (!chatSettings.isStoneActive)
            {
                twitchClient.SendWhisper(From, "/me В данный момент игра выключена.");
                return;
            }

            Console.WriteLine($"[{ channelName}] {From} try to throw stone in {To}");

            UserClass hunter = GetPlayer(From);
            UserClass prey = GetPlayer(To);

            if (hunter != null)
            {
                if (hunter.inventory.items["камень"].count <= 0)
                {
                    twitchClient.SendWhisper(From, $"/me У тебя нету камня для броска. CoolStoryBob");
                    return;
                }
            }
            else { return; }

            if (prey == null)
            {
                twitchClient.SendWhisper(From, $"/me Ты не можешь попасть в того, кого не видишь! NotLikeThis {To} отсутствует.");
                return;
            }

            hunter.inventory.items["камень"].count--;

            if (prey.curHealth == 0)
            {
                twitchClient.SendMessage(channelName, $"/me {From} кинул камень в {To}. Но {To} уже лежит в сточных водах, забитый камнями BibleThump");
                return;
            }

            if (To == channelName)
            {
                twitchClient.SendMessage(channelName, $"/me стример ловко уворачивается от булыжника OhMyDog");
                return;
            }

            if (isModer(To))
            {
                twitchClient.SendMessage(channelName, $"/me модератор {To}, останавливая камень одним мизинцем, произносит фразу 'OMAE WA MOU SHINDEIRU' PowerUpL DarkMode PowerUpR ");
                return;
            }

            Console.WriteLine($"[{ channelName}] Stone hits {To}");

            prey.curHealth -= chatSettings.stoneDamage;
            
            CreateRegenTimer(To);

            if (prey.curHealth <= 0)
            {
                prey.curHealth = 0;
                if (To != From)
                    twitchClient.SendMessage(channelName, $"/me {To} теряет сознание на {chatSettings.healthRegenTime} секунд. SoBayed");
                else
                    twitchClient.SendMessage(channelName, $"/me {From} теряет сознание на {chatSettings.healthRegenTime} сек. FailFish");

                TimeOutUser(To, chatSettings.healthRegenTime, "Потерял много крови.");
                //api.V5.Users.BlockUserAsync("", "", TwitchInfo.BotToken);
            }
            else
            {
                if (To != From)
                    twitchClient.SendMessage(channelName, $"/me {From} бросает камень в {To} Poooound и наносит {chatSettings.stoneDamage} урона. Проверка <3 : !health");
                else
                    twitchClient.SendMessage(channelName, $"/me {From} со всей силы бьётся головой о камень! FailFish Наносит себе {chatSettings.stoneDamage} урона. Проверка <3 : !health");
            }

            Console.WriteLine($"[{ channelName}] {To} HP = {prey.curHealth}");

            Console.WriteLine("");
            Save();
        }

        private void CreateRegenTimer(string nickname)
        {
            if (timerForRegenerate.ContainsKey(nickname))
                CloseRegenTimer(nickname);

            Timer _timer = new Timer();
            timerForRegenerate.Add(nickname, _timer);
            timerForRegenerate[nickname].Interval = chatSettings.healthRegenTime * 1000;
            timerForRegenerate[nickname].Elapsed += RegenerateTimerDone;
            timerForRegenerate[nickname].Start();
        }

        private void RegenerateTimerDone(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Try 'RegenerateTimerDone'");
            try
            {
                var keysWithMatchingValues = timerForRegenerate.Where(p => p.Value == (Timer)sender).Select(p => p.Key);

                foreach (var nickname in keysWithMatchingValues)
                {
                    UserClass user = GetPlayer(nickname);
                    if (user == null)
                        continue;
                    user.curHealth = user.maxHealth;
                    twitchClient.SendWhisper(nickname, $"Ты снова здоров.");
                    CloseRegenTimer(nickname);
                    break;
                }
            }
            catch { Console.WriteLine("Error in method 'RegenerateTimerDone'"); }
        }

        private void CloseRegenTimer(string user)
        {
            if (timerForRegenerate.ContainsKey(user))
            {
                timerForRegenerate[user].Close();
                timerForRegenerate.Remove(user);
                if (!timerForRegenerate.ContainsKey(user))
                    Console.WriteLine("RegenTimer removed successfully!");
            }
        }

        private void HealUser(string nickname)
        {
            nickname = FormateNickname(nickname);
            UserClass user = GetPlayer(nickname.ToLower());
            if (user != null)
            {
                user.curHealth = user.maxHealth;
                CloseRegenTimer(nickname);
                twitchClient.SendWhisper(nickname, $"/me Ты полностью исцеляешься BlessRNG");
            }
            else
            {
               // twitchClient.SendMessage(channelName, $"/me Кто то пытался вылечить не существующего {nickname} DansGame");
            }
        }
        private void TwitchClient_OnMessageReceived(object sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            if (chatSettings.useHelloMessages)
            {
                string message = e.ChatMessage.Message;
                message = message.ToLower();
                string user = e.ChatMessage.DisplayName;
                if (!timerForHelloMessage.ContainsKey(user.ToLower()))
                {
                    UserClass player = GetPlayer(e.ChatMessage.Username);

                    if (player != null)
                    {
                        if (isHelloMessage(message))
                        {
                            Console.WriteLine($"[{ channelName}] Hello For @" + user);
                            twitchClient.SendMessage(channelName, GetRandomHelloMessage() + " @" + user);
                            if (player != null)
                                CreateHelloTimer(user.ToLower());
                        }
                    }
                }
            }
        }

        private void CreateHelloTimer(string nickname)
        {
            if (timerForHelloMessage.ContainsKey(nickname))
                CloseHelloTimer(nickname);
            Timer _timer = new Timer();
            timerForHelloMessage.Add(nickname, _timer);
            timerForHelloMessage[nickname].Interval = chatSettings.helloTimerDelay * 1000;
            timerForHelloMessage[nickname].Elapsed += HelloTimerDone;
            timerForHelloMessage[nickname].Start();
            
        }

        private void CloseHelloTimer(string user)
        {
            if (timerForHelloMessage.ContainsKey(user))
            {
                timerForHelloMessage[user].Close();
                timerForHelloMessage.Remove(user);
            }
        }

        private void HelloTimerDone(object sender, ElapsedEventArgs e)
        {
            try
            {
                var keysWithMatchingValues = timerForHelloMessage.Where(p => p.Value == (Timer)sender).Select(p => p.Key);

                foreach (var nickname in keysWithMatchingValues)
                {
                    CloseHelloTimer(nickname);
                    break;
                }
            }
            catch { Console.WriteLine("Error in method 'HelloTimerDone'"); }
        }

        private void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            //Console.WriteLine(e.Data);
        }

        #endregion

        #region Stone Game
        private void StoneGame_Start(string nickname)
        {
            Console.WriteLine($"[{ channelName}] Запуск интерактива STONE");
            Console.WriteLine("");
            twitchClient.SendWhisper(nickname, "Игра Началась!");
            //StoneGame_Rule();
            chatSettings.isStoneActive = true;
        }
        private void StoneGame_Rule()
        {
            twitchClient.SendMessage(channelName, $"/me " +
                $"(つ◉益◉)つcxxx[]:::::::::::> STONE JUDGE! " +
                $"Цель заключается в том, чтобы найти в чате муд@ка и закидать его камнями. " +
                $"Камень вы можете купить в магазине (!store). " +
                $"Написав в чате команду: !throw [username], вы бросите камень в человека который вам не нравится. " +
                $"Вы потеряете камень, а он {chatSettings.stoneDamage} из 100 HP. " +
                $"При 0 HP поверженный уходит во временный бан на {chatSettings.healthRegenTime} сек. " +
                $"●▬▬▬▬▬▬▬▬▬▬๑۩۩๑▬▬▬▬▬▬▬▬▬▬▬●");
        }
        private void StoneGame_End(string nickname)
        {
            Console.WriteLine($"[{ channelName}] Остановка интерактива STONE");
            Console.WriteLine("");
            twitchClient.SendWhisper(nickname, "Игра Остановлена!");
            chatSettings.isStoneActive = false;
        }
        #endregion

        private bool isHelloMessage(string message)
        {
            for (int i = 0; i < chatSettings.helloExample.Count; i++)
            {
                if (message.Contains(chatSettings.helloExample[i]))
                    return true;
            }

            return false;
        }
        private string GetRandomHelloMessage()
        {
            Random random = new Random();
            string message = chatSettings.helloMessages[random.Next(chatSettings.helloMessages.Length)];
            if (message == previousHelloMessage)
                return GetRandomHelloMessage();
            previousHelloMessage = message;
            return message;
        }

        private UserClass GetPlayer(string user)
        {
            if (user == null)
                return null;

            if (!isUserActive(user))
                return null;
            else
                AddPlayer(user);
            
            return users[user];
        }
        private UserClass AddPlayer(string user)
        {
           //if (users.ContainsKey(user))
             //   return users[user];
            UserClass newPlayer = new UserClass();
            if (!users.ContainsKey(user))
            {
                newPlayer.inventory.items.Add("coins", new ItemClass(1, 100000000, 1));
                newPlayer.inventory.items.Add("камень", new ItemClass(1, 1, 300));
                newPlayer.inventory.items.Add("аптечка", new ItemClass(1, 5, 150));
                users.Add(user, newPlayer);
                Console.WriteLine($"[{ channelName}] User Added: " + user);
            }

            return users[user];
        }
        private bool isUserActive(string nickname)
        {
            if (chatUsers.Contains(nickname))
                return true;
            else
            {
                return GetActiveUsersList().Contains(nickname);
            }
        }
        private bool isModer(string nickname)
        {
            return chatModers.Contains(nickname);
        }

        private async Task<string> GetUserId(string nickname)
        {
            Users users = await api.V5.Users.GetUserByNameAsync(nickname);
            //Console.WriteLine($"[{ channelName}] User: {nickname},ID: {users.Matches[0].Id}");
            return users.Matches[0].Id;
        }

        private void TimeOutUser(string nickname, int time, string reason)
        {
            twitchClient.SendMessage(channelName, $"/timeout {nickname} {time}");
        }

        private List<string> GetActiveUsersList()
        {
            //Console.WriteLine("Users list loading...");
            chatUsers = new List<string>();
            chatModers = new List<string>();
            string url = $"http://tmi.twitch.tv/group/user/{channelName}/chatters";
            string response;
            using (var webClient = new WebClient())
            {
                response = webClient.DownloadString(url);
            }

            ChattersClass chattersTemp = new JavaScriptSerializer().Deserialize<ChattersClass>(response);

            chatUsers.AddRange(chattersTemp.chatters.admins);
            chatUsers.AddRange(chattersTemp.chatters.global_mods);
            chatUsers.AddRange(chattersTemp.chatters.moderators);
            chatUsers.AddRange(chattersTemp.chatters.staff);
            chatUsers.AddRange(chattersTemp.chatters.viewers);
            chatUsers.AddRange(chattersTemp.chatters.vips);
            /*
            List<TwitchLib.Api.Core.Models.Undocumented.Chatters.ChatterFormatted> chatters = await api.Undocumented.GetChattersAsync(channelName);
            List<string> chattersList = new List<string>();
            for (int i = 0; i < chatters.Count; i++)
                chattersList.Add(chatters[i].Username);
            */

           // Console.WriteLine("Done list loading.");
           // Console.WriteLine("Begin users loading...");
            for (int i = 0; i < chatUsers.Count; i++)
            {
                AddPlayer(chatUsers[i]);
            }
             //   Console.WriteLine("Users loading done.");
            Save();
            return chatUsers;
        }

        private void DebugUsersList()
        {
            GetActiveUsersList();
            for (int i = 0; i < chatUsers.Count; i++)
                Console.WriteLine(chatUsers[i]);
        }
        private string line()
        {
            return " / ";
            //return " ‏‏ ‏ ‏ ‏ ‏ ‏ ‏ ‏ ‏ ‏";
            //return System.Environment.NewLine;
        }

        internal void Donation(string from, string Amount, string Currency, string FormattedAmount, string message)
        {
            Amount = Amount.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries)[0];
            float reward = float.Parse(Amount) * chatSettings.rewardPerRub;
            Currency = Currency.ToLower();

            if (Currency == "usd")
                reward *= 65;
            if (Currency == "eur")
                reward *= 74;

            twitchClient.SendMessage(channelName, $"/me {from} задонатил {FormattedAmount} GayPride .");
            GiveItem(from.ToLower(), "coins", (int)reward);
        }

        private int DefaultLimit()
        {
            return 100;
        }

        private string FirstUpper(string str)
        {
            return str.Substring(0, 1).ToUpper() + (str.Length > 1 ? str.Substring(1) : "");
        }

        #region IDisposable Support
        private bool disposedValue = false; // Для определения избыточных вызовов

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты).
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить ниже метод завершения.
                // TODO: задать большим полям значение NULL.

                disposedValue = true;
            }
        }

        // TODO: переопределить метод завершения, только если Dispose(bool disposing) выше включает код для освобождения неуправляемых ресурсов.
        // ~TwitchChatBot() {
        //   // Не изменяйте этот код. Разместите код очистки выше, в методе Dispose(bool disposing).
        //   Dispose(false);
        // }

        // Этот код добавлен для правильной реализации шаблона высвобождаемого класса.
        public void Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки выше, в методе Dispose(bool disposing).
            Dispose(true);
            // TODO: раскомментировать следующую строку, если метод завершения переопределен выше.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}