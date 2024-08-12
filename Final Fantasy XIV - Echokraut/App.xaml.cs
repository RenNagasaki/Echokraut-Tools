using RestoreWindowPlace;
using System.Configuration;
using System.Data;
using System.Windows;

namespace FF14_Echokraut
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public WindowPlace WindowPlace { get; }

        public App()
        {
            this.WindowPlace = new WindowPlace("placement_" + System.Environment.MachineName + ".config");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            this.WindowPlace.Save();
        }
    }

}
