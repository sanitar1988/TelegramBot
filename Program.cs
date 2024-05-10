using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;

using Newtonsoft.Json;

using Vosk;
using FFMpegCore;
using System.Diagnostics;
using Whisper.net;

namespace TelegramBot
{
    internal class Program
    {
        static readonly TelegramBotClient botClient = new("7179339622:AAG7ljPdAqhJzTeIj9CEaIcG1Gk4_Jj35y8");
        static VoskRecognizer? voskRecognition;
        static WhisperProcessor? whisperRecognition;
        static string modelPathVosk = @"D:\LLM-models\Vosk\model-small-ru-0.22";
        static string modelPathWhisper = @"D:\LLM-models\Whisper\ggml-base.bin";
        static string? returnWhisper = "";

        // static bool modeTV = false;
        // static string? commandTV = "";
        // static string? prompt;
        //static string? context;
        // public static string? responseAI;
        // static bool sendContext = true;

        static Dictionary<int, TelegramUsers> telegramUsers = new();

        static async Task Main(string[] args)
        {

            using CancellationTokenSource cts = new();

            // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
            ReceiverOptions receiverOptions = new()
            {
                // receive all update types except ChatMember related updates
                AllowedUpdates = Array.Empty<UpdateType>() 
            };

            botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            var me = await botClient.GetMeAsync();

            //var cts = new CancellationTokenSource();
            //var cancellationToken = cts.Token;
            //var receiverOptions = new ReceiverOptions
            //{
            //    AllowedUpdates = { }
            //};

            //_botClient.StartReceiving(
            //    HandleUpdateAsync,
            //    HandleError,
            //    receiverOptions,
            //    cancellationToken
            //);

            //Console.ReadLine();
            
            Console.WriteLine($"Vosk running...");
            voskRecognition = InitVosk(modelPathVosk);
            Console.WriteLine($"Vosk run...");

            Console.WriteLine($"Whisper running...");
            whisperRecognition = InitWhisper(modelPathWhisper);
            Console.WriteLine($"Whisper run...");

            // // устанавливаем метод обратного вызова
            // TimerCallback tm = new TimerCallback(TheardsInfo);
            // // создаем таймер
            // Timer timer = new(tm, null, 0, 60000);

            // Create first telegram user
            telegramUsers.Add(0, new TelegramUsers()
            {
                UserId = 0,
                UserName = "DEFAULT",
                UserTypeSTT = true,
                UserReference = "",
                UserChangeReference = false
            });

            Console.WriteLine($"Telegram BOT {me.FirstName} run...");
            Console.ReadLine();

            // Send cancellation request to stop bot
            cts.Cancel();

        }
        async static Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
        {
            await botClient.GetUpdatesAsync(cancellationToken: cancellationToken);
            Console.WriteLine(JsonConvert.SerializeObject(update));

            if (update.Message != null)
            {
                int userKey = -1;
                // Если текстовое сообщение
                if (update.Message.Type == MessageType.Text && update.Message.Text != null)
                {
                    DateTime now = DateTime.Now;
                    DateTime then = update.Message.Date.AddHours(3);
                    TimeSpan ts = now.Subtract(then);
                    double diff = ts.TotalSeconds;

                    if (diff < 10)
                    {
                        //if (update.Message.Text == @"/start") update.Message.Text = "Привет";
                        userKey = telegramUsers.Where(s => s.Value.UserName == update.Message.Chat.Username).Select(i => i.Key).FirstOrDefault();
                        // If NEW user
                        if (userKey == 0)
                        {
                            bool typeSTT = true;
                            if (update.Message.Text.ToLower() == "/vosk")
                            {
                                typeSTT = true;
                                await botClient.SendTextMessageAsync(
                                    chatId: update.Message.Chat.Id,
                                    text: "Select Vosk",
                                    disableNotification: true,
                                    replyToMessageId: update.Message.MessageId,
                                    cancellationToken: cancellationToken);
                            }
                            if (update.Message.Text.ToLower() == "/whisper")
                            {
                                typeSTT = false;
                                await botClient.SendTextMessageAsync(
                                    chatId: update.Message.Chat.Id,
                                    text: "Select Whisper",
                                    disableNotification: true,
                                    replyToMessageId: update.Message.MessageId,
                                    cancellationToken: cancellationToken);
                            }

#pragma warning disable CS8604 // Possible null reference argument.

                            telegramUsers.Add(telegramUsers.Count + 1, new TelegramUsers()
                            {
                                UserId = telegramUsers.Count + 1,
                                UserName = update.Message.Chat.Username,
                                UserTypeSTT = typeSTT,
                                UserReference = "",
                                UserChangeReference = false
                            });

#pragma warning restore CS8604 // Possible null reference argument.
                        }
                        else
                        {
                            if (update.Message.Text.ToLower() == "/vosk" || update.Message.Text.ToLower() == "/whisper")
                            {
                                bool typeSTT = true;
                                if (update.Message.Text.ToLower() == "/vosk")
                                {
                                    typeSTT = true;
                                    await botClient.SendTextMessageAsync(
                                        chatId: update.Message.Chat.Id,
                                        text: "Select Vosk",
                                        disableNotification: true,
                                        replyToMessageId: update.Message.MessageId,
                                        cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    typeSTT = false;
                                    await botClient.SendTextMessageAsync(
                                        chatId: update.Message.Chat.Id,
                                        text: "Select Whisper",
                                        disableNotification: true,
                                        replyToMessageId: update.Message.MessageId,
                                        cancellationToken: cancellationToken);
                                }
#pragma warning disable CS8604 // Possible null reference argument.
                                telegramUsers[userKey].UserTypeSTT = typeSTT;
#pragma warning restore CS8604 // Possible null reference argument.
                            }

                            if (telegramUsers[userKey].UserChangeReference)
                            {
                                if (update.Message.Text.Length <= 2048)
                                {
                                    telegramUsers[userKey].UserReference = update.Message.Text;
                                    telegramUsers[userKey].UserChangeReference = false;

                                    await botClient.SendTextMessageAsync(
                                        chatId: update.Message.Chat.Id,
                                        text: "Reference text set up",
                                        disableNotification: true,
                                        replyToMessageId: update.Message.MessageId,
                                        cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    telegramUsers[userKey].UserChangeReference = false;
                                    await botClient.SendTextMessageAsync(
                                        chatId: update.Message.Chat.Id,
                                        text: "Error!!!\n" +
                                        "Reference text more 2048 characters\n" +
                                        "Please re-enter reference command",
                                        disableNotification: true,
                                        replyToMessageId: update.Message.MessageId,
                                        cancellationToken: cancellationToken);
                                }
                            }

                            if (update.Message.Text.ToLower() == "/reference")
                            {
                                telegramUsers[userKey].UserChangeReference = true;
                                await botClient.SendTextMessageAsync(
                                            chatId: update.Message.Chat.Id,
                                            text: "Please enter reference text",
                                            disableNotification: true,
                                            replyToMessageId: update.Message.MessageId,
                                            cancellationToken: cancellationToken);
                            }

                        }

                        if (update.Message.Text.ToLower() == "/givemeusers")
                        {
                            string result = "";

                            foreach (var user in telegramUsers.Values)
                            {
                                result += user.UserId.ToString() + "\n" +
                                            user.UserName + "\n" +
                                            user.UserTypeSTT.ToString() + "\n" +
                                            user.UserReference + "\n" +
                                            user.UserChangeReference.ToString() + "\n\n";
                            }

                            await botClient.SendTextMessageAsync(
                                chatId: update.Message.Chat.Id,
                                text: result,
                                disableNotification: true,
                                replyToMessageId: update.Message.MessageId,
                                cancellationToken: cancellationToken);
                        }

                        // if (usersTypeSST.Where(d => d.Key == update.Message.Chat.Username).Select(d => d.Value).FirstOrDefault())
                        // {

                        //     usersTypeSST[update.Message.Chat.Username] = typeSTT;
                        // }
                        // else
                        // {
                        //     bool typeSTT = true;
                        //     if (update.Message.Text == "vosk") typeSTT = true;
                        //     if (update.Message.Text == "whisper") typeSTT = false;
                        //     usersTypeSST.Add(update.Message.Chat.Username, typeSTT);
                        // }

                        // prompt = "";

                        // if (sendContext)
                        // {
                        //     prompt += context + "\n";
                        //     sendContext = false;
                        // }

                        // prompt += update.Message.Text;

                        // await botClient.SendChatActionAsync(
                        //         chatId: update.Message.Chat.Id,
                        //         chatAction: ChatAction.Typing,
                        //         cancellationToken: cancellationToken);

                        //responseAI = await OobaTalker.PromptAI(prompt, 4096, 60);

                        // await botClient.SendTextMessageAsync(
                        //         chatId: update.Message.Chat.Id,
                        //         text: "responseAI",
                        //         disableNotification: true,
                        //         replyToMessageId: update.Message.MessageId,
                        //         cancellationToken: cancellationToken);
                    }
                }
                // Если звуковое сообщение
                if (update.Message.Type == MessageType.Voice && update.Message.Voice != null)
                {
                    DateTime now = DateTime.Now;
                    DateTime then = update.Message.Date.AddHours(3);
                    TimeSpan ts = now.Subtract(then);
                    double diff = ts.TotalSeconds;

                    if (diff < 10)
                    {
                        if (telegramUsers.Where(s => s.Value.UserName == update.Message.Chat.Username).Select(i => i.Key).Count() == 0)
                        {
#pragma warning disable CS8604 // Possible null reference argument.
                            telegramUsers.Add(telegramUsers.Count + 1, new TelegramUsers()
                            {
                                UserId = telegramUsers.Count + 1,
                                UserName = update.Message.Chat.Username,
                                UserTypeSTT = true,
                                UserReference = "",
                                UserChangeReference = false
                            });
#pragma warning restore CS8604 // Possible null reference argument.
                        }

                        await Task.Run(() => STTJobAsync(update, cancellationToken), cancellationToken);

                        // if (_modeTV) ModeTV(result);
                        // else
                        // {
                        //     await botClient.SendTextMessageAsync(
                        //             chatId: update.Message.Chat.Id,
                        //             text: result,
                        //             disableNotification: true,
                        //             replyToMessageId: update.Message.MessageId,
                        //             cancellationToken: cancellationToken);
                        // }

                        // if (result == "режим телевизора")
                        // {
                        //     Console.WriteLine("Mode TV on...");
                        //     _modeTV = true;

                        //     await botClient.SendTextMessageAsync(
                        //         chatId: update.Message.Chat.Id,
                        //         text: "включен режим телевизора",
                        //         disableNotification: true,
                        //         replyToMessageId: update.Message.MessageId,
                        //         cancellationToken: cancellationToken);
                        // }
                        // if (result == "режим обычный")
                        // {
                        //     Console.WriteLine("Mode TV off...");
                        //     _modeTV = false;

                        //     await botClient.SendTextMessageAsync(
                        //         chatId: update.Message.Chat.Id,
                        //         text: "включен режим обычный",
                        //         disableNotification: true,
                        //         replyToMessageId: update.Message.MessageId,
                        //         cancellationToken: cancellationToken);
                        // }

                    }
                    //return;
                }
            }
        }
        static void STTJobAsync(Update update, CancellationToken cancellationToken)
        {
            //watch.Elapsed.ToString("hh\\:mm\\:ss\\.fff")

            string resultAll = "noData";

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            botClient.SendChatActionAsync(
                                chatId: update.Message.Chat.Id,
                                chatAction: ChatAction.Typing,
                                cancellationToken: cancellationToken);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            Stopwatch watch = Stopwatch.StartNew();

            SaveDataTelegramm(update, cancellationToken);
#pragma warning disable CS8604 // Possible null reference argument.
            ConvertFile(update.Message.Chat.Username);
#pragma warning restore CS8604 // Possible null reference argument.

            watch.Stop();
            string timeSave = $"Audio time: {watch.ElapsedMilliseconds} ms \n";
            //string timeSave = "";
            watch.Reset();

            int userKey = telegramUsers.Where(s => s.Value.UserName == update.Message.Chat.Username).Select(i => i.Key).FirstOrDefault();

            if (telegramUsers[userKey].UserTypeSTT)
            {
                Console.WriteLine("Vosk Convert...");
                watch.Start();
                string voskResult = VoskConvert(update.Message.Chat.Username);
                watch.Stop();
                string voskTime = $"Vosk time: {watch.ElapsedMilliseconds} ms \n";
                watch.Reset();
                string voskModelName = $"Vosk model: {modelPathVosk.Replace(@"D:\LLM-models\Vosk\", "")} \n \n";

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (telegramUsers[userKey].UserReference.Length > 0)
                {
#pragma warning disable CS8604 // Possible null reference argument.
                    string werResult = "WER: " + CalculateWER(telegramUsers[userKey].UserReference, voskResult) + " % \n";
#pragma warning restore CS8604 // Possible null reference argument.
                    resultAll = timeSave + voskTime + werResult + voskModelName + "Vosk result: \n" + voskResult + "\n";
                }
                else resultAll = timeSave + voskTime + voskModelName + "Vosk result: \n" + voskResult + "\n";
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            else
            {
                Console.WriteLine("Whisper Convert...");
                watch.Start();
                string whisperResult = WhisperConvert(update.Message.Chat.Username);
                watch.Stop();
                string whisperTime = $"Whisper time: {watch.ElapsedMilliseconds} ms \n";
                watch.Reset();
                string whisperModelName = $"Whisper model: {modelPathWhisper.Replace(@"D:\LLM-models\Whisper\", "")} \n \n";

                whisperResult = whisperResult.Remove(0, 1);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                if (telegramUsers[userKey].UserReference.Length > 0)
                {

#pragma warning disable CS8604 // Possible null reference argument.
                    string werResult = "WER: " + CalculateWER(telegramUsers[userKey].UserReference, whisperResult) + " % \n";
#pragma warning restore CS8604 // Possible null reference argument.
                    resultAll = timeSave + whisperTime + werResult + whisperModelName + "Whisper result: \n" + whisperResult + "\n";
                }
                else resultAll = timeSave + whisperTime + whisperModelName + "Whisper result: \n" + whisperResult + "\n";
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }

            botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: resultAll,
                    disableNotification: true,
                    replyToMessageId: update.Message.MessageId,
                    cancellationToken: cancellationToken);
        }
        static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }

        //public static async Task GenerateVoiceAsync(string inputMessage, Update update)
        //{
        //    try
        //    {
        //        string response = "";

        //        try
        //        {
        //            using (HttpRequestMessage httpRequestMessage = PrepareRequest(inputMessage))
        //            {

        //                if (System.IO.Directory.Exists(@"C:\Users\andre\AppData\Local\Temp\gradio"))
        //                {
        //                    System.IO.Directory.Delete(@"C:\Users\andre\AppData\Local\Temp\gradio",true);
        //                }

        //                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _baseUrlRVC);
        //                string jsonContent = "{\"data\":[" +
        //                    "\"Joi_v1\"," +
        //                    "-20," +
        //                    "\"" + inputMessage + "\"," +
        //                    "\"ru-RU-SvetlanaNeural-Female\"," +
        //                    "2," +
        //                    "\"rmvpe\"," +
        //                    "0.12," +
        //                    "0.4]," +
        //                    "\"event_data\":null," +
        //                    "\"fn_index\":0," +
        //                    "\"session_hash\":\"kjddapy5kfd\"}";

        //                request.Content = new StringContent(jsonContent);
        //                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        //                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        //                response = await _httpClient.Send(request).Content.ReadAsStringAsync();

        //                response = response.Substring(response.IndexOf(@"C:\") + 3, (response.Length - response.IndexOf(@"C:\") - 3));
        //                response = response.Substring(response.IndexOf(@"C:\"), (response.Length - response.IndexOf(@"C:\")));
        //                response = response.Substring(0, (response.IndexOf(",") - 1));
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.ToString());
        //        }

