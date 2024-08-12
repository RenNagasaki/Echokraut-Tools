using Microsoft.Win32;
using SaintCoinach;
using SaintCoinach.Ex;
using SaintCoinach.Xiv;
using System.IO;
using System.Windows;

namespace PrepareAlltalkTrainingData
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        public bool Closing = false;
        public MainWindow()
        {
            InitializeComponent();
            Instance = this;
        }

        private async void DoSomething()
        {
            var language = FF14Helper.GetLanguage(cBox_Language.Text);
            const string GameDirectory = @"G:\SteamLibrary\steamapps\common\FINAL FANTASY XIV Online";
            var realm = new SaintCoinach.ARealmReversed(GameDirectory, language);

            if (!realm.IsCurrentVersion)
            {
                const bool IncludeDataChanges = true;
                var updateReport = realm.Update(IncludeDataChanges);
            }

            var saveLoc = tBox_SaveLocation.Text;
            await Task.Run(() => GetScdHelper.GetExd(realm, language, saveLoc));
            //GetScdHelper.WorkCutScenes(realm, language, saveLoc);
            await Task.Run(() => GetScdHelper.WorkCutScenes(realm, language, saveLoc));

            lbl_progress.Content = "Done";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var path = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\SaintCoinach.History.zip";
            if (File.Exists(path))
                File.Delete(path);
            DoSomething();
        }

        private void btn_SelectSaveLocation_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog
            {
                DefaultDirectory = tBox_SaveLocation.Text,
                InitialDirectory = tBox_GameLocation.Text
            };

            if (folderDialog.ShowDialog() == true)
            {
                tBox_SaveLocation.Text = folderDialog.FolderName;
                // Do something with the result
            }
        }

        private void btn_SelectGameLocation_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog
            {
                DefaultDirectory = tBox_GameLocation.Text,
                InitialDirectory = tBox_GameLocation.Text
            };

            if (folderDialog.ShowDialog() == true)
            {
                tBox_GameLocation.Text = folderDialog.FolderName;
                // Do something with the result
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Closing = true;
        }
    }
}