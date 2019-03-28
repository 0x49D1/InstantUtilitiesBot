using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using UtilitiesBot.Properties;
using UtilitiesBot.Utilities;
using System.Threading;

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
            //Debugger.Break();
        }

        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received choosen inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
            logger.Info($"Received choosen inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message == null || message.Type != MessageType.TextMessage) return;
            try
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
            }
            catch (Exception ex)
            {
                logger.Error(ex);// ignore telegram exceptions.
                try
                {
                    await Bot.SendTextMessageAsync(message.Chat.Id, "OOps. Smth went wrong");
                }
                catch (Exception e)
                {
                    logger.Error(e);
                    // ignore for now
                }
            }

            string msg = message.Text;
            bool disableMessagePreview = false;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Chat: " + message.Chat.Id + ", Message: " + msg);
            Console.ForegroundColor = ConsoleColor.White;
            logger.Info("Chat: " + message.Chat.Id + ", Message: " + msg);

            if (!msg.StartsWith("/"))
                msg = "/ddg " + msg.TrimStart(); // Default command is DDG

            string resMessage = "Nothing found for command. Try /help";
            IInstantAnswer instantAnswer = null;
            try
            {
                if (msg.StartsWithOrdinalIgnoreCase("/blockchair"))
                {
                    var value = msg.RemoveCommandPart().Trim();
                    resMessage = "https://blockchair.com/search?q=" + value;
                }
                if (msg.StartsWithOrdinalIgnoreCase("/blockchaininfo;/blockchain;/btcinfo"))
                {
                    var value = msg.RemoveCommandPart().Trim();
                    resMessage = "https://blockchain.info/search/" + value;

                    //https://blockchain.info/api/blockchain_api
                    var httpClient = new HttpClient();
                    var response =
                        await
                            httpClient.GetAsync(value.Length > 36
                                ? ("https://blockchain.info/rawtx/" + value)
                                : ("https://blockchain.info/address/" + value + "?format=json"));
                    var resp = await response.Content.ReadAsStringAsync();
                    if (resp.Length > 4000)
                        resp = resp.Substring(0, 4000) + ".....";
                    resMessage += "\n" + resp;
                }
                if (msg.StartsWithOrdinalIgnoreCase("/tobase64;/base64encode"))
                {
                    var value = msg.RemoveCommandPart().Trim();
                    var bytes = Encoding.UTF8.GetBytes(value);
                    resMessage = Convert.ToBase64String(bytes);
                }
                if (msg.StartsWithOrdinalIgnoreCase("/frombase64;/base64decode"))
                {
                    var value = msg.RemoveCommandPart().Trim();
                    resMessage = Encoding.UTF8.GetString(Convert.FromBase64String(value));
                }
                if (msg.StartsWithOrdinalIgnoreCase("/strlen"))
                {
                    resMessage = msg.RemoveCommandPart().Trim().Length.ToString();
                }
                if (msg.StartsWithOrdinalIgnoreCase("/guid"))
                {
                    resMessage = Guid.NewGuid().ToString();
                }
                if (msg.StartsWithOrdinalIgnoreCase("/encodeurl;/urlencode"))
                {
                    string value = msg.RemoveCommandPart().Trim();
                    resMessage = HttpUtility.UrlEncode(value);
                }
                if (msg.StartsWithOrdinalIgnoreCase("/decodeurl;/urldecode"))
                {
                    string value = msg.RemoveCommandPart().Trim();
                    resMessage = HttpUtility.UrlDecode(value);
                }
                if (msg.StartsWithOrdinalIgnoreCase("/tounixtime;/toepoch"))
                {
                    instantAnswer = new UnixTimeStampInstantAnswer();
                }
                if (msg.StartsWithOrdinalIgnoreCase("/fromunixtime;/fromepoch"))
                {
                    instantAnswer = new EpochConverterInstantAnswer();
                }
                if (msg.StartsWithOrdinalIgnoreCase("/formatjson;/jsonformat"))
                {
                    try
                    {
                        bool wasEscaped = false;

                        string value = msg.RemoveCommandPart().Trim();
                        if (value.Contains("\\\""))
                            wasEscaped = true;
                        value = value.Replace("\\\"", "\"");
                        dynamic parsedJson = JsonConvert.DeserializeObject(value);

                        resMessage = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                        // todo needs refactor
                        if (wasEscaped)
                            resMessage = resMessage.Replace("\"", "\\\"");
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        resMessage = "Probably JSON was not correct";
                    }
                }
                if (msg.StartsWithOrdinalIgnoreCase("/iplocation;/geolocation;/ip"))
                {

                    string ipCheckSource = "http://ip-api.com/json/";
                    string value = msg.RemoveCommandPart().Trim();
                    if (string.IsNullOrEmpty(value))
                        resMessage = "You can check your ip here: http://ipinfo.io/ip";
                    else
                    {

                        var ips = Regex.Matches(value, @"(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)").Cast<Match>().Select(m => m.Value).ToArray();
                        if (ips.Length == 0)
                            resMessage = "Wrong ip format!";
                        else
                        {
                            resMessage = "";
                            foreach (var ipvalue in ips)
                            {
                                Console.WriteLine("Checking " + ipvalue);

                                //https://ip-api.com/json/$ip
                                var httpClient = new HttpClient();
                                var response = httpClient.GetAsync(ipCheckSource + ipvalue).Result;
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
                                    resMessage += "Try count exceeded, retry later. http://ip-api.com/json/" + ipvalue + "\n";
                                else
                                {
                                    string content = response.Content.ReadAsStringAsync().Result;
                                    dynamic parsedJson = JsonConvert.DeserializeObject(content);
                                    resMessage += JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
                                    JObject jo = JObject.Parse(content);
                                    if (jo == null)
                                        continue;
                                    string country = jo.SelectToken("countryCode")?.ToString() ?? jo.SelectToken("country_code")?.ToString();

                                    if (!string.IsNullOrEmpty(country))
                                        resMessage += "\nhttp://icons.iconarchive.com/icons/famfamfam/flag/16/" +
                                                      country.ToLower() + "-icon.png\n";
                                }
                                try
                                {
                                    Thread.Sleep(400);
                                    await Bot.SendTextMessageAsync(message.Chat.Id, resMessage, disableWebPagePreview: disableMessagePreview);

                                    resMessage = "";
                                }
                                catch (Exception ex)
                                {
                                    if (ex.ToString().Contains("Too Many Requests"))
                                        ipCheckSource = "https://freegeoip.net/json/";

                                    Console.WriteLine(string.Format("{0} {1} exception: {2}", resMessage, ipvalue, ex));
                                    try
                                    {
                                        await Bot.SendTextMessageAsync(message.Chat.Id, "Something went wrong on " + resMessage, disableWebPagePreview: disableMessagePreview);
                                    }
                                    catch (Exception) { }

                                    resMessage = "";
                                }
                            }
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
                        var response =
                            await httpClient.GetAsync("https://api.duckduckgo.com/?q=" + value + "&format=json");
                        string content = await response.Content.ReadAsStringAsync();
                        JObject jo = JObject.Parse(content);
                        string answer = Regex.Replace(jo.SelectToken("Answer").ToString(), @"<[^>]*>", String.Empty);
                        if (string.IsNullOrEmpty(answer) || answer.Contains(" IP ")) // Your IP address is xxx in xxx
                        {
                            string moreAnswer = jo.SelectToken("RelatedTopics").Any()
                                ? jo.SelectToken("RelatedTopics")[0]["Result"].ToString()
                                : "";
                            if (!string.IsNullOrEmpty(moreAnswer) && moreAnswer.Contains("</a>") &&
                                moreAnswer.IndexOf("</a>", StringComparison.OrdinalIgnoreCase) + 4 < moreAnswer.Length)
                            {
                                moreAnswer =
                                    moreAnswer.Substring(
                                        moreAnswer.IndexOf("</a>", StringComparison.OrdinalIgnoreCase) + 4);
                                if (!string.IsNullOrEmpty(moreAnswer))
                                    resMessage = moreAnswer + "\n" + jo.SelectToken("RelatedTopics")[0]["Icon"]["URL"] +
                                                 "\n" + "See: https://duckduckgo.com/?q=" + value;
                            }
                            else
                            {
                                disableMessagePreview = true;
                                resMessage =
                                    "Instant not found. Try the following multi searches:\nGoogle: https://google.com/search?q=" +
                                    value +
                                    "\nDuckduckgo: https://duckduckgo.com/?q=" + value +
                                    "\nYandex: https://yandex.ru/search/?text=" + value +
                                    "\nGitHub: https://github.com/search?utf8=%E2%9C%93&q=" + value +
                                    "\nWikipedia: https://en.wikipedia.org/wiki/Special:Search?search=" + value +
                                    "\nWolframAlpha: https://www.wolframalpha.com/input/?i=" + value;
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
/ip - Information about selected ip address (location, etc). But feel free to mess IP with texts or send LIST of IPs. Example /ip xxx.xxx.xxx.xxx
/formatjson - Reformats provided JSON string into pretty idented string
/blockchain - Gets link to check bitcoin address/transaction information on blockchain.info
/blockchair - Just generate link to search blockchair.com
/tounixtime - Convert datetime to unixtimestamp. Message must be like in format: dd.MM.yyyy HH:mm:ss 01.09.1980 06:32:32. Or just text 'now'
/fromunixtime - Converts back from epoch timestamp to datetime string
/tobase64 - Encode to base64
/frombase64 - Decode from base64
/hash - Calculate hash. Use like this: /hash sha256 test
/urlencode - URL-encodes a string and returns the encoded string
/urldecode - Decodes URL-encoded string
/guid - Generate Global Unique Identifier
/strlen - Returns length of the provided string
";
                }

                if (instantAnswer != null)
                    resMessage = instantAnswer.GetInstantAnswer(msg.RemoveCommandPart().Trim());
            }
            catch (Exception globalEx)
            {
                logger.Error(globalEx); // Ignore any global exception
                Console.Write(globalEx.ToString());
                resMessage = "Something went wrong, try another request.";
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(resMessage);
            Console.ForegroundColor = ConsoleColor.White;
            logger.Info(resMessage);

            // Remove sensitive information from resMessage
            foreach (var tt in textsToExclude)
            {
                resMessage = resMessage.Replace(tt, "xx");
            }
            try
            {
                if (!string.IsNullOrEmpty(resMessage))
                    await Bot.SendTextMessageAsync(message.Chat.Id, resMessage, disableWebPagePreview: disableMessagePreview);
            }
            catch (Exception ex)
            {
                logger.Error(ex);// ignore telegram exceptions.
                try
                {
                    await Bot.SendTextMessageAsync(message.Chat.Id, "OOps. Smth went wrong");
                }
                catch (Exception e)
                {
                    logger.Error(e);
                    //Ignore this one too
                }
            }
            try
            {
                var httpClient = new HttpClient();
                var content = new StringContent("{'command':'" + msg.RemoveCommandPart() + "'}", Encoding.UTF8, "application/json");
                await httpClient.PostAsync("https://api.botan.io/track?token=" + Settings.Default.BotanIoKey + "&uid=UID&name=" + msg.GetCommandPart(), content);
            }
            catch (Exception ex)
            {
                logger.Error(ex);// ignore errors
            }
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id,
                $"Received {callbackQueryEventArgs.CallbackQuery.Data}");
        }


    }
}
