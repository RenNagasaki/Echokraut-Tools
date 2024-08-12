using FF14_Echokraut.DataClasses;
using FF14_Echokraut.Helpers.Backend;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebSocketSharp;

namespace FF14_Echokraut.Helpers
{
    static internal class FF14WebSocketHelper
    {
        static internal List<FF14Voice> mappedVoices = null;
        static internal Dictionary<string, FF14NPCData> npcDatas = null;
        static internal bool queueText = false;
        static internal Thread queueThread = new Thread(workQueue);
        static internal bool stopThread = false;
        static List<RawSourceWaveStream> voiceQueue = new List<RawSourceWaveStream>();
        static WebSocket webSocket;
        static ITTSHelper backend;
        static WasapiOut activePlayer = null;
        static Random rand = new Random(Guid.NewGuid().GetHashCode());
        static bool playing = false;

        static internal void prepareHelpers(string allTalkUrlBase, string voicesPath, string generatePath, string stopPath, string readyPath)
        {
            queueThread.Start();
            backend = new AllTalkHelper(allTalkUrlBase, voicesPath, generatePath, stopPath, readyPath);
            getAndMapVoices();
            MainWindow.mainWindow.btn_ConnectBackend.IsEnabled = false;
            MainWindow.mainWindow.btn_AddNpcData.IsEnabled = true;
        }

        static internal void loadNPCData()
        {
            LogHelper.logData("Loading NPCData");
            npcDatas = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, FF14NPCData>>(File.ReadAllText(Constants.UNVOICEDNPCS));
            if (npcDatas == null)
                npcDatas = new Dictionary<string, FF14NPCData>();

            LogHelper.logData("Success");
        }

        static internal void addNPCData(FF14NPCData npcData)
        {
            npcDatas.Add(npcData.name, npcData);
            MainWindow.mainWindow.Dispatcher.Invoke(() =>
            {
                MainWindow.mainWindow.addNPCData(npcData);
            });
        }

        static internal void setup(string fF14UrlBase, string xIVAPIUrlBase, string xIVAPIPath)
        {
            webSocket = new WebSocket(fF14UrlBase);
            XIVApiHelper.setup(xIVAPIUrlBase, xIVAPIPath);

            webSocket.OnMessage += (sender, e) =>
            {
                FF14VoiceMessage? voiceMessage = System.Text.Json.JsonSerializer.Deserialize<FF14VoiceMessage>(e.Data);
                MainWindow.mainWindow.Dispatcher.Invoke(() => {
                    LogHelper.logData(String.Format("New FF14 Message - Type: [{0}] Source: [{3}] NPC: [{1}] Message: {2}", voiceMessage.Type.ToString(), voiceMessage.Speaker, voiceMessage.Payload, voiceMessage.Source));
                });

                try
                {
                    switch (voiceMessage.Type)
                    {
                        case "Say":

                            if (voiceMessage.Source == "Chat")
                            {

                            }
                            else
                                generateVoice(analyzeAndImproveText(voiceMessage.Payload), getAllTalkVoice(voiceMessage.NpcId, voiceMessage.Speaker), voiceMessage.Language);
                            break;
                        case "Cancel":
                            if (playing)
                            {
                                if (activePlayer != null)
                                {
                                    activePlayer.Stop();
                                }
                                backend.StopGenerating();
                                playing = false;
                            }
                            break;
                    }

                }
                catch (Exception ex)
                {
                    LogHelper.logThread(ex.ToString());
                }

            };

            webSocket.OnClose += (sender, e) =>
            {
                MainWindow.mainWindow.Dispatcher.Invoke(() =>
                {
                    MainWindow.mainWindow.btn_connectFF14.IsEnabled = true;
                });
                LogHelper.logThread("Lost connection to FF14");
            };

            webSocket.OnOpen += (sender, e) =>
            {
                MainWindow.mainWindow.Dispatcher.Invoke(() =>
                {
                    MainWindow.mainWindow.btn_connectFF14.IsEnabled = false;
                });
                LogHelper.logThread("Connected to FF14");
            };

            webSocket.Connect();
        }

        static void workQueue()
        {
            while (!stopThread)
            {
                if (!playing && voiceQueue.Count > 0)
                {
                    var queueItem = voiceQueue[0];
                    voiceQueue.RemoveAt(0);
                    LogHelper.logThread("Playing next Queue Item");
                    activePlayer = new WasapiOut(AudioClientShareMode.Shared, 0);
                    activePlayer.PlaybackStopped += SoundOut_PlaybackStopped;
                    activePlayer.Init(queueItem);
                    activePlayer.Play();
                    playing = true;
                }

                Thread.Sleep(100);
            }
        }

