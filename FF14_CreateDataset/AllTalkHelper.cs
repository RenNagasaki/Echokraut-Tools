
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using System.Web;
using System.Net;
using System.Globalization;
using FF14_Echokraut.Exceptions;
using Microsoft.VisualBasic;
using NAudio.Wave;
using FF14_CreateDataset;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace FF14_Echokraut.Helpers.Backend
{
    internal class AllTalkHelper
    {
        HttpClient httpClient;
        string urlBase;
        string generatePath;

        public AllTalkHelper(string urlBase, string generatePath)
        {
            this.urlBase = urlBase;
            this.generatePath = generatePath;

            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(urlBase);
            httpClient.Timeout = TimeSpan.FromSeconds(15);
        }

        public async Task<bool> GenerateAudioFileFromVoice(string voiceLine, string voice, string language)
        {
            LogHelper.logData("Generating Alltalk Audio");

            HttpResponseMessage res = null;
            try
            {
                var uriBuilder = new UriBuilder(urlBase);
                uriBuilder.Path = generatePath;
                var nvc = new List<KeyValuePair<string, string>>();
                nvc.Add(new KeyValuePair<string, string>("text_input", voiceLine));
                nvc.Add(new KeyValuePair<string, string>("text_filtering", "standard"));
                nvc.Add(new KeyValuePair<string, string>("character_voice_gen", voice + ".wav"));
                nvc.Add(new KeyValuePair<string, string>("narrator_enabled", "false"));
                nvc.Add(new KeyValuePair<string, string>("narrator_voice_gen", voice + ".wav"));
                nvc.Add(new KeyValuePair<string, string>("text_not_inside", "character"));
                nvc.Add(new KeyValuePair<string, string>("language", language));
                nvc.Add(new KeyValuePair<string, string>("output_file_name", "ignoreme"));
                nvc.Add(new KeyValuePair<string, string>("output_file_timestamp", "false"));
                nvc.Add(new KeyValuePair<string, string>("autoplay", "false"));
                nvc.Add(new KeyValuePair<string, string>("autoplay_volume", "1.0"));
                LogHelper.logData("Requesting...");
                LogHelper.logData(uriBuilder.Uri.AbsoluteUri);
                using var req = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri);
                req.Content = new FormUrlEncodedContent(nvc);

                res = await httpClient.SendAsync(req);
                LogHelper.logData(res.StatusCode.ToString());

                // Copy the sound to a new buffer and enqueue it
                LogHelper.logData("Getting response...");
                var responseStream = res.Content.ReadAsStringAsync().Result;
                dynamic jsonObj = JObject.Parse(responseStream);
                var genFilePath = jsonObj.output_cache_url.Value;
                //var s = new RawSourceWaveStream(responseStream, new WaveFormat(24000, 16, 1));

                var speaker = Regex.Replace(voice, @"[^a-zA-Z0-9 _-]", "").Replace(" ", "_").Replace("-", "_");
                var sentence = voiceLine;
                var VoiceFilesPath = @"G:\XIV_Voices\Data";
                var DirectoryPath = @"G:\XIV_Voices";
                var voiceName = "Roegadyn_Sea_Wolves_Female_01";
                // Create a Path
                string cleanedSentence = Regex.Replace(sentence, " < [^<]*>", "");
                cleanedSentence = RemoveSymbolsAndLowercase(cleanedSentence);
                string actorDirectory = VoiceFilesPath + "/" + voiceName;
                string speakerDirectory = actorDirectory + "/" + speaker;

                string filePath = speakerDirectory + "/" + cleanedSentence;
                int missingFromDirectoryPath = 0;
                if (DirectoryPath.Length < 13)
                    missingFromDirectoryPath = 13 - DirectoryPath.Length;
                int maxLength = 200 - ((DirectoryPath + "/" + voiceName + "/" + speaker).Length);
                maxLength -= missingFromDirectoryPath;
                if (cleanedSentence.Length > maxLength)
                    cleanedSentence = cleanedSentence.Substring(0, maxLength);

                cleanedSentence = Regex.Replace(cleanedSentence, @"[^a-zA-Z0-9 _-]", "").Replace(" ", "_").Replace("-", "_");
                filePath = speakerDirectory + "/" + cleanedSentence;

                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);
                var generateData = new GenerateData()
                {
                    speaker = voice,
                    sentence = voiceLine,
                    lastSave = DateTime.Now.ToLongDateString()
                };

                File.WriteAllText(filePath + ".json", $"{{\r\n\"speaker\": \"{speaker}\",\r\n\"sentence\": \"{voiceLine}\",\r\n\"lastSave\": \"{DateTime.Now.ToLongDateString()}\"\r\n}}");
                using (var client = new WebClient())
                {
                    client.DownloadFile(genFilePath, filePath + ".wav");
                    LogHelper.logData("Done");
                    return true;
                }
                //using (var fileWriter = new WaveFileWriter(filePath + audioFileName + ".wav", new WaveFormat(24000, 16, 1)))
                //{
                //    responseStream.CopyTo(fileWriter);
                //    LogHelper.logData("Done");
                //    return;
                //}
            }
            catch (Exception ex)
            {
                LogHelper.logData(ex.ToString());
            }

            return false;
        }

        string RemoveSymbolsAndLowercase(string input)
        {
            // Replace player name with Adventurer before loading or saving
            string pattern = "\\b" + "_NAME_" + "\\b";
            string result = Regex.Replace(input, pattern, "Abenteurer");

            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in result)
            {
                if (char.IsLetter(c))
                {
                    stringBuilder.Append(char.ToLower(c));
                }
            }
            return stringBuilder.ToString();
        }
    }
}