        //        try
        //        {

        //            if (System.IO.File.Exists(Directory.GetCurrentDirectory() + @"/input.wav"))
        //            {
        //                System.IO.File.Delete(Directory.GetCurrentDirectory() + @"/input.wav");
        //                System.IO.File.Delete(Directory.GetCurrentDirectory() + @"/output.ogg");
        //            }

        //            System.IO.File.Move(response, Directory.GetCurrentDirectory() + @"/input.wav");

        //            FFMpegArguments
        //                .FromFileInput(Directory.GetCurrentDirectory() + @"/input.wav")
        //                .OutputToFile(Directory.GetCurrentDirectory() + @"/output.ogg", true, options => options
        //                    .WithConstantRateFactor(21)
        //                    .WithVariableBitrate(4)
        //                    .WithFastStart())
        //                .ProcessSynchronously();

        //            using (Stream stream = System.IO.File.OpenRead(Directory.GetCurrentDirectory() + @"/output.ogg"))
        //            {
        //                Message message = await _botClient.SendVoiceAsync(
        //                    chatId: update.Message.Chat.Id,
        //                    voice: InputFile.FromStream(stream),
        //                duration: 36,
        //                    cancellationToken: _cancellationToken);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.ToString());
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.ToString());
        //    }
        //}

        //private static async Task <string> GetDataAsync(string message, Update update)
        //{
        //    string response_ai = "Ошибка ответа";
        //    try
        //    {
        //        using (HttpRequestMessage httpRequestMessage = PrepareRequest(message))
        //        {
        //            response_ai = await _httpClient.Send(httpRequestMessage).Content.ReadAsStringAsync();

