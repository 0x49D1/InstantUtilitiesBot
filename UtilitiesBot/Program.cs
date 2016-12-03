using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputMessageContents;
using Telegram.Bot.Types.ReplyMarkups;
using UtilitiesBot.Properties;
using UtilitiesBot.Utilities;

namespace UtilitiesBot
{
    class Program
    {
        private static readonly TelegramBotClient Bot = new TelegramBotClient(Settings.Default.ApiKey);
        private static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnInlineQuery += BotOnInlineQueryReceived;
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

        private static async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            InlineQueryResult[] results = {
                new InlineQueryResultLocation
                {
                    Id = "1",
                    Latitude = 40.7058316f, // displayed result
                    Longitude = -74.2581888f,
                    Title = "New York",
                    InputMessageContent = new InputLocationMessageContent // message if result is selected
                    {
                        Latitude = 40.7058316f,
                        Longitude = -74.2581888f,
                    }
                },

                new InlineQueryResultLocation
                {
                    Id = "2",
                    Longitude = 52.507629f, // displayed result
                    Latitude = 13.1449577f,
                    Title = "Berlin",
                    InputMessageContent = new InputLocationMessageContent // message if result is selected
                    {
                        Longitude = 52.507629f,
                        Latitude = 13.1449577f
                    }
                }
            };

            await Bot.AnswerInlineQueryAsync(inlineQueryEventArgs.InlineQuery.Id, results, isPersonal: true, cacheTime: 0);
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
                msg = "/ddg"; // Default command is DDG

            string resMessage = "Nothing found for command. Try /help";
            if (msg.StartsWithOrdinalIgnoreCase("/tounixtime;/toepoch"))
            {
                UnixTimeStamp stamp = new UnixTimeStamp();
                resMessage = stamp.ConvertCommandToUnixTime(message.Text);
            }
            if (msg.StartsWithOrdinalIgnoreCase("/ddg;/duckduckgo;/duckduckgoinstant"))
            {
                string value = msg.RemoveCommandPart().Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    //http://api.duckduckgo.com/?q=14ml%20in%20litre&format=json
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync("http://api.duckduckgo.com/?q=" + value + "&format=json");
                    string content = await response.Content.ReadAsStringAsync();
                    JObject jo = JObject.Parse(content);
                    string answer = Regex.Replace(jo.SelectToken("Answer").ToString(), @"<[^>]*>", String.Empty);
                    if (string.IsNullOrEmpty(answer))
                    {
                        string moreAnswer = jo.SelectToken("RelatedTopics").Any() ? jo.SelectToken("RelatedTopics")[0]["Result"].ToString() : "";
                        if (!string.IsNullOrEmpty(moreAnswer))
                        {
                            moreAnswer = moreAnswer.Substring(moreAnswer.IndexOf("</a>") + 4);
                            if (!string.IsNullOrEmpty(moreAnswer))
                                resMessage = moreAnswer + "\n" + jo.SelectToken("RelatedTopics")[0]["Icon"]["URL"] +
                                             "\n" + "See: https://duckduckgo.com/?q=" + value;
                        }
                    }
                    else
                        resMessage = answer;
                }
            }
            if (msg.StartsWithOrdinalIgnoreCase("/hash"))
            {
                string msgLocal = "Proper format for /hash command is /hash sha256 test";
                try
                {
                    string v = message.Text.RemoveCommandPart().Trim();
                    string type = v.Split(' ')[0];
                    v = v.Replace(type, "");
                    HashCalculator hc = new HashCalculator();
                    msgLocal = hc.CalculateHash(v, type);
                }
                catch (IndexOutOfRangeException ior)
                {
                    logger.Error(ior);
                    msgLocal = "Proper format for /hash command is /hash sha256 test";
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
                resMessage = msgLocal;
            }
            if (msg.StartsWithOrdinalIgnoreCase("/sha1"))
                resMessage = msg.Trim().Hash<SHA1>();
            if (msg.StartsWithOrdinalIgnoreCase("/sha512"))
                resMessage = msg.Trim().Hash<SHA512>();
            if (msg.StartsWithOrdinalIgnoreCase("/sha256"))
                resMessage = msg.Trim().Hash<SHA256>();
            if (msg.StartsWithOrdinalIgnoreCase("/md5"))
                resMessage = msg.Trim().Hash<MD5>();

            if (message.Text.StartsWith("/inline")) // send inline keyboard
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] // first row
                    {
                        new InlineKeyboardButton("1.1"),
                        new InlineKeyboardButton("1.2"),
                    },
                    new[] // second row
                    {
                        new InlineKeyboardButton("2.1"),
                        new InlineKeyboardButton("2.2"),
                    }
                });

                await Task.Delay(500); // simulate longer running task

                await Bot.SendTextMessageAsync(message.Chat.Id, "Choose",
                    replyMarkup: keyboard);
            }
            else if (message.Text.StartsWith("/keyboard")) // send custom keyboard
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new [] // first row
                    {
                        new KeyboardButton("1.1"),
                        new KeyboardButton("1.2"),
                    },
                    new [] // last row
                    {
                        new KeyboardButton("2.1"),
                        new KeyboardButton("2.2"),
                    }
                });

                await Bot.SendTextMessageAsync(message.Chat.Id, "Choose",
                    replyMarkup: keyboard);
            }
            else if (message.Text.StartsWith("/photo")) // send a photo
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.UploadPhoto);

                const string file = @"<FilePath>";

                var fileName = file.Split('\\').Last();

                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var fts = new FileToSend(fileName, fileStream);

                    await Bot.SendPhotoAsync(message.Chat.Id, fts, "Nice Picture");
                }
            }
            else if (message.Text.StartsWith("/request")) // request location or contact
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton("Location")
                    {
                        RequestLocation = true
                    },
                    new KeyboardButton("Contact")
                    {
                        RequestContact = true
                    },
                });

                await Bot.SendTextMessageAsync(message.Chat.Id, "Who or Where are you?", replyMarkup: keyboard);
            }
            else if (message.Text.StartsWith("/start") || message.Text.StartsWith("/help"))
            {
                resMessage = @"Usage:
Default command is /ddg
/help - Shows all the commands with examples for some of them
/ddg - Instant answers from duckduckgo.com. Example: /ddg 15km to miles
/tounixtime - Convert datetime to unixtimestamp. Message must be like in format: dd.MM.yyyy HH:mm:sss 01.09.1980 06:32:32. Or just text 'now'
/toepoch - Calculate unix timestamp for date in format dd.MM.yyyy HH:mm:ss
/hash - Calculate hash. Use like this: /hash sha256 test
";
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(resMessage);
            Console.ForegroundColor = ConsoleColor.White;

            await Bot.SendTextMessageAsync(message.Chat.Id, resMessage);
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            await Bot.AnswerCallbackQueryAsync(callbackQueryEventArgs.CallbackQuery.Id,
                $"Received {callbackQueryEventArgs.CallbackQuery.Data}");
        }


    }
}