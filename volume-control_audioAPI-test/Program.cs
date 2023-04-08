using System;
using VolumeControl.Log;

namespace volume_control_audioAPI_test
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Config settings = new();

            App app = new();

            try
            {
                int rc = app.Run(new MainWindow());
            }
            catch (Exception ex)
            {
                FLog.Log.Fatal("Application exited because of an unhandled exception:", ex);
            }
        }
    }
}