        //            response_ai = Regex.Replace(response_ai, "[^0-9a-zA-Zа-яА-Я,:. -\"\n]+", "");

        //            if (response_ai.Contains("!doctype")) response_ai = "Ошибка ответа";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(ex.Message);
        //    }

        //    return response_ai;
        //}

        // static HttpRequestMessage PrepareRequest(string message)
        // {
        //     Parts[] parts = new Parts[]
        //        {
        //             new Parts
        //             {
        //                 content = message,
        //                 role = "user"
        //             }
        //        };
        //     Conversation conversation = new Conversation()
        //     {
        //         conversation_id = "",
        //         action = "_ask",
        //         model = "gpt-3.5-turbo",
        //         jailbreak = "default",
        //         provider = "g4f.Provider.Auto",
        //         meta = new Meta()
        //         {
        //             id = "",
        //             content = new Content()
        //             {
        //                 conversation = new string[] {},
        //                 internet_access = false,
        //                 content_type = "text",
        //                 parts = parts
        //             }
        //         }
        //     };
        //     var stringPayload = JsonConvert.SerializeObject(conversation);

        //     HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, _baseUrlGPT + @"/backend-api/v2/conversation");
        //     request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
        //     request.Content = new StringContent(stringPayload);
        //     request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        //     return request;
        // }       
        static void SaveDataTelegramm(Update update, CancellationToken cancellationToken)
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var fileId = update.Message.Voice.FileId;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            string destinationFolder = "./" + update.Message.Chat.Username;

