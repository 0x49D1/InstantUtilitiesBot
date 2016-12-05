using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using UtilitiesBot.Properties;
using UtilitiesBot.Utilities;

namespace UtilitiesBot
{
    class Program
    {
        private static readonly TelegramBotClient Bot = new TelegramBotClient(Settings.Default.ApiKey);
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
        private static MemoryCache cache = MemoryCache.Default;
        private static int locationTryCount = 0;
        private static List<string> textsToExclude = new List<string>();

        static void Main(string[] args)
        {
            string serviceIp = Utilities.Utilities.GetExternalIp();
            logger.Trace("Service ip is " + serviceIp);
            textsToExclude.Add(serviceIp);
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            //Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            Bot.OnReceiveError += BotOnReceiveError;


            var me = Bot.GetMeAsync().Result;

            Console.Title = me.Username;

            Bot.StartReceiving();
            logger.Trace("Utilities bot starts listening");
            Console.ReadLine();
            Bot.StopReceiving();
        }

        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            logger.Error(receiveErrorEventArgs.ApiRequestException.ToJson());
            Debugger.Break();
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received choosen inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message == null || message.Type != MessageType.TextMessage) return;

            await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
            string msg = message.Text;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Chat: " + message.Chat.Id + ", Message: " + msg);
            Console.ForegroundColor = ConsoleColor.White;

            if (!msg.StartsWith("/"))
                msg = "/ddg " + msg.TrimStart(); // Default command is DDG

            string resMessage = "Nothing found for command. Try /help";
            IInstantAnswer instantAnswer = null;
            if (msg.StartsWithOrdinalIgnoreCase("/tounixtime;/toepoch"))
            {
                instantAnswer = new UnixTimeStampInstantAnswer();
            }
            if (msg.StartsWithOrdinalIgnoreCase("/formatjson;/jsonformat"))
            {
                string value = HttpUtility.UrlEncode(msg.RemoveCommandPart().Trim());
                // todo add method to reformat passed JSON with identations and so on
            }
            if (msg.StartsWithOrdinalIgnoreCase("/iplocation;/geolocation;/ip"))
            {
                string value = HttpUtility.UrlEncode(msg.RemoveCommandPart().Trim());

                if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"))
                    resMessage = "Wrong ip format!";
                else if (string.IsNullOrEmpty(value))
                    resMessage = "You can check your ip here: https://ipinfo.io/ip";
                else
                {
                    //http://ip-api.com/json/$ip
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync("http://ip-api.com/json/" + value);
                    bool? timeout = cache.Get("iplocationtrytimeout") as bool?; // only 150 per minute allowed
                    if (timeout != null)
                        locationTryCount++;
                    else
                    {
                        locationTryCount = 0;
                        cache.Set("iplocationtrytimeout", true,
                            new CacheItemPolicy() { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(1) });
                    }
                    if (locationTryCount > 100)
                        resMessage = "Try count exceeded, retry later";
                    else
                    {
                        string content = await response.Content.ReadAsStringAsync();
                        dynamic parsedJson = JsonConvert.DeserializeObject(content);
                        resMessage = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                        JObject jo = JObject.Parse(content);
                        string country = jo.SelectToken("countryCode").ToString();
                        if (!string.IsNullOrEmpty(country))
                            resMessage += "\nhttp://icons.iconarchive.com/icons/famfamfam/flag/16/" + country.ToLower() +
                                          "-icon.png";
                    }
                }
            }
            if (msg.StartsWithOrdinalIgnoreCase("/ddg;/duckduckgo;/duckduckgoinstant"))
            {
                string value = HttpUtility.UrlEncode(msg.RemoveCommandPart().Trim());
                if (!string.IsNullOrEmpty(value))
                {
                    //http://api.duckduckgo.com/?q=14ml%20in%20litre&format=json
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync("https://api.duckduckgo.com/?q=" + value + "&format=json");
                    string content = await response.Content.ReadAsStringAsync();
                    JObject jo = JObject.Parse(content);
                    string answer = Regex.Replace(jo.SelectToken("Answer").ToString(), @"<[^>]*>", String.Empty);
                    if (string.IsNullOrEmpty(answer) || answer.Contains(" IP "))// Your IP address is xxx in xxx
                    {
                        string moreAnswer = jo.SelectToken("RelatedTopics").Any() ? jo.SelectToken("RelatedTopics")[0]["Result"].ToString() : "";
                        if (!string.IsNullOrEmpty(moreAnswer) && moreAnswer.Contains("</a>") && moreAnswer.IndexOf("</a>", StringComparison.OrdinalIgnoreCase) + 4 < moreAnswer.Length)
                        {
                            moreAnswer = moreAnswer.Substring(moreAnswer.IndexOf("</a>", StringComparison.OrdinalIgnoreCase) + 4);
                            if (!string.IsNullOrEmpty(moreAnswer))
                                resMessage = moreAnswer + "\n" + jo.SelectToken("RelatedTopics")[0]["Icon"]["URL"] +
                                             "\n" + "See: https://duckduckgo.com/?q=" + value;
                        }
                        else
                        {
                            // todo remove link preview for such messages
                            resMessage =
                                "Instant not found. Try the following multi searches:\nGoogle: https://google.com/search?q=" + value +
                                "\nDuckduckgo: https://duckduckgo.com/?q=" + value +
                                "\nYandex: https://yandex.ru/search/?text=" + value +
                                "\nGitHub: https://github.com/search?utf8=%E2%9C%93&q=" + value +
                                "\nWikipedia: https://en.wikipedia.org/wiki/Special:Search?search=" + value+                                
                                "\nWolframAlpha: https://www.wolframalpha.com/input/?i="+value;
                        }
                    }
                    else
                        resMessage = answer;
                }
            }
            if (msg.StartsWithOrdinalIgnoreCase("/hash"))
            {
                instantAnswer = new HashCalculatorInstantAnswer();
            }
            else if (message.Text.StartsWith("/start") || message.Text.StartsWith("/help"))
            {
                resMessage = @"Usage:
Default command is /ddg
/help - Shows all the commands with examples for some of them
/ddg - Instant answers from duckduckgo.com. Example: /ddg 15km to miles
/ip - Information about selected ip address (location, etc)
/tounixtime - Convert datetime to unixtimestamp. Message must be like in format: dd.MM.yyyy HH:mm:sss 01.09.1980 06:32:32. Or just text 'now'
/toepoch - Calculate unix timestamp for date in format dd.MM.yyyy HH:mm:ss
/hash - Calculate hash. Use like this: /hash sha256 test
";
            }

            if (instantAnswer != null)
                resMessage = instantAnswer.GetInstantAnswer(msg.RemoveCommandPart().Trim());

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(resMessage);
            Console.ForegroundColor = ConsoleColor.White;

            // Remove sensitive information from resMessage
            foreach (var tt in textsToExclude)
            {
                resMessage = resMessage.Replace(tt, "xx");
            }

            await Bot.SendTextMessageAsync(message.Chat.Id, resMessage);
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id,
                $"Received {callbackQueryEventArgs.CallbackQuery.Data}");
        }


    }
}
