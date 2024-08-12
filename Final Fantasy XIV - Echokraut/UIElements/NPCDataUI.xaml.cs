using FF14_Echokraut.DataClasses;
using FF14_Echokraut.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FF14_Echokraut.UIElements
{
    /// <summary>
    /// Interaktionslogik für NPCDataUI.xaml
    /// </summary>
    public partial class NPCDataUI : UserControl
    {
        string oldName = "";
        FF14NPCData npcData;
        bool loaded = false;

        public NPCDataUI(FF14NPCData npcData)
        {
            InitializeComponent();
            this.npcData = npcData;

            foreach (var voice in FF14WebSocketHelper.mappedVoices)
            {
                cBox_Voices.Items.Add(voice);
            }

            if (npcData.name != null)
            {
                tBox_NpcName.Text = XIVApiHelper.unCleanUpName(npcData.name);
                oldName = npcData.name;
                cBox_Voices.SelectedItem = npcData.voiceItem;
            }
            else
                cBox_Voices.SelectedIndex = 0;
            loaded = true;
        }

        private void tBox_NpcName_TextChanged(object sender, TextChangedEventArgs e)
        {
            lbl_Error.Content = "";

            if (loaded)
            {
                var npcName = XIVApiHelper.cleanUpName(tBox_NpcName.Text.Trim());
                if (!string.IsNullOrWhiteSpace(npcName))
                {
                    if (!FF14WebSocketHelper.npcDatas.ContainsKey(npcName))
                    {
                        FF14WebSocketHelper.npcDatas.Remove(oldName);
                        var voiceItem = cBox_Voices.SelectedItem as FF14Voice;
                        npcData.name = npcName;
                        npcData.race = voiceItem.race;
                        npcData.gender = voiceItem.gender;
                        npcData.voiceItem = voiceItem;

                        FF14WebSocketHelper.npcDatas.Add(npcData.name, npcData);
                        oldName = npcName;
                    }
                    else
                        lbl_Error.Content = "This NPC is already mapped";
                }
                else
                    lbl_Error.Content = "Please enter a NPC name";
            }
        }

        private void cBox_Voices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                var voiceItem = cBox_Voices.SelectedItem as FF14Voice;
                npcData.voiceItem = voiceItem;
            }
        }

        private void btn_Test_Click(object sender, RoutedEventArgs e)
        {
            FF14WebSocketHelper.generateVoice(Constants.TESTMESSAGE, npcData.voiceItem.voice, MainWindow.mainWindow.config.Language);
        }

        private void btn_Delete_Click(object sender, RoutedEventArgs e)
        {
            var npcName = XIVApiHelper.cleanUpName(tBox_NpcName.Text.Trim());
            if (!string.IsNullOrWhiteSpace(npcName))
            {
                if (FF14WebSocketHelper.npcDatas.ContainsKey(npcName))
                {
                    FF14WebSocketHelper.npcDatas.Remove(npcName);
                }
            }

            MainWindow.mainWindow.stack_NpcData.Children.Remove(this);
        }
    }
}
