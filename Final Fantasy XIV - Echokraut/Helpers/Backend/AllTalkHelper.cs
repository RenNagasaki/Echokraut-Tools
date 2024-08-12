using FF14_Echokraut.DataClasses;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Web;
using FF14_Echokraut.Exceptions;
using System.Net;
using System.Globalization;

namespace FF14_Echokraut.Helpers.Backend
{
    internal class AllTalkHelper : ITTSHelper
    {
        HttpClient httpClient;
        HttpClient httpReadyClient;
        string urlBase;
        string voicesPath;
        string generatePath;
        string stopPath;
        string readyPath;

        public AllTalkHelper(string urlBase, string voicesPath, string generatePath, string stopPath, string readyPath)
        {
            this.urlBase = urlBase;
            this.voicesPath = voicesPath;
            this.generatePath = generatePath;
            this.stopPath = stopPath;
            this.readyPath = readyPath;

            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(urlBase);
            httpClient.Timeout = TimeSpan.FromSeconds(1);
            httpReadyClient = new HttpClient();
            httpReadyClient.BaseAddress = new Uri(urlBase);
            httpReadyClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<Stream> GenerateAudioStreamFromVoice(string voiceLine, string voice, string language)
        {
            LogHelper.logThread("Generating Alltalk Audio");

            HttpResponseMessage res = null;
            while (res == null)
            {
                try
                {
                    var uriBuilder = new UriBuilder(urlBase);
                    uriBuilder.Path = generatePath;
                    var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                    query["text"] = voiceLine;
                    query["voice"] = voice;
                    query["language"] = getAlltalkLanguage(language);
                    query["output_file"] = "ignoreme.wav";
                    uriBuilder.Query = query.ToString();
                    LogHelper.logThread("Requesting...");
                    using var req = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

                    res = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
                    EnsureSuccessStatusCode(res);

                    // Copy the sound to a new buffer and enqueue it
                    LogHelper.logThread("Getting response...");
                    var responseStream = await res.Content.ReadAsStreamAsync();
                    LogHelper.logThread("Done");

                    return responseStream;
                }
                catch (Exception ex)
                {
                    LogHelper.logThread(ex.ToString());
                }
            }

            return null;
        }

        public List<FF14Voice> GetAvailableVoices()
        {
            LogHelper.logThread("Loading Alltalk Voices");
            var mappedVoices = new List<FF14Voice>();
            var uriBuilder = new UriBuilder(urlBase) { Path = voicesPath };
            var result = httpClient.GetStringAsync(uriBuilder.Uri);
            result.Wait();
            string resultStr = result.Result.Replace("\\", "");
            AllTalkVoices voices = System.Text.Json.JsonSerializer.Deserialize<AllTalkVoices>(resultStr);

            foreach (string voice in voices.voices)
            {
                if (voice == Constants.NARRATORVOICE)
                {
                    var voiceItem = new FF14Voice()
                    {
                        voiceName = Constants.NARRATORVOICE.Replace(".wav", ""),
                        voice = voice
                    };
                    mappedVoices.Add(voiceItem);
                }
                else
                {
                    string[] splitVoice = voice.Split('_');
                    var gender = splitVoice[0];
                    var race = splitVoice[1];
                    string voiceName = splitVoice[2].Replace(".wav", "");

                    var voiceItem = new FF14Voice()
                    {
                        gender = gender,
                        race = race,
                        voice = voice
                    };

                    voiceItem.patchVersion = 1.0m;
                    var splitVoicePatch = voiceName.Split("@");
                    voiceItem.voiceName = splitVoicePatch[0];
                    if (splitVoicePatch.Length > 1)
                        voiceItem.patchVersion = Convert.ToDecimal(splitVoicePatch[1], new CultureInfo("en-US"));
                    mappedVoices.Add(voiceItem);

                    if (voice.Contains("NPC") && Constants.RACESFORRANDOMNPC.Contains(race))
                    {
                        voiceItem = new FF14Voice()
                        {
                            gender = gender,
                            race = "Default",
                            voiceName = voiceName,
                            voice = voice
                        };
                        mappedVoices.Add(voiceItem);
                    }
                }
            }

            LogHelper.logThread("Done");
            return mappedVoices;
        }

        public async void StopGenerating()
        {
            LogHelper.logThread("Stopping Alltalk Generation");
            HttpResponseMessage res = null;
            while (res == null)
            {
                try
                {
                    var content = new StringContent("");
                    res = await httpClient.PutAsync(stopPath, content);
                } catch (Exception ex)
                {
                    LogHelper.logThread(ex.ToString());
                }
            }
        }

        public async Task<string> CheckReady()
        {
            LogHelper.logThread("Checking if Alltalk is ready");
            var res = await httpReadyClient.GetAsync(readyPath);

            var responseString = await res.Content.ReadAsStringAsync();
            LogHelper.logThread("Ready");

            return responseString;
        }

        private void EnsureSuccessStatusCode(HttpResponseMessage res)
        {
            if (!res.IsSuccessStatusCode)
            {
                throw new AlltalkFailedException(res.StatusCode, "Failed to make request.");
            }
        }

        string getAlltalkLanguage(string language)
        {
            switch (language)
            {
                case "German":
                    return "de";
                case "English":
                    return "en";
            }

            return "de";
        }
    }
}