        static string analyzeAndImproveText(string text)
        {
            string resultText = text;

            resultText = Regex.Replace(resultText, "(?<=^|[^/.\\w])[a-zA-Z]+[\\.\\,\\!\\?](?=[a-zA-ZäöüÄÖÜ])", "$& ");

            return resultText;
        }

        static void getAndMapVoices()
        {
            LogHelper.logThread("Loading and mapping voices");
            mappedVoices = backend.GetAvailableVoices();
            mappedVoices.Sort((x, y) => x.ToString().CompareTo(y.ToString()));

            LogHelper.logThread("Success");
        }

        static internal async void generateVoice(string text, string voice, string language)
        {
            LogHelper.logThread("Generating Audio");

            try
            {
                var splitText = prepareAndSentenceSplit(text).ToList();
                splitText.RemoveAt(splitText.Count - 1);

                foreach (var textLine in splitText)
                {

                    var ready = "";

                    while (ready != "Ready")
                        ready = await backend.CheckReady();

                    var responseStream = await backend.GenerateAudioStreamFromVoice(textLine, voice, language);

                    var s = new RawSourceWaveStream(responseStream, new WaveFormat(24000, 16, 1));
                    voiceQueue.Add(s);
                }
            }
            catch (Exception ex)
            {
                LogHelper.logThread(ex.ToString());
            }

            LogHelper.logThread("Done");
        }

        private static string[] prepareAndSentenceSplit(string text)
        {
            text = text.Replace("...", ",,,");
            text = text.Replace("..", ",,");
            text = text.Replace(".", "D0T.");
            text = text.Replace("!", "EXC!");
            text = text.Replace("?", "QUEST?");

            var splitText = text.Split(Constants.SENTENCESEPARATORS);

            for (int i = 0; i < splitText.Length; i++)
            {
                splitText[i] = splitText[i].Replace(",,,", "...").Replace(",,", "..").Replace("D0T", ".").Replace("EXC", "!").Replace("QUEST", "?").Trim();
            }

            return splitText;
        }

        private static void SoundOut_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            var soundOut = sender as WasapiOut;
            soundOut.Dispose();
            playing = false;
        }

        static void getVoiceOrRandom(ref FF14NPCData npcData, int npcId)
        {
            var localNpcData = npcData;
            FF14Voice voiceItem = npcData.voiceItem;

            var voiceItems = mappedVoices.FindAll(p => p.gender == localNpcData.gender && p.race == localNpcData.race && p.voiceName == localNpcData.name && localNpcData.patchVersion >= p.patchVersion);
            if (voiceItems.Count > 0)
            {
                voiceItems.Sort((a, b) => b.patchVersion.CompareTo(a.patchVersion));
                voiceItem = voiceItems[0];
            }

            if (voiceItem == null)
            {
                voiceItems = mappedVoices.FindAll(p => p.gender == localNpcData.gender && p.race == localNpcData.race && p.voiceName.Contains("NPC"));

                if (voiceItems.Count == 0)
                    voiceItems = mappedVoices.FindAll(p => p.gender == localNpcData.gender && p.race == "Default" && p.voiceName.Contains("NPC"));

                var randomVoice = voiceItems[rand.Next(0, voiceItems.Count)];
                voiceItem = randomVoice;
            }

            if (voiceItem == null)
                voiceItem = mappedVoices.Find(p => p.voice == Constants.NARRATORVOICE);

            npcData.voiceItem = voiceItem;
        }

        static void loadNPCDataAPI(int npcId, ref FF14NPCData npcData, bool newData = true)
        {
            XIVApiHelper.loadNPCData(npcId, ref npcData, newData);

            getVoiceOrRandom(ref npcData, npcId);

            if (!npcDatas.ContainsKey(npcData.name))
                addNPCData(npcData);

            MainWindow.mainWindow.saveNPCData();
            LogHelper.logThread(string.Format("Loaded NPC Data from API -> {0} | {1} | {2}({3})", npcData.gender, npcData.race, npcData.name, npcId));
        }

        static string getAllTalkVoice(int? npcId, string npcName)
        {
            if (npcId != null)
            {
                FF14NPCData npcData;
                if (!npcDatas.TryGetValue(XIVApiHelper.cleanUpName(npcName), out npcData))
                {
                    npcData = new FF14NPCData();
                    loadNPCDataAPI(npcId.Value, ref npcData);
                }
                else
                    loadNPCDataAPI(npcId.Value, ref npcData, false);

                LogHelper.logThread(string.Format("Loaded voice: {0} for NPC: {1}", npcData.voiceItem.voice, npcName));
                return npcData.voiceItem.voice;
            }

            return Constants.NARRATORVOICE;
        }
    }
}