            if (!Directory.Exists(destinationFolder)) Directory.CreateDirectory(destinationFolder);

            string destinationFilePath = destinationFolder + "/input.ogg";

            using Stream fileStream = System.IO.File.Create(destinationFilePath);

            botClient.GetInfoAndDownloadFileAsync(
                fileId: fileId,
                destination: fileStream,
                cancellationToken: cancellationToken).Wait(cancellationToken);
        }
        static void ConvertFile(string userName)
        {
            //.WithConstantRateFactor(21)
            //.WithVariableBitrate(4)
            //.WithFastStart())
            try
            {
                string outputFile = "./" + userName + "/output.wav";

                //if (!System.IO.File.Exists(outputFile)) System.IO.File.Create(outputFile);

                FFMpegArguments.FromFileInput("./" + userName + "/input.ogg").OutputToFile(outputFile, true, options =>
                options.WithAudioSamplingRate(16000).WithAudioBitrate(FFMpegCore.Enums.AudioQuality.Low)).ProcessSynchronously();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }
        static WhisperProcessor InitWhisper(string ModelPath)
        {
            WhisperProcessor wproc;

            if (!System.IO.File.Exists(ModelPath))
            {
                Console.WriteLine("Voice recognition model missing...");
#pragma warning disable CS8603 // Possible null reference return.
                return null;
#pragma warning restore CS8603 // Possible null reference return.
            }

            try
            {

                WhisperFactory whis = WhisperFactory.FromPath(ModelPath, libraryPath: "./whisper.dll");
                //WhisperFactory whis = WhisperFactory.FromPath(ModelPath);
                //@"D:\LLM-models\Whisper\whisper.dll"
                // string whisperPrompt = "Короткий звук исходит от пользователя, который разговаривает с языковой моделью ИИ в реальном времени.\n Обратите особое внимание на такие команды, как «ок, стоп» или просто «стоп».\n Если предложения неразборчивы, предположим, что они говорят «стоп».".Trim();

                // var builder = whis.CreateBuilder().WithThreads(16)
                // .WithPrompt(whisperPrompt).WithSingleSegment()
                // .WithLanguage("ru")
                // .WithSegmentEventHandler(NewWhisperSegment);

                // (builder.WithBeamSearchSamplingStrategy() as BeamSearchSamplingStrategyBuilder)!.WithPatience(0.2f).WithBeamSize(5);
                // wproc = builder.Build();

                wproc = whis.CreateBuilder()
                .WithSegmentEventHandler(NewWhisperSegment)
                .WithLanguage("ru")
                .WithSingleSegment()
                .Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Can't load model (could it be missing?): {ex.Message}");
#pragma warning disable CS8603 // Possible null reference return.
                return null;
#pragma warning restore CS8603 // Possible null reference return.
            }

            return wproc;
        }
        static string WhisperConvert(string userName)
        {
            returnWhisper = "notext";
            using var fileStream = System.IO.File.OpenRead("./" + userName + "/output.wav");

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            whisperRecognition.Process(fileStream);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            return returnWhisper;
        }
        static void NewWhisperSegment(SegmentData e)
        {
            returnWhisper = e.Text;
        }
        static VoskRecognizer InitVosk(string ModelPath)
        {
            // set -1 to disable logging messages


            Vosk.Vosk.SetLogLevel(0);
            VoskRecognizer rec;

            if (!Directory.Exists(ModelPath))
            {
                Console.WriteLine("Voice recognition model missing...");
#pragma warning disable CS8603 // Possible null reference return.
                return null;
#pragma warning restore CS8603 // Possible null reference return.
            }

            try
            {
                var model = new Model(ModelPath);
                rec = new VoskRecognizer(model, 16000.0f);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Can't load model (could it be missing?): {ex.Message}");
#pragma warning disable CS8603 // Possible null reference return.
                return null;
#pragma warning restore CS8603 // Possible null reference return.
            }

            //rec.SetMaxAlternatives(5);    // количество вариантов
            //rec.SetWords(true);           // разделение на слова
            //rec.SetPartialWords(true);    // так и не понял что это. что то типа разделение слов на части

            return rec;
        }
        static string VoskConvert(string userName)
        {
            using (Stream source = System.IO.File.OpenRead("./" + userName + "/output.wav"))
            {
                byte[] buffer = new byte[4096];
                int bytesRead;

                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    voskRecognition.AcceptWaveform(buffer, bytesRead);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                };
            }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
            return JsonConvert.DeserializeObject<VoskResult>(voskRecognition.FinalResult()).Text;
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
        static string CalculateWER(string referenceText, string llmText)
        {
            var punctuationIn = referenceText.Where(Char.IsPunctuation).Distinct().ToArray();
            var punctuationOut = llmText.Where(Char.IsPunctuation).Distinct().ToArray();

            var r = referenceText.ToLower().Split(' ').Select(z => z.Trim(punctuationIn));
            var h = llmText.ToLower().Split(' ').Select(z => z.Trim(punctuationOut));

            double deletion, substitution, insertion;
            double[,] d = new double[r.Count() + 1, h.Count() + 1];

            for (int i = 0; i < r.Count() + 1; i++)
            {
                for (int j = 0; j < h.Count() + 1; j++)
                {
                    if (i == 0)
                    {
                        d[0, j] = j;
                    }
                    else if (j == 0)
                    {
                        d[i, 0] = i;
                    }
                }
            }
            for (int i = 1; i < r.Count() + 1; i++)
            {
                for (int j = 1; j < h.Count() + 1; j++)
                {
                    if (r.ElementAt(i - 1) == h.ElementAt(j - 1))
                    {
                        d[i, j] = d[i - 1, j - 1];
                    }
                    else
                    {
                        substitution = d[i - 1, j - 1] + 1;
                        insertion = d[i, j - 1] + 1;
                        deletion = d[i - 1, j] + 1;
                        d[i, j] = Math.Min(substitution, Math.Min(insertion, deletion));
                    }
                }
            }

            double wer = d[r.Count(), h.Count()] / r.Count() * 100;
            wer = Math.Round(wer, 3);
            return wer.ToString();
        }

        // static void TheardsInfo(object obj)
        // {
        //     int nWorkerThreads;
        //     int nCompletionThreads;
        //     ThreadPool.GetMaxThreads(out nWorkerThreads, out nCompletionThreads);
        //     Console.WriteLine("Maximum number of threads: " + nWorkerThreads + "\nI/O threads available: " + nCompletionThreads);
        // }

        //static void ModeTV(string command)
        // {
        //     if (command == "включить" || command == "выключить") _commandTV = "onoff";
        //     if (command == "меню") _commandTV = "menu";
        //     if (command == "источник") _commandTV = "source";
        //     if (command == "ютуб") _commandTV = "youtube";
        //     if (command == "голос") _commandTV = "voice";
        //     if (command == "громче") _commandTV = "upvolume";
        //     if (command == "тише") _commandTV = "downvolume";

        //     if (command == "вверх") _commandTV = "up";
        //     if (command == "вниз") _commandTV = "down";
        //     if (command == "влево") _commandTV = "left";
        //     if (command == "вправо") _commandTV = "right";
        //     if (command == "подтвердить") _commandTV = "ok";
        //     if (command == "выход") _commandTV = "exit";

        //     _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, (_baseUrlESP32 + _commandTV)));
        // }
    }
    public class Conversation
    {
        public string? conversation_id { get; set; }

        public string? action { get; set; }

        public string? model { get; set; }

        public string? jailbreak { get; set; }

        public string? provider { get; set; }

        public Meta? meta { get; set; }
    }
    public class Meta
    {
        public string? id { get; set; }

        public Content? content { get; set; }
    }
    public class Content
    {
        public string[]? conversation { get; set; }
        public bool? internet_access { get; set; }
        public string? content_type { get; set; }
        public Parts[]? parts { get; set; }
    }
    public class Parts
    {
        public string? content { get; set; }
        public string? role { get; set; }
    }
}