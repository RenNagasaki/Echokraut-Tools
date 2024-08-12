using FF14_Echokraut.DataClasses;
using FF14_Echokraut.Enums;
using FF14_Echokraut.Exceptions;
using FF14_Echokraut.Helpers;
using FF14_Echokraut.Helpers.Backend;
using FF14_Echokraut.UIElements;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using WebSocketSharp;

namespace FF14_Echokraut
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static internal MainWindow mainWindow;
        internal Config config;

        public MainWindow()
        {
            InitializeComponent();
            mainWindow = this;
            this.Title +=  " v" + Assembly.GetEntryAssembly().GetName().Version;

            ServicePointManager.DefaultConnectionLimit = 20;
            ((App)Application.Current).WindowPlace.Register(this);

            loadSettings();

            btn_connectFF14.Background = System.Windows.Media.Brushes.Red;
            btn_AddNpcData.IsEnabled = false;
            btn_ConnectBackend.Background = System.Windows.Media.Brushes.Red;
        }

        void loadNPCData()
        {
            FF14WebSocketHelper.loadNPCData();

            foreach(var npcData in FF14WebSocketHelper.npcDatas.Values)
            {
                addNPCData(npcData);
            }

            if (!File.Exists(Constants.UNVOICEDNPCS))
                saveNPCData();
        }

        void loadSettings()
        {
            config = System.Text.Json.JsonSerializer.Deserialize<Config>(File.ReadAllText("Config.json"));
            if (config.Language == null)
                config.Language = "German";

            tBox_Ff14Url.Text = config.FF14UrlBase;
            tBox_XivApiUrl.Text = config.XIVAPIUrlBase;
            tBox_XivApiPath.Text = config.XIVAPIPath;
            tBox_AlltalkUrl.Text = config.UrlBase;
            tBox_AlltalkStreamPath.Text = config.GeneratePath;
            tBox_AlltalkReadyPath.Text = config.ReadyPath;
            tBox_AlltalkVoicesPath.Text = config.VoicePath;
            tBox_AlltalkModelPath.Text = config.SetModelFT;
            tBox_AlltalkStartPath.Text = config.StartPath;
            tBox_AlltalkStopPath.Text = config.StopPath;
            check_TopMost.IsChecked = config.TopMost;
            check_QueueText.IsChecked = config.QueueText;
        }

        void saveSettings()
        {
            config.FF14UrlBase = tBox_Ff14Url.Text;
            config.XIVAPIUrlBase = tBox_XivApiUrl.Text;
            config.XIVAPIPath = tBox_XivApiPath.Text;
            config.UrlBase = tBox_AlltalkUrl.Text;
            config.GeneratePath = tBox_AlltalkStreamPath.Text;
            config.ReadyPath = tBox_AlltalkReadyPath.Text;
            config.VoicePath = tBox_AlltalkVoicesPath.Text;
            config.SetModelFT = tBox_AlltalkModelPath.Text;
            config.StartPath = tBox_AlltalkStartPath.Text;
            config.StopPath = tBox_AlltalkStopPath.Text;
            config.TopMost = check_TopMost.IsChecked;
            config.QueueText = check_QueueText.IsChecked;
            File.WriteAllText("Config.json", System.Text.Json.JsonSerializer.Serialize<Config>(config));
        }

        internal void addNPCData(FF14NPCData npcData)
        {
            stack_NpcData.Children.Remove(btn_AddNpcData);
            NPCDataUI npcDataUI = new NPCDataUI(npcData);
            stack_NpcData.Children.Add(npcDataUI);
            stack_NpcData.Children.Add(btn_AddNpcData);
        }

        internal void saveNPCData()
        {
            File.WriteAllText(Constants.UNVOICEDNPCS, System.Text.Json.JsonSerializer.Serialize(FF14WebSocketHelper.npcDatas, new JsonSerializerOptions() { WriteIndented = true }));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            FF14WebSocketHelper.stopThread = true;
            saveNPCData();
            saveSettings();
        }

        private void btn_connectFF14_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (btn_ConnectBackend.IsEnabled)
                {
                    btn_connectBackend_Click(sender, e);
                }

                FF14WebSocketHelper.setup(tBox_Ff14Url.Text, tBox_XivApiUrl.Text, tBox_XivApiPath.Text);
            }
            catch (Exception ex)
            {
                LogHelper.logData(ex.ToString());
            }
        }

        private void check_TopMost_Checked(object sender, RoutedEventArgs e)
        {
            window_Main.Topmost = ((System.Windows.Controls.CheckBox)sender).IsChecked.Value;
        }

        private void btn_TestMessage_Click(object sender, RoutedEventArgs e)
        {
            //FF14WebSocketHelper.generateVoice(Constants.TESTMESSAGE, Constants.NARRATORVOICE, "German");
            FF14WebSocketHelper.generateVoice("Damit wären wir wieder alle beisammen. Wie ich hörte, haben wir auch die Ergebnisse aus Thavnair. Damit können wir nun austauschen, was wir bisher gelernt haben.", "Female_Lalafell_Krile.wav", "German");
        }

        private void btn_connectBackend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FF14WebSocketHelper.prepareHelpers(tBox_AlltalkUrl.Text, tBox_AlltalkVoicesPath.Text, tBox_AlltalkStreamPath.Text, tBox_AlltalkStopPath.Text, tBox_AlltalkReadyPath.Text);
                loadNPCData();
            }
            catch (Exception ex)
            {
                LogHelper.logData(ex.ToString());
            }
        }

        private void btn_AddNpcData_Click(object sender, RoutedEventArgs e)
        {
            addNPCData(new FF14NPCData());
        }

        private void check_QueueText_Checked(object sender, RoutedEventArgs e)
        {
            FF14WebSocketHelper.queueText = check_QueueText.IsChecked.Value;
        }
    }
}