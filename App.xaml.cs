using MediaEngine.Unpackers;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace MediaEngine
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var dialog = new OpenFileDialog
            {
                Filter = "Unpacked runtime (*.MXRU;*.LCRU;*.LCLU)|*.MXRU;*.LCRU;*.LCLU" +
                    "|Packed runtime (*.MXR;*.LCR;*.LCL)|*.MXR;*.LCR;*.LCL"
            };

            if (dialog.ShowDialog() == true)
            {
                switch (Path.GetExtension(dialog.FileName)?.ToUpper())
                {
                    case ".MXR":
                    case ".LCR":
                    case ".LCL":
                        Inflater.Inflate(dialog.FileName);
                        break;
                }
            }

            Shutdown();
        }
    }
}