using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FF14_Echokraut.DataClasses
{
    public class FF14NPCData
    {
        public FF14NPCData() 
        {
            patchVersion = 1.0m;
        }

        public string name { get; set; }
        public decimal patchVersion { get; set; }
        public string race { get; set; }
        public string gender { get; set; }
        public FF14Voice voiceItem { get; set; }
    }
}
