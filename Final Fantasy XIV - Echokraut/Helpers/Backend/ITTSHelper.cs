using FF14_Echokraut.DataClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FF14_Echokraut.Helpers.Backend
{
    internal interface ITTSHelper
    {
        List<FF14Voice> GetAvailableVoices();
        Task<Stream> GenerateAudioStreamFromVoice(string voiceLine, string voice, string language);
        Task<string> CheckReady();
        void StopGenerating();
    }
}
