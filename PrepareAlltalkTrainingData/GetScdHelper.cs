using SaintCoinach;
using SaintCoinach.Ex;
using SaintCoinach.IO;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace PrepareAlltalkTrainingData
{
    public static class GetScdHelper
    {
        private static bool loadingExd = false;
        private static List<string> logList = new List<string>();

        public static void GetExd(ARealmReversed realm, Language language, string saveLocation)
        {
            loadingExd = true;
            realm.Packs.GetPack(new SaintCoinach.IO.PackIdentifier("exd", PackIdentifier.DefaultExpansion, 0)).KeepInMemory = false;
            const string CsvFileFormat = "exd/{0}{1}.csv";

            var filesToExport = realm.GameData.AvailableSheets.ToList().FindAll(p => p.Contains("cut_scene") || p.Contains("Balloon") || p.Contains("InstanceContentTextData") || p.Contains("ManFst"));

            var successCount = 0;
            var failCount = 0;
            var maxFiles = filesToExport.Count();
            var currentFiles = 0;
            App.Current.Dispatcher.Invoke(() => MainWindow.Instance.progress_bar.Maximum = maxFiles);
            App.Current.Dispatcher.Invoke(() => MainWindow.Instance.progress_bar.Value = currentFiles);
            foreach (var name in filesToExport)
            {
                currentFiles++;
                App.Current.Dispatcher.Invoke(() => MainWindow.Instance.progress_bar.Value = currentFiles);
                var currentFile = $"Getting Exd: {name} {currentFiles}/{maxFiles}";
                App.Current.Dispatcher.Invoke(() => MainWindow.Instance.lbl_progress.Content = currentFile);

                var sheet = realm.GameData.GetSheet(name);
                foreach (var lang in sheet.Header.AvailableLanguages)
                {
                    if (lang == language || lang == Language.None)
                    {
                        var code = lang.GetCode();
                        if (code.Length > 0)
                            code = "." + code;
                        var target = new FileInfo(Path.Combine(saveLocation, string.Format(CsvFileFormat, name, code)));
                        try
                        {
                            if (!target.Directory.Exists)
                                target.Directory.Create();
                            ExdHelper.SaveAsCsv(sheet, lang, target.FullName, false);
                            ++successCount;
                        }
                        catch (Exception e)
                        {
                            logList.Add($"Export of {name} failed: {e.Message}");
                            try { if (target.Exists) { target.Delete(); } } catch { }
                            ++failCount;
                        }
                    }
                }

                if (MainWindow.Instance.Closing)
                    break;
            }

            logList.Add($"{successCount} files exported, {failCount} failed");
            loadingExd = false;
        }

        public static void WorkCutScenes(ARealmReversed realm, Language language, string savePath)
        {
            foreach (var expansion in PackIdentifier.ExpansionToKeyMap.Keys)
            {
                realm.Packs.GetPack(new SaintCoinach.IO.PackIdentifier("cut", expansion, 0)).KeepInMemory = false;
            }
            var languageString = FF14Helper.GetLanguageString(language);
            var folder = savePath + @"\exd\";
            var files = System.IO.Directory.GetFiles(folder, $"*.{languageString}.*", SearchOption.AllDirectories).ToList();
            Dictionary<string, List<string>> characterLines = new Dictionary<string, List<string>>();

            var maxFiles = files.Count;
            var actFile = 0;
            App.Current.Dispatcher.Invoke(() => MainWindow.Instance.progress_bar.Maximum = maxFiles);
            App.Current.Dispatcher.Invoke(() => MainWindow.Instance.progress_bar.Value = actFile);
            foreach (var file in files)
            {
                actFile++;
                App.Current.Dispatcher.Invoke(() => MainWindow.Instance.progress_bar.Value = actFile);
                var currentFile = $"Getting audio files for: {file} {actFile}/{maxFiles}";
                App.Current.Dispatcher.Invoke(() => MainWindow.Instance.lbl_progress.Content = currentFile);
                var fileData = System.IO.File.ReadAllLines(file).ToList();
                fileData.RemoveAt(0);
                var maxAudioFiles = fileData.Count;
                var actAudioFile = 0;
                foreach (var audioLine in fileData)
                {
                    actAudioFile++;
                    if (audioLine.ToLower().Contains("voiceman") || audioLine.ToLower().Contains("manfst"))
                    {
                        var audioLineFixed = audioLine.Replace(",\"", "|\"");
                        var audioLineSplit = audioLineFixed.Split('|');
                        var audioFile = audioLineSplit[1];
                        var audioFileSplit = audioFile.Split('_');
                        if (audioFileSplit.Length == 5)
                        {
                            var character = audioFileSplit[4].ToLower().Replace("\"", "");
                            audioFile = audioFile.Substring(0, audioFile.Length - character.Length - 1).Replace("TEXT", "vo").ToLower().Replace("\"", "");

                            var text = audioLineSplit[2].Trim();
                            var wantedLines = WorkText(text);

                            if (wantedLines == null || String.IsNullOrWhiteSpace(wantedLines[0]))
                                continue;

                            AddLine(realm, languageString, savePath, wantedLines[0], audioFile + "m_", character, characterLines);
                            if (wantedLines.Length > 1)
                                AddLine(realm, languageString, savePath, wantedLines[1], audioFile + "f_", character, characterLines);
                        }
                    }

                    Console.WriteLine(string.Format("  ->  Checking audiofiles. {0}/{1}", actAudioFile, maxAudioFiles));

                    if (MainWindow.Instance.Closing)
                        break;
                }


                ConvertFiles(savePath + @"\temp", savePath + @"\wavs");
                Console.WriteLine(string.Format("Creating datasets. {0}/{1}", actFile, maxFiles));

                if (MainWindow.Instance.Closing)
                    break;
            }

            var finalDataSetTrain = new List<string>();
            var finalDataSetEval = new  List<string>();
            finalDataSetTrain.Add("audio_file|text|speaker_name");
            finalDataSetEval.Add("audio_file|text|speaker_name");
            foreach (var dataSet in characterLines.Values)
            {
                int i = 0;
                foreach (var line in dataSet)
                {
                    i++;

                    if (i < 9)
                        finalDataSetTrain.Add(line);
                    else
                        finalDataSetEval.Add(line);

                    if (i == 10)
                        i = 0;
                }
            }

            System.IO.File.WriteAllLines(savePath + @"\metadata_train.csv", finalDataSetTrain);
            System.IO.File.WriteAllLines(savePath + @"\metadata_eval.csv", finalDataSetEval);
        }

        private static void AddLine(ARealmReversed realm, string languageString, string savePath, string line, string audioFile, string character, Dictionary<string, List<string>> characterLines)
        {
            var localText = line;
            var audioPath = audioFile + $"{languageString}.scd";
            var fileName = audioFile + $"{languageString}.wav";
            foreach (var expansion in PackIdentifier.ExpansionToKeyMap.Keys)
            {
                var audioPathFull = $"cut/{expansion}/sound/{audioFile.Substring(3, 6)}/{audioFile.Substring(3, 14)}/{audioPath}";
                if (audioFile.Contains("manfst"))
                    audioPathFull = $"cut/{expansion}/sound/{audioFile.Substring(3, 6)}/{audioFile.Substring(3, 9)}/{audioPath.Substring(0, 12)}{audioPath.Substring(18)}";

                var result = ExportScdFile(realm, audioPathFull, savePath + @"\temp");

                if (result)
                {
                    if (!characterLines.ContainsKey(character))
                        characterLines.Add(character, new List<string>());
                    characterLines[character].Add("wavs\\" + fileName + "|" + localText + "|" + character);
                    break;
                }
            }
        }

        private static bool ExportScdFile(ARealmReversed realm, string filePath, string savePath)
        {
            try
            {
                if (!realm.Packs.TryGetFile(filePath, out var file))
                    return false;

                var scdFile = new SaintCoinach.Sound.ScdFile(file);
                var count = 0;
                for (var i = 0; i < scdFile.ScdHeader.EntryCount; ++i)
                {
                    var e = scdFile.Entries[i];
                    if (e == null)
                        continue;

                    var fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    var targetPath = System.IO.Path.Combine(savePath, fileNameWithoutExtension);

                    switch (e.Header.Codec)
                    {
                        case SaintCoinach.Sound.ScdCodec.MSADPCM:
                            targetPath += ".wav";
                            break;
                        case SaintCoinach.Sound.ScdCodec.OGG:
                            targetPath += ".ogg";
                            break;
                        default:
                            throw new NotSupportedException();
                    }

                    var fInfoRaw = new System.IO.FileInfo(targetPath.Replace(".wav", ".scd"));
                    var fInfo = new System.IO.FileInfo(targetPath.Replace("temp", "wavs"));

                    if (!fInfoRaw.Directory.Exists) 
                        fInfoRaw.Directory.Create();
                    if (!fInfo.Directory.Exists)
                        fInfo.Directory.Create();
                    System.IO.File.WriteAllBytes(fInfoRaw.FullName, e.File.SourceFile.GetData());
                    //System.IO.File.WriteAllBytes(fInfo.FullName, e.GetDecoded());
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static void ConvertFiles(string inPath, string outPath)
        {
            if (System.IO.Directory.Exists(inPath))
            {
                String command = @$"/c VGSC\VGSC.exe {inPath} {outPath} & exit";
                ProcessStartInfo cmdsi = new ProcessStartInfo("cmd.exe");
                cmdsi.Arguments = command;
                Process cmd = Process.Start(cmdsi);
                cmd.WaitForExit();
                cmd.Kill();
                foreach (var file in System.IO.Directory.GetFiles(inPath))
                    System.IO.File.Delete(file);
            }
        }

        private static string[] WorkText(string text)
        {
            //LogHelper.logData($"Working on Text: {text}");
            text = text.Substring(1, text.Length - 2).Trim();
            text = CleanUpLine(text);

            if (string.IsNullOrWhiteSpace(text))
                return null;

            var wantedLine = text;
            var wantedLine2 = wantedLine;



            if (wantedLine.Contains("<if(PLYR: 53 ? 1 )"))
            {
                wantedLine = wantedLine.Replace("<if(PLYR: 53 ? 1 ) {<if( 5 ) {<ref:GCRankLimsaFemaleText>} else {<ref:GCRankLimsaMaleText>}>} else {<if(PLYR: 54 ? 1 ) {<if( 5 ) {<ref:GCRankGridaniaFemaleText>} else {<ref:GCRankGridaniaMaleText>}>} else {<if(PLYR: 55 ? 1 ) {<if( 5 ) {<ref:GCRankUldahFemaleText>} else {<ref:GCRankUldahMaleText>}>} else {}>}>}>", "");
                wantedLine2 = wantedLine2.Replace("<if(PLYR: 53 ? 1 ) {<if( 5 ) {<ref:GCRankLimsaFemaleText>} else {<ref:GCRankLimsaMaleText>}>} else {<if(PLYR: 54 ? 1 ) {<if( 5 ) {<ref:GCRankGridaniaFemaleText>} else {<ref:GCRankGridaniaMaleText>}>} else {<if(PLYR: 55 ? 1 ) {<if( 5 ) {<ref:GCRankUldahFemaleText>} else {<ref:GCRankUldahMaleText>}>} else {}>}>}>", "");
            }

            if (wantedLine.Contains("<If(PlayerParameter(4))"))
            {
                var genderTexts = ReplaceGenderText(wantedLine);
                var localMaleText = genderTexts[0];
                var localFemaleText = genderTexts[1];

                wantedLine = localMaleText;
                wantedLine2 = localFemaleText;
            }

            wantedLine = wantedLine.Replace("<if(PLYR: 55 ? 1 ) {<if(PLYR: 55 ) { 128 } else {<if(PLYR: 55 ? 1 ) {<ref:GCRankUldahMaleText>} else {}> }>} else {", "");
            wantedLine2 = wantedLine2.Replace("<if(PLYR: 55 ? 1 ) {<if(PLYR: 55 ) { 128 } else {<if(PLYR: 55 ? 1 ) {<ref:GCRankUldahFemaleText>} else {}> }>} else {", "");
            wantedLine = wantedLine.Replace("<if(PLYR: 12 ) { 13 } else {<if(PLYR: 12 ) { 5 } else {", "");
            wantedLine2 = wantedLine2.Replace("<if(PLYR: 12 ) { 13 } else {<if(PLYR: 12 ) { 5 } else {", "");

            wantedLine = Regex.Replace(wantedLine, "<if.*?}>}>", "");
            wantedLine2 = Regex.Replace(wantedLine2, "<if.*?}>}>", "");
            wantedLine = Regex.Replace(wantedLine, "<if.*?}>", "");
            wantedLine2 = Regex.Replace(wantedLine2, "<if.*?}>", "");
            wantedLine = Regex.Replace(wantedLine, "<.*?>", "");
            wantedLine2 = Regex.Replace(wantedLine2, "<.*?>", "");
            wantedLine = wantedLine.Replace("}>}>", "");
            wantedLine2 = wantedLine2.Replace("}>}>", "");
            wantedLine = wantedLine.Replace("}>", "");
            wantedLine2 = wantedLine2.Replace("}>", "");
            wantedLine = wantedLine.Replace("} else {", "");
            wantedLine2 = wantedLine2.Replace("} else {", "");

            return new string[] { wantedLine, wantedLine2 };
        }
        private static string CleanUpLine(string line)
        {
            line = line
                .Replace("<br>", " ")
                .Replace("<i>", "")
                .Replace("</i>", "")
                .Replace(" <forename>", "")
                .Replace(" <surname>", "")
                .Replace(" <forename surname>", "")
                .Replace("<forename>", "")
                .Replace("<surname>", "")
                .Replace("<forename surname>", "")
                .Replace("<?0x32>", "Handwerker")
                .Replace(",,", ",")
                .Replace("（★未使用／削除予定★）", "")
                .Replace("__", "_")
                .Replace("\u0003", "")
                .Replace("\u0005", "")
                .Replace("\\u0003", "")
                .Replace("\\u0005", "")
                .Replace("\\u00e1", "á")
                .Replace("\\u00e9", "é")
                .Replace("\\u00ed", "í")
                .Replace("\\u00f3", "ó")
                .Replace("\\u00fa", "ú")
                .Replace("\\u00f1", "ñ")
                .Replace("\\u00e0", "à")
                .Replace("\\u00e8", "è")
                .Replace("\\u00ec", "ì")
                .Replace("\\u00f2", "ò")
                .Replace("+", "plus ")
                .Replace("=", "gleicht")
                .Replace("(仮)にぎやかし381_森", "")
                .Replace("(仮)にぎやかし381_森", "")
                .Replace("「XXXX]で「イベントアイテムB」を使い、 しばらく影で待機するんだ。 そうすれば、お目当ての敵がやってくるからね。", "")
                .Replace("「XXXX]で「イベントアイテムB」を使い、 しばらく影で待機するんだ。 そうすれば、お目当ての敵がやってくるからね。", "")
                .Replace("さぁ、いっておいで。 「XXXX]で「イベントアイテムB」を使い、しばらく影で待機するんだ。 そうすれば、お目当ての敵がやってくるからね。", "")
                .Replace("さぁ、いっておいで。 「XXXX]で「イベントアイテムB」を使い、しばらく影で待機するんだ。 そうすれば、お目当ての敵がやってくるからね。", "")
                .Replace("EItem：クエスト：GaiUsa912_02を使ってBnpc_GaiUsa912_00を倒す", "")
                .Replace("EItem：クエスト：GaiUsa912_02を使ってBnpc_GaiUsa912_00を倒す", "")
                .Replace("（コスタの柱にアイテムを使うとアンモがPOP*5）", "")
                .Replace("（コスタの柱にアイテムを使うとアンモがPOP*5）", "")
                .Replace("�V", "")
                .Replace("�V", "")
                .Replace("�3", "")
                .Replace("�3", "")
                .Replace("�E", "")
                .Replace("�E", "")
                .Replace("�:", "")
                .Replace("�:", "")
                .Replace("�2", "")
                .Replace("�2", "")
                .Replace("�>", "")
                .Replace("�>", "")
                .Replace("�A", "")
                .Replace("�A", "")
                .Replace("�@", "")
                .Replace("�@", "")
                .Replace("�G", "")
                .Replace("\\u00f9", "ù");
            line = Regex.Replace(line, @"(\.{3})(\w)", "$1 $2");
            line = Regex.Replace(line, "[“”]", "\"");
            bool isOpeningQuote = true;
            line = Regex.Replace(line, "\"", match =>
            {
                if (isOpeningQuote)
                {
                    isOpeningQuote = false;
                    return "“";
                }
                else
                {
                    isOpeningQuote = true;
                    return "”";
                }
            });
            line = Regex.Replace(line, "  ", " ");
            line = Regex.Replace(line, @"\(-.*?-\)", "");
            if (line.StartsWith(","))
                line = line.Substring(2);
            line = line.Trim();

            while (line.Length > 0 && Regex.IsMatch(line.Substring(0, 1), "[!?.,]"))
                line = line.Substring(1);

            return line.Trim();
        }

        private static List<string> ReplaceGenderText(string text)
        {
            var returnVal = new List<string>();

            var maleText = text;
            var femaleText = text;
            while (maleText.Contains("<If(PlayerParameter(4))"))
            {
                var startIndexCode = maleText.IndexOf("<If(PlayerParameter(4))");
                var ifBeginText = maleText.Substring(startIndexCode);
                var endIndexCode = ifBeginText.IndexOf("If>") + 3;
                var codeSubstring = maleText.Substring(startIndexCode, endIndexCode);
                var femaleStartIndex = codeSubstring.IndexOf(")>") + 2;
                var femaleEndIndex = codeSubstring.IndexOf("<Else");
                var femaleSubstring = codeSubstring.Substring(femaleStartIndex, femaleEndIndex - femaleStartIndex);
                femaleText = femaleText.Replace(codeSubstring, femaleSubstring);

                var maleStartIndex = codeSubstring.IndexOf("Else/>", femaleEndIndex) + 6;
                var maleEndIndex = codeSubstring.IndexOf("</If>", femaleEndIndex);
                var maleSubstring = codeSubstring.Substring(maleStartIndex, maleEndIndex - maleStartIndex);
                maleText = maleText.Replace(codeSubstring, maleSubstring);
            }

            returnVal.Add(maleText);
            returnVal.Add(femaleText);
            return returnVal;
        }
    }
}
