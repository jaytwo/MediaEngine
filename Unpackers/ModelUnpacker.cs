using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MediaEngine.Unpackers
{
    enum ModelField
    {
        VertexCount = 16,
        UnknownLength17 = 17,
        IndexCount = 18,
        UnknownMarker19 = 19,
        UnknownArray22 = 22,
        Vertices = 32,
        FacesDouble = 33,
        UnknownMarker34 = 34,
        FacesSingle = 35,
        UnknownArray36 = 36,
        UnknownArray113 = 113,
        UnknownArray115 = 115,
        UnknownArray144 = 144,
        UnknownArray145 = 145,
        UnknownArray146 = 146,
        UnknownArray147 = 147,
        UnknownShort149 = 149,
        UnknownShort150 = 150,
        UnknownShort151 = 151,
        UnknownString177 = 177,
    }

    /// <summary>
    /// See http://www.cgdev.net/axe/x-file.html
    /// </summary>
    class ModelUnpacker : Unpacker<ModelField>
    {
        protected override void Unpack(BinaryReader source, BinaryWriter destination, ModelField field)
        {
            switch (field)
            {
                case ModelField.UnknownMarker19:
                case ModelField.UnknownMarker34:
                    break;

                case ModelField.UnknownArray22:
                    source.ReadBytes(_fieldValues[ModelField.VertexCount]);
                    break;

                case ModelField.Vertices:
                    var meshVertices = _fieldValues[ModelField.VertexCount];
                    var meshText = "xof 0302txt 0032" + Environment.NewLine // .x file header, 32-bit floats
                        + "Mesh 0 {" + Environment.NewLine
                        + meshVertices + "; // vertices" + Environment.NewLine
                        + string.Join(string.Empty, Enumerable.Range(0, meshVertices)
                            .Select(v => source.ReadSingle() + "; "
                                + source.ReadSingle() + "; "
                                + source.ReadSingle() + ";," + Environment.NewLine));
                    
                    meshText = meshText.Substring(0, meshText.Length - 3) + ";" + Environment.NewLine;
                    destination.Write(Encoding.ASCII.GetBytes(meshText)); 
                    break;

                case ModelField.FacesSingle:
                    _fieldValues[field] = source.ReadByte();
                    var indices = source.ReadBytes(_fieldValues[ModelField.IndexCount]);
                    destination.Write(UnpackFaces(indices.Select(b => (short)b).ToArray()));
                    break;

                case ModelField.FacesDouble:
                    destination.Write(UnpackFaces(Enumerable.Range(0, _fieldValues[ModelField.IndexCount])
                        .Select(i => source.ReadInt16()).ToArray()));

                    if (_fieldValues[ModelField.IndexCount] != 0)
                        switch (source.ReadByte())
                        {
                            case 34:
                                source.ReadBytes(2 * _fieldValues[ModelField.UnknownLength17]);
                                break;

                            case 36:
                                var bitsPerItem = source.ReadByte();
                                var byteCount = _fieldValues[ModelField.UnknownLength17] * bitsPerItem / 32.0;
                                source.ReadBytes((int)Math.Ceiling(byteCount));
                                break;

                            default:
                                throw new NotImplementedException();
                        }

                    break;

                case ModelField.UnknownArray36:
                    _fieldValues[field] = source.ReadInt16();
                    
                    while (source.ReadByte() != 150)
                    { }

                    source.BaseStream.Position--;
                    break;

                case ModelField.UnknownArray113:
                    source.ReadBytes(12);
                    break;

                case ModelField.UnknownArray115:
                    source.ReadBytes(24);
                    break;

                case ModelField.UnknownArray144:
                case ModelField.UnknownArray145:
                case ModelField.UnknownArray146:
                case ModelField.UnknownArray147:
                    source.ReadBytes(16);
                    break;

                case ModelField.UnknownShort149:
                case ModelField.UnknownShort150:
                case ModelField.UnknownShort151:
                    _fieldValues[field] = source.ReadInt16();
                    break;

                case ModelField.UnknownString177:
                    var unknownLength = source.ReadInt32();
                    var unknownString = Encoding.GetEncoding(932).GetString(source.ReadBytes(unknownLength));
                    break;

                default:
                    _fieldValues[field] = source.ReadInt32();
                    break;
            }
        }

        private static byte[] UnpackFaces(short[] indices)
        {
            var faces = new List<string>();
            var i = 0;

            while (i < indices.Length)
                faces.Add(string.Join(string.Empty, Enumerable.Range(0, indices[i] + 1)
                    .Select(j => indices[i++] + ";")));

            return Encoding.ASCII.GetBytes(faces.Count + "; // faces" + Environment.NewLine +
                string.Join("," + Environment.NewLine, faces) +
                ";" + Environment.NewLine + "}" + Environment.NewLine);
        }
    }
}
