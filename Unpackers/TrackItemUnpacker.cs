using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    static class TrackItemUnpacker
    {
        public static bool CanUnpack(ResourceType resourceType)
        {
            switch (resourceType)
            {
                case ResourceType.Bitmap:
                case ResourceType.Camera:
                case ResourceType.Ear:
                case ResourceType.Light:
                case ResourceType.Model:
                case ResourceType.Movie:
                    return true;

                default:
                    return false;
            }
        }

        public static string Unpack(BinaryReader source, ResourceType resourceType)
        {
            byte section = 47;
            var destination = new StringBuilder();
            var firstObjRef = new int?();
            var emptyObjRef = 0;

            switch (resourceType)
            {
                case ResourceType.Bitmap:
                    emptyObjRef = 1;
                    break;

                case ResourceType.Movie:
                    emptyObjRef = -1;
                    break;
            }

            while (source.BaseStream.Position < source.BaseStream.Length - 1)
            {
                var objRef = (int)source.ReadByte();
                if (objRef == 255)
                    break;

                if (objRef == section + 1)
                {
                    var nextByte = source.ReadByte();
                    source.BaseStream.Position--;

                    if (firstObjRef == null || firstObjRef == nextByte || firstObjRef == emptyObjRef)
                    {
                        section = (byte)objRef;
                        destination.AppendLine($"Section {section}");

                        if (section == 50 && firstObjRef == emptyObjRef)
                            break;

                        objRef = (int)source.ReadByte();
                    }
                }

                if (objRef == 128)
                    objRef = source.ReadInt32();

                if (firstObjRef == null)
                    firstObjRef = objRef;

                var length = firstObjRef == emptyObjRef ? 0 : source.ReadByte();
                var content = Enumerable.Range(0, length * 3)
                    .Select(i => source.ReadSingle())
                    .ToArray();

                destination.AppendLine($"    Object {objRef}: " + string.Join(", ", content));
            }

            return destination.ToString();
        }
    }
}