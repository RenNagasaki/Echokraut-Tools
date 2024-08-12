using FF14_Echokraut.DataClasses;
using FF14_Echokraut.Enums;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FF14_Echokraut.Helpers
{
    static internal class XIVApiHelper
    {
        static string xivApiUrlBase;
        static string xivApiPath;
        static HttpClient httpClient;

        static internal void setup(string xivApiUrlBase, string xivApiPath)
        {
            XIVApiHelper.xivApiUrlBase = xivApiUrlBase;
            XIVApiHelper.xivApiPath = xivApiPath;
            httpClient = new HttpClient();
        }

        static internal void loadNPCData(int npcId, ref FF14NPCData npcData, bool newData = true)
        {
            var uriBuilder = new UriBuilder(xivApiUrlBase) { Path = xivApiPath + npcId.ToString() };
            var result = httpClient.GetStringAsync(uriBuilder.Uri);
            result.Wait();
            dynamic jsonObj = JObject.Parse(result.Result);

            npcData.gender = jsonObj.Base.Gender.Value == 0 ? "Male" : "Female";
            if (newData)
                npcData.name = cleanUpName(jsonObj.Name_de.Value);

            if (!string.IsNullOrWhiteSpace(jsonObj.Base.Race.ToString()))
            {
                if (jsonObj.Base.BodyType != null && jsonObj.Base.BodyType.Value == 4)
                    npcData.race = cleanUpName("Child");
                else
                    npcData.race = cleanUpName(jsonObj.Base.Race.Name.Value);
            }
            else
            {
                npcData.race = "Default";

                if (!string.IsNullOrWhiteSpace(jsonObj.Base.ModelChara.ToString()))
                {
                    var baseModel = Convert.ToInt32(jsonObj.Base.ModelChara.Base.Value.ToString() + jsonObj.Base.ModelChara.Model.Value.ToString().PadLeft(4, '0'));
                    LogHelper.logThread(string.Format("{0} | {1}", npcData.name, baseModel.ToString()));
                    if (Enum.IsDefined(typeof(NPCRacesEnum), baseModel))
                        npcData.race = ((NPCRacesEnum)baseModel).ToString();
                }
            }

            npcData.patchVersion = Convert.ToDecimal(jsonObj.GamePatch.Version, new CultureInfo("en-US"));
        }

        static internal string cleanUpName(string name)
        {
            name = name.Replace("[a]", "");
            name = Regex.Replace(name, "[^a-zA-Z0-9-' ]+", "");
            name = name.Replace(" ", "+").Replace("'", "=");

            return name;
        }

        static internal string unCleanUpName(string name)
        {
            name = name.Replace("+", " ").Replace("=", "'");

            return name;
        }
    }
}
