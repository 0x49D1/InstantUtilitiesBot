using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using utilitiesBotDotCore.Utilities;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using System.Text;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using utilitiesBotDotCore.Models;
using Microsoft.Extensions.Logging;

namespace utilitiesBotDotCore.Services
{
    public interface IService
    {
        void Run();
    }

    public class Service : IService
    {

        private TelegramBotClient _bot;
        private CacheManager.Core.ICacheManager<object> _cache;
        private int locationTryCount = 0;
        private UtilitiesBotSettings _settings;
        private List<string> textsToExclude = new List<string>();
        Microsoft.Extensions.Logging.ILogger _logger;
        public Service(IOptions<UtilitiesBotSettings> _option, ILogger<IService> logger)
        {
            using(logger.BeginScope("init"))
            try
            {
                _logger = logger;
                _settings = _option.Value;
                _bot = new TelegramBotClient(_settings.ApiKey);
                _cache = CacheManager.Core.CacheFactory.Build("botCache", p => p.WithDictionaryHandle());
            }
            catch(Exception ex)
            {
                logger.LogError(ex.ToString());
                throw;
            }
        }

        public void Run()
        {
            string serviceIp = Utilities.Utilities.GetExternalIp().Result;
            _logger.LogInformation("Service ip is " + serviceIp);
            textsToExclude.Add(serviceIp);
            _bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            _bot.OnMessage += BotOnMessageReceived;
            _bot.OnMessageEdited += BotOnMessageReceived;
            //Bot.OnInlineQuery += BotOnInlineQueryReceived;
            _bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            _bot.OnReceiveError += BotOnReceiveError;


            var me = _bot.GetMeAsync().Result;

            Console.Title = me.Username;

            _bot.StartReceiving();
            _logger.LogInformation("Utilities bot starts listening");
            Console.ReadLine();
            _bot.StopReceiving();
        }




        private void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            _logger.LogError(receiveErrorEventArgs.ApiRequestException.ToJson());
            //Debugger.Break();
        }

        private void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received choosen inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }

        private async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;

            if (message == null || message.Type != MessageType.TextMessage) return;

            await _bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);
            string msg = message.Text;
            bool disableMessagePreview = false;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Chat: " + message.Chat.Id + ", Message: " + msg);
            Console.ForegroundColor = ConsoleColor.White;

            if (!msg.StartsWith("/"))
                msg = "/ddg " + msg.TrimStart(); // Default command is DDG

            string resMessage = "Nothing found for command. Try /help";
            IInstantAnswer instantAnswer = null;
            try
            {
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
                    resMessage = WebUtility.UrlEncode(value);
                }
                if (msg.StartsWithOrdinalIgnoreCase("/decodeurl;/urldecode"))
                {
                    string value = msg.RemoveCommandPart().Trim();
                    resMessage = WebUtility.UrlDecode(value);
                }
                if (msg.StartsWithOrdinalIgnoreCase("/tounixtime;/toepoch"))
                {
                    instantAnswer = new UnixTimeStampInstantAnswer(_logger);
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
                        _logger.LogError(ex.ToString());
                        resMessage = "Probably JSON was not correct";
                    }
                }
                if (msg.StartsWithOrdinalIgnoreCase("/iplocation;/geolocation;/ip"))
                {
                    string value = WebUtility.UrlEncode(msg.RemoveCommandPart().Trim());

                    if (!string.IsNullOrEmpty(value) &&
                        !Regex.IsMatch(value,
                            @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$"))
                        resMessage = "Wrong ip format!";
                    else if (string.IsNullOrEmpty(value))
                        resMessage = "You can check your ip here: https://ipinfo.io/ip";
                    else
                    {
                        //http://ip-api.com/json/$ip
                        var httpClient = new HttpClient();
                        var response = await httpClient.GetAsync("http://ip-api.com/json/" + value);
                        bool? timeout = _cache.Get("iplocationtrytimeout") as bool?; // only 150 per minute allowed
                        if (timeout != null)
                            locationTryCount++;
                        else
                        {
                            locationTryCount = 0;
                            _cache.Put("iplocationtrytimeout", true, TimeSpan.FromMinutes(1));
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
                                resMessage += "\nhttp://icons.iconarchive.com/icons/famfamfam/flag/16/" +
                                              country.ToLower() + "-icon.png";
                        }
                    }
                }
                if (msg.StartsWithOrdinalIgnoreCase("/ddg;/duckduckgo;/duckduckgoinstant"))
                {
                    string value = WebUtility.UrlEncode(msg.RemoveCommandPart().Trim());
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
                    instantAnswer = new HashCalculatorInstantAnswer(_logger);
                }
                else if (message.Text.StartsWith("/start") || message.Text.StartsWith("/help"))
                {
                    resMessage = @"Usage:
Default command is /ddg
/help - Shows all the commands with examples for some of them
/ddg - Instant answers from duckduckgo.com. Example: /ddg 15km to miles
/ip - Information about selected ip address (location, etc). Example /ip xxx.xxx.xxx.xxx
/formatjson - Reformats provided JSON string into pretty idented string
/blockchain - Gets link to check bitcoin address/transaction information on blockchain.info
/tounixtime - Convert datetime to unixtimestamp. Message must be like in format: dd.MM.yyyy HH:mm:ss 01.09.1980 06:32:32. Or just text 'now'
/tobase64 - Encode to base64
/frombase64 - Decode from base64
/hash - Calculate hash. Use like this: /hash sha256 test
/urlencode - URL-encodes a string and returns the encoded string.
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
                _logger.LogError(globalEx.ToString()); // Ignore any global exception
                resMessage = "Something went wrong, try another request.";
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(resMessage);
            Console.ForegroundColor = ConsoleColor.White;

            // Remove sensitive information from resMessage
            foreach (var tt in textsToExclude)
            {
                resMessage = resMessage.Replace(tt, "xx");
            }
            try
            {
                await _bot.SendTextMessageAsync(message.Chat.Id, resMessage, disableMessagePreview);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());// ignore telegram exceptions.
                await _bot.SendTextMessageAsync(message.Chat.Id, "OOps. Smth went wrong");
            }
            try
            {
                var httpClient = new HttpClient();
                var content = new StringContent("{'command':'" + msg.RemoveCommandPart() + "'}", Encoding.UTF8, "application/json");
                await httpClient.PostAsync("https://api.botan.io/track?token=" + _settings.BotanIoKey + "&uid=UID&name=" + msg.GetCommandPart(), content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());// ignore errors
            }
        }

        private async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            await _bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id,
                $"Received {callbackQueryEventArgs.CallbackQuery.Data}");
        }


    }
}
