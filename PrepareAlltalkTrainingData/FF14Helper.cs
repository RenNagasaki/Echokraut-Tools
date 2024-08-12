using SaintCoinach.Ex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrepareAlltalkTrainingData
{
    public static class FF14Helper
    {
        public static string GetLanguageString(Language language)
        {
            switch (language)
            {
                case Language.English:
                    return "en";
                case Language.German:
                    return "de";
                case Language.French:
                    return "fr";
                case Language.Japanese:
                    return "ja";
            }

            return "en";
        }
        public static Language GetLanguage(string language)
        {
            switch (language)
            {
                case "English":
                    return Language.English;
                case "German":
                    return Language.German;
                case "French":
                    return Language.French;
                case "Japanese":
                    return Language.Japanese;
            }

            return Language.English;
        }
    }
}
