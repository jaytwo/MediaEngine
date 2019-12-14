using System;
using System.Collections.Generic;
using System.IO;

namespace MediaEngine.Unpackers
{
    static class ResourceUnpacker
    {
        public static IEnumerable<Resource> Unpack(BinaryReader source, string path)
        {
            if (source.ReadByte() == (byte)ResourceType.Header)
                source.BaseStream.Position--;

            while (true)
            {
                var resource = new Resource(source);
                yield return resource;

                // Force textures to something predictable because we unpack them after models refer to them
                var fileName = resource.ResourceType == ResourceType.Texture ? ".png" :
                    ("-" + resource.Name + Path.GetExtension(resource.Source));

                var name = Path.Combine(path, resource.ResourceType.ToString(), resource.Index + fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(name));

                using (var output = File.Create(name))
                using (var writer = new BinaryWriter(output))
                {
                    try
                    {
                        switch (resource.ResourceType)
                        {
                            case ResourceType.Header:
                                new HeaderUnpacker().Unpack(source, writer);
                                break;

                            case ResourceType.Model:
                                new ModelUnpacker().Unpack(source, writer);
                                break;

                            case ResourceType.Texture:
                            case ResourceType.Bitmap:
                                new BitmapUnpacker().Unpack(source, writer);
                                break;

                            case ResourceType.Text:
                                new TextUnpacker().Unpack(source, writer);
                                break;

                            case ResourceType.Wave:
                            case ResourceType.Sound3d:
                                new WaveUnpacker().Unpack(source, writer);
                                break;

                            case ResourceType.Midi:
                                new MidiUnpacker().Unpack(source, writer);
                                break;

                            case ResourceType.Camera:
                                new CameraUnpacker().Unpack(source, writer);
                                break;

                            case ResourceType.Light:
                                new LightUnpacker().Unpack(source, writer);
                                break;

                            case ResourceType.Ear:
                                new EarUnpacker().Unpack(source, writer);
                                break;

                            case ResourceType.Movie:
                                throw new NotImplementedException();

                            case ResourceType.Script:
                                new TrackUnpacker().Unpack(source, writer);
                                break;
                        }
                    }
                    catch (EndOfStreamException)
                    {
                        yield break;
                    }
                }
            }
        }
    }
}