using System;
using System.Collections.Generic;
using System.IO;

namespace MediaEngine.Unpackers
{
    static class ResourceUnpacker
    {
        public static List<Resource> Unpack(BinaryReader source, string path)
        {
            var resources = new List<Resource>();

            if (source.ReadByte() == (byte)ResourceType.Header)
                source.BaseStream.Position--;

            while (true)
            {
                var resource = new Resource(source);
                resources.Add(resource);
                resource.Unpacker = GetUnpacker(resources, resource.ResourceType);

                // Force textures to something predictable because we unpack them after models refer to them
                var fileName = resource.ResourceType == ResourceType.Texture ? ".png" :
                    ("-" + resource.Name + Path.GetExtension(resource.Source));

                resource.Path = Path.Combine(path, resource.ResourceType.ToString(), resource.Index + fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(resource.Path));

                using (var output = File.Create(resource.Path))
                using (var destination = new BinaryWriter(output))
                {
                    try
                    {
                        resource.Unpacker.Unpack(source, destination);
                    }
                    catch (EndOfStreamException)
                    {
                        return resources;
                    }
                }
            }
        }

        private static Unpacker GetUnpacker(IEnumerable<Resource> resources, ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Header:
                    return new HeaderUnpacker();

                case ResourceType.Model:
                    return new ModelUnpacker();

                case ResourceType.Texture:
                case ResourceType.Bitmap:
                    return new BitmapUnpacker();

                case ResourceType.Text:
                    return new TextUnpacker();

                case ResourceType.Wave:
                case ResourceType.Sound3d:
                    return new WaveUnpacker();

                case ResourceType.Midi:
                    return new MidiUnpacker();

                case ResourceType.Camera:
                    return new CameraUnpacker();

                case ResourceType.Light:
                    return new LightUnpacker();

                case ResourceType.Ear:
                    return new EarUnpacker();

                case ResourceType.Movie:
                    return new MovieUnpacker();

                case ResourceType.Score:
                    return new ScoreUnpacker(resources);

                case ResourceType.Script:
                    return new ScriptUnpacker();

                default:
                    throw new NotImplementedException();
            }
        }
    }
}