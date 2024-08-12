// See https://aka.ms/new-console-template for more information
using FF14_Echokraut.Helpers;
using FF14_Echokraut.Helpers.Backend;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

string toBeDatasetted = @"G:\audiofiles\Saint Coinach Cmd\2024.06.18.0000.0000\rawexd\";
string voiceToBeSorted = @"G:\audiofiles\gdrive\aud2\cut\ex5\";
string pathForNew = @"G:\audiofiles\gdrive\aud2\Converted";
string pathForNewQuests = @"H:\Meine Ablage\FFXIV\aud2\Unvoiced";
string pathForDataSets = "G:\\audiofiles\\gdrive\\aud2\\MergeThat";
string language = "de";

var folders = Directory.GetDirectories(toBeDatasetted);
Dictionary<string, List<string>> characterLines = new Dictionary<string, List<string>>();
Dictionary<string, List<string>> characterLinesCutMan = new Dictionary<string, List<string>>();
Dictionary<string, List<string>> characterLinesOrig = new Dictionary<string, List<string>>();
var linesToCheck = new List<string>();

Console.WriteLine("Enter command: ");
var command = Console.ReadLine().ToLower();

while (command != "stop" && command != "quit")
{
    if (command == "create")
    {
        FixFiles();
        //WorkDefaultTalk();
        foreach (var folder in folders)
        {
            Console.WriteLine(Path.GetFileName(folder));
            switch (Path.GetFileName(folder))
            {
                case "cut_scene":
                    WorkCutScenes(folder);
                    break;
                case "custom":
                case "quest":
                    //WorkCutMan(folder);
                    break;
                case "dungeon":
                case "opening":
                case "raid":
                case "warp":
                case "content":
                    //WorkQuests(folder);
                    break;
            }
        }
        //SaveData();
    }
    else if (command == "cutman")
    {
        foreach (var folder in folders)
        {
            Console.WriteLine(Path.GetFileName(folder));
            switch (Path.GetFileName(folder))
            {
                case "quest":
                    WorkCutMan(folder);
                    break;
            }
        }
    }
    else if (command == "testcreate")
    {
        foreach (var folder in folders)
        {
            Console.WriteLine(Path.GetFileName(folder));
            switch (Path.GetFileName(folder))
            {
                case "cut_scene":
                    //WorkCutScenes();
                    break;
                case "custom":
                case "quest":
                case "dungeon":
                case "opening":
                case "raid":
                case "warp":
                case "content":
                    WorkQuests(folder);
                    break;
            }
        }

        File.WriteAllLines(pathForNewQuests + @"\LinesToCheck.csv", linesToCheck);
    }
    else if (command == "generate")
    {
        Console.WriteLine("Please enter the name of the character to generate");
        var character = Console.ReadLine();
        var subFolder = Directory.GetDirectories(pathForNewQuests);
        var npcFolders = new List<string>();

        foreach (var folder in subFolder)
        {
            npcFolders.AddRange(Directory.GetDirectories(folder).ToList());
        }

        var npcFolder = npcFolders.Find(p => Path.GetFileName(p) == character);
        var lines = File.ReadLines(npcFolder + "\\" + "UnvoicedLines.csv").ToList();
        var donePath = npcFolder + "\\" + "DonevoicedLines.csv";
        var linesDone = File.Exists(donePath) ? File.ReadLines(donePath).ToList() : new List<string>();
        var allTalk = new AllTalkHelper("http://192.168.178.44:7851", "/api/tts-generate");

        for (int i = 0; i < lines.Count; i++)
        {
            string id = i.ToString();

            if (!linesDone.Contains(id))
            {
                var line = lines[i];
                var result = allTalk.GenerateAudioFileFromVoice(line, character, "de").Result;

                if (result)
                {
                    linesDone.Add(id);
                    File.AppendAllLines(donePath, new List<string>() { id });
                }
            }
        }
    }
    else if (command == "megaset")
    {
        var files = Directory.GetFiles(pathForDataSets, "*.*", SearchOption.AllDirectories);

        List<string> newTrainFile = new List<string>() { "audio_file|text|speaker_name" };
        List<string> newTestFile = new List<string>() { "audio_file|text|speaker_name" };

        foreach (var file in files) 
        {
            var data = File.ReadAllLines(file).ToList();

            if (data[0].Contains("audio_file|text|speaker_name"))
                data.RemoveAt(0);

            if (file.Contains("_train"))
                newTrainFile.AddRange(data);
            else
                newTestFile.AddRange(data);
        }

        File.WriteAllLines($"{pathForDataSets}\\00DONE\\metadata_train.csv", newTrainFile);
        File.WriteAllLines($"{pathForDataSets}\\00DONE\\metadata_eval.csv", newTestFile);
    }

    Console.WriteLine("Enter command: ");
    command = Console.ReadLine().ToLower();
}

void FixFiles()
{
    Console.WriteLine("Fixing files");
    var files = Directory.GetFiles(toBeDatasetted, "*.*", SearchOption.AllDirectories).ToList();

    foreach (var file in files)
    {
        var fileData = File.ReadAllText(file);

        if (Regex.IsMatch(fileData, "(?<=[a-z/♪>;\\-:“ .,?!☆])\\r?\\n"))
        {
            fileData = Regex.Replace(fileData, "(?<=[a-z/♪>;\\-:“ .,?!☆])\\r?\\n", "<br>");

            File.WriteAllText(file, fileData);
            Console.WriteLine("Fixed file");
        }
    }
    Console.WriteLine("Fixed all files");
}

void WorkDefaultTalk()
{
    var defaultTalkLines = File.ReadAllLines(toBeDatasetted + "DefaultTalk.csv").ToList();
    var npcBaseLines = File.ReadAllLines(toBeDatasetted + "ENpcBase.csv").ToList();
    var npcResidentLines = File.ReadAllLines(toBeDatasetted + "ENpcResident.csv").ToList();
    var mappedDefaultTalkLines = new Dictionary<string, List<string>>();
    var npcTalkMap = new Dictionary<string, string>();

    defaultTalkLines.RemoveAt(0);
    defaultTalkLines.RemoveAt(0);
    defaultTalkLines.RemoveAt(0);
    npcBaseLines.RemoveAt(0);
    npcBaseLines.RemoveAt(0);
    npcBaseLines.RemoveAt(0);
    npcResidentLines.RemoveAt(0);
    npcResidentLines.RemoveAt(0);
    npcResidentLines.RemoveAt(0);
    foreach (var line in defaultTalkLines)
    {
        var lineFixed = line.Replace(",\"", "|\"");
        var lineFixedSplit = lineFixed.Split('|');
        var lineSplit = lineFixedSplit[0].Split(',');
        var talkId = lineSplit[0];
        var text1 = lineFixedSplit[1];
        var text2 = lineFixedSplit[2];
        var text3 = lineFixedSplit[3];

        var defaultLines = new List<string>();

        if (text1 != "\"\"" && text1 != "\"0\"")
        {
            var textLines = WorkText(text1);
            if (textLines != null)
            {
                if (textLines[0] != textLines[1])
                    defaultLines.AddRange(textLines);
                else
                    defaultLines.Add(textLines[0]);
            }
        }
        if (text2 != "\"\"" && text2 != "\"0\"")
        {
            var textLines = WorkText(text2);

            if (textLines != null)
            {
                if (textLines[0] != textLines[1])
                    defaultLines.AddRange(textLines);
                else
                    defaultLines.Add(textLines[0]);
            }
        }
        if (text3 != "\"\"" && text3 != "\"0\"")
        {
            var textLines = WorkText(text3);

            if (textLines != null)
            {
                if (textLines[0] != textLines[1])
                    defaultLines.AddRange(textLines);
                else
                    defaultLines.Add(textLines[0]);
            }
        }

        if (defaultLines.Count > 0)
            mappedDefaultTalkLines.Add(talkId, defaultLines);
    }

    foreach (var line in npcBaseLines) {
        var lineSplit = line.Split(',');
        var npcId = lineSplit[0];

        var defaultTalkId = "";
        for(int i = 3; i < 34; i++)
        {
            var id = lineSplit[i];

            if (id != "0" && (id.StartsWith("58") || id.StartsWith("59")) && id.Length == 6)
            {
                defaultTalkId = id;
                break;
            }
        }    

        if (!string.IsNullOrWhiteSpace(defaultTalkId))
            npcTalkMap.Add(npcId, defaultTalkId);
    }

    foreach (var line in npcResidentLines)
    {
        var lineSplit = line.Split(',');
        var npcId = lineSplit[0];
        var npcName = lineSplit[1];
        npcName = Regex.Replace(npcName, @"[^a-zA-Z0-9 _-]", "").Replace(" ", "_").Replace("-", "_");

        if (npcTalkMap.TryGetValue(npcId, out _) && mappedDefaultTalkLines.TryGetValue(npcTalkMap[npcId], out _))
        {
            var lines = mappedDefaultTalkLines[npcTalkMap[npcId]];
            if (!characterLines.ContainsKey(npcName))
            {
                characterLines.Add(npcName, new List<string>());
                characterLinesOrig.Add(npcName, new List<string>());
            }
            characterLines[npcName].AddRange(lines);
        }
    }
}

string cleanUpLine(string line)
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

void WorkCutMan(string folder)
{
    var audioFiles = Directory.GetFiles(voiceToBeSorted, "*.*", SearchOption.AllDirectories).ToList().FindAll(p => p.Contains("_" + language));
    var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories).ToList();

    var maxFiles = files.Count;
    var actFile = 0;
    foreach (var file in files)
    {
        actFile++;
        var fileData = File.ReadAllLines(file).ToList();
        fileData.RemoveAt(0);
        foreach (var audioLine in fileData)
        {
            var audioLineFixed = audioLine.Replace(",\"", "|\"");
            var audioLineSplit = audioLineFixed.Split('|');

            if (audioLineSplit.Length < 3 || string.IsNullOrWhiteSpace(audioLineSplit[1]) || string.IsNullOrWhiteSpace(audioLineSplit[2]))
                continue;

            var audioFile = audioLineSplit[1];
            audioFile = audioFile.Substring(1, audioFile.Length - 2);
            var audioFileSplit = audioFile.Split('_');

            if (audioFileSplit.Length > 4 && int.TryParse(audioFileSplit[2], out _) && int.TryParse(audioFileSplit[3], out _))
            {
                var audioPath = $"vo_{audioFileSplit[1].ToLower()}_{audioFileSplit[3]}";
                var audioFilesFound = audioFiles.FindAll(p => p.Contains(audioPath));
                var character = audioFileSplit[4].ToLower();
                character = Regex.Replace(character, "^(.)", text => text.ToString().ToUpper());
                var text = audioLineSplit[2];
                var wantedLines = WorkText(text);
                if (wantedLines == null)
                    continue;

                var wantedLine = wantedLines[0];
                var wantedLine2 = wantedLines[1];
                var path = pathForNew + @"\" + character;
                var pathAudio = path + @"\wavs";

                foreach (var audioFileFound in audioFilesFound)
                {
                    var fileName = Path.GetFileName(audioFileFound);
                    var localText = wantedLines[0];
                    if (audioFileFound.Contains("_f_de"))
                    {
                        localText = wantedLines[1];
                    }

                    if (!Directory.Exists(pathAudio))
                        Directory.CreateDirectory(pathAudio);
                    if (!File.Exists(pathAudio + @"\" + fileName))
                        File.Copy(audioFileFound, pathAudio + @"\" + fileName);
                    if (!characterLinesCutMan.ContainsKey(character))
                    {
                        characterLinesCutMan.Add(character, new List<string>());
                    }
                    characterLinesCutMan[character].Add("wavs\\" + fileName + "|" + localText + "|" + character);

                    if (localText.Contains("("))
                        Console.WriteLine(fileName + "|" + localText + "|" + character);
                }
            }
        }

        Console.WriteLine(string.Format("Reading files: {0}/{1}", actFile, maxFiles));
    }

    maxFiles = characterLinesCutMan.Keys.Count;
    actFile = 0;
    foreach (var pair in characterLinesCutMan)
    {
        var actLine = 0;
        actFile++;
        if (!File.Exists(pathForNew + @"\" + pair.Key + @"\metadata_eval.csv"))
            File.AppendAllLines(pathForNew + @"\" + pair.Key + @"\metadata_eval.csv", new List<string>() { "audio_file|text|speaker_name" });
        foreach (var line in pair.Value)
        {
            if (actLine > 6)
                File.AppendAllLines(pathForNew + @"\" + pair.Key + @"\metadata_eval.csv", new List<string>() { line });
            else
                File.AppendAllLines(pathForNew + @"\" + pair.Key + @"\metadata_train.csv", new List<string>() { line });
            actLine++;

            if (actLine > 9)
                actLine = 0;
        }

        Console.WriteLine($"Saving datasets: {pair.Key} - {actFile}/{maxFiles}");
    }
}

void WorkQuests(string folder, int characterIndex = 2)
{
    var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories).ToList();

    var maxFiles = files.Count;
    var actFile = 0;
    foreach (var file in files)
    {
        actFile++;
        var fileData = File.ReadAllLines(file).ToList();
        fileData.RemoveAt(0);
        foreach (var audioLine in fileData)
        {
            var localCharIndex = characterIndex;
            var audioLineFixed = audioLine.Replace(",\"", "|\"");
            var audioLineSplit = audioLineFixed.Split('|');

            if (audioLineSplit.Length < 3 || string.IsNullOrWhiteSpace(audioLineSplit[1]) || string.IsNullOrWhiteSpace(audioLineSplit[2]))
                continue;

            var audioFile = audioLineSplit[1];
            audioFile = audioFile.Substring(1, audioFile.Length - 2);
            var audioFileSplit = audioFile.Split('_');

            if (audioFileSplit.Length > 4 && int.TryParse(audioFileSplit[2], out _) && int.TryParse(audioFileSplit[3], out _))
                continue;

            var character = "";
            if (audioFileSplit.Length > 2)
            {
                character = audioFileSplit[localCharIndex].ToLower();
                while ((character == "qib" || int.TryParse(character, out _)) && localCharIndex < audioFileSplit.Length - 1)
                {
                    localCharIndex++;
                    character = audioFileSplit[localCharIndex].ToLower();
                }
            }

            if (character == "qib" || int.TryParse(character, out _))
                character = audioFileSplit[1].ToLower();


            if (character.Length < 3 || character == "seq" || character == "todo" ||
                character.StartsWith("q1") || character.StartsWith("a1") || 
                character.StartsWith("q2") || character.StartsWith("a2") || 
                character.StartsWith("q3") || character.StartsWith("a3") || 
                character.StartsWith("q4") || character.StartsWith("a4") || 
                character.StartsWith("q5") || character.StartsWith("a5") || 
                character.StartsWith("q6") || character.StartsWith("a6") || 
                character.StartsWith("q7") || character.StartsWith("a7") || 
                character.StartsWith("q8") || character.StartsWith("a8") || 
                character.StartsWith("q9") || character.StartsWith("a9"))
                continue;

            character = Regex.Replace(character, "^(.)", text => text.ToString().ToUpper());

            var keys = characterLines.Keys.ToList();
            var charKey = keys.Find(p => p.Replace("_", "").ToLower() == character.ToLower());
            if (charKey != null && !string.IsNullOrWhiteSpace(charKey))
                character = charKey;

            var text = audioLineSplit[2];
            var wantedLines = WorkText(text);
            if (wantedLines == null)
                continue;

            var wantedLine = wantedLines[0];
            var wantedLine2 = wantedLines[1];

            //var startIndex = localMaleText.IndexOf("(");
            //var endIndex = localMaleText.IndexOf(")") + 1;
            //if (startIndex >= 0 && endIndex >= 0)
            //    localMaleText = localMaleText.Replace(localMaleText.Substring(startIndex, endIndex - startIndex), "");

            if (!characterLines.ContainsKey(character))
            {
                characterLines.Add(character, new List<string>());
                characterLinesOrig.Add(character, new List<string>());
            }
            characterLines[character].Add($"{wantedLine}");
            characterLinesOrig[character].Add($"{audioLineFixed}|{file}");
            if (wantedLine != wantedLine2)
            {
                characterLines[character].Add($"{wantedLine2}");
                characterLinesOrig[character].Add($"{audioLineFixed}|{file}");
            }

            if (Regex.IsMatch(wantedLine, @"[@#$^*_+=\[\]{}\\|<>]"))
                linesToCheck.Add($"{wantedLine}\t|\t{file}");
        }

        Console.WriteLine(string.Format("Reading files: {0}/{1}", actFile, maxFiles));
    }

}

string[] WorkText(string text)
{
    //LogHelper.logData($"Working on Text: {text}");
    text = text.Substring(1, text.Length - 2).Trim();
    text = cleanUpLine(text);

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

void SaveData()
{
    File.WriteAllLines(pathForDataSets + @"\LinesToCheck.csv", linesToCheck);

    var maxFiles = characterLines.Keys.Count;
    var actFile = 0;
    foreach (var pair in characterLines)
    {
        Console.WriteLine($"Saving UnvoicedData: {actFile}/{maxFiles} | {pair.Key} ");
        var voiceLines = pair.Value;
        var path = pathForNewQuests + @"\";

        path += pair.Key;
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        File.WriteAllLines(path + @"\UnvoicedLines.csv", voiceLines);
        actFile++;
    }

    maxFiles = characterLinesOrig.Keys.Count;
    actFile = 0;
    foreach (var pair in characterLinesOrig)
    {
        Console.WriteLine($"Saving Original: {actFile}/{maxFiles} | {pair.Key} ");
        var voiceLines = pair.Value;
        var path = pathForNewQuests + @"\";

        path += pair.Key;
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        File.WriteAllLines(path + @"\UnvoicedLinesOrig.csv", voiceLines);
        actFile++;
    }
}

List<string> ReplaceGenderText(string text)
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

void WorkCutScenes(string folder)
{
    var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories).ToList();
    var audioFiles = Directory.GetFiles(voiceToBeSorted, "*.*", SearchOption.AllDirectories).ToList().FindAll(p => p.Contains("_" + language));

    Dictionary<string, List<string>> characterLines = new Dictionary<string, List<string>>();


    var maxFiles = files.Count;
    var actFile = 0;
    foreach (var file in files)
    {
        actFile++;
        var fileData = File.ReadAllLines(file).ToList();
        fileData.RemoveAt(0);
        var maxAudioFiles = fileData.Count;
        var actAudioFile = 0;
        foreach (var audioLine in fileData)
        {
            actAudioFile++;
            if (audioLine.ToLower().Contains("voiceman"))
            {
                var audioLineFixed = audioLine.Replace(",\"", "|\"");
                var audioLineSplit = audioLineFixed.Split('|');
                var audioFile = audioLineSplit[1];
                var character = audioFile.Split('_')[4].ToLower().Replace("\"", "");
                audioFile = audioFile.Substring(0, audioFile.Length - character.Length - 1).Replace("TEXT", "vo").ToLower().Replace("\"", "");

                var text = audioLineSplit[2].Trim();
                var wantedLines = WorkText(text);

                if (wantedLines == null || String.IsNullOrWhiteSpace(wantedLines[0]))
                    continue;

                var path = pathForNew + @"\" + character;
                var pathAudio = path + @"\wavs";

                var realAudioFiles = audioFiles.FindAll(p => p.Contains(audioFile));

                foreach (var realAudioFile in realAudioFiles)
                {
                    var fileName = Path.GetFileName(realAudioFile);
                    var localText = wantedLines[0];
                    if (realAudioFile.Contains("_f_de"))
                    {
                        localText = wantedLines[1];
                    }

                    if (!Directory.Exists(pathAudio))
                        Directory.CreateDirectory(pathAudio);
                    File.Copy(realAudioFile, pathAudio + @"\" + fileName);

                    if (!characterLines.ContainsKey(character))
                        characterLines.Add(character, new List<string>());
                    characterLines[character].Add("wavs\\" + fileName + "|" + localText + "|" + character);

                    if (localText.Contains("("))
                        Console.WriteLine(fileName + "|" + localText + "|" + character);
                }
            }

            Console.WriteLine(string.Format("  ->  Checking audiofiles. {0}/{1}", actAudioFile, maxAudioFiles));
        }

        Console.WriteLine(string.Format("Creating datasets. {0}/{1}", actFile, maxFiles));
    }

    maxFiles = characterLines.Keys.Count;
    actFile = 0;
    foreach (var pair in characterLines)
    {
        var actLine = 0;
        actFile++;
        File.AppendAllLines(pathForNew + @"\" + pair.Key + @"\metadata_eval.csv", new List<string>() { "audio_file|text|speaker_name" });
        foreach (var line in pair.Value)
        {
            if (actLine > 6)
                File.AppendAllLines(pathForNew + @"\" + pair.Key + @"\metadata_eval.csv", new List<string>() { line });
            else
                File.AppendAllLines(pathForNew + @"\" + pair.Key + @"\metadata_train.csv", new List<string>() { line });
            actLine++;

            if (actLine > 9)
                actLine = 0;
        }

        Console.WriteLine(string.Format("Saving datasets. {0}/{1}", actFile, maxFiles));
    }
}