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
                case ResourceType.Camera:
                case ResourceType.Ear:
                case ResourceType.Model:
                    return true;

                default:
                    return false;
            }
        }

        public static string Unpack(BinaryReader source)
        {
            var destination = new StringBuilder();
            var firstObjRef = new int?();
            byte section = 47;

            while (source.BaseStream.Position < source.BaseStream.Length - 1)
            {
                var objRef = (int)source.ReadByte();
                if (objRef == 255)
                    break;

                if (objRef == section + 1)
                {
                    var nextByte = source.ReadByte();
                    source.BaseStream.Position--;

                    if (firstObjRef == null || firstObjRef == nextByte || firstObjRef == 0)
                    {
                        section = (byte)objRef;
                        destination.AppendLine($"Section {section}");

                        if (section == 50 && firstObjRef == 0)
                            break;

                        objRef = (int)source.ReadByte();
                    }
                }

                if (objRef == 128)
                    objRef = source.ReadInt32();

                if (firstObjRef == null)
                    firstObjRef = objRef;

                var length = firstObjRef == 0 ? 0 : source.ReadByte();
                var content = Enumerable.Range(0, length * 3)
                    .Select(i => source.ReadSingle())
                    .ToArray();

                destination.AppendLine($"    Object {objRef}: " + string.Join(", ", content));
            }

            return destination.ToString();
        }
    }
}