using MediaEngine.Unpackers;
using Microsoft.Win32;
using System.IO;
using System.Linq;
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

                    default:
                        var assetsPath = Path.ChangeExtension(dialog.FileName, null) + " assets";
                        if (Directory.Exists(assetsPath))
                            foreach (var file in Directory.EnumerateFiles(assetsPath, "*", SearchOption.AllDirectories))
                                File.Delete(file);
                        else
                            Directory.CreateDirectory(assetsPath);

                        using (var reader = new BinaryReader(File.OpenRead(dialog.FileName)))
                        {
                            var casts = ResourceUnpacker.Unpack(reader, assetsPath).ToArray();

                            Translator.Save(Path.Combine(assetsPath, "TranslationKeys.txt"));
                        }

                        break;
                }
            }

            Shutdown();
        }
    }
}