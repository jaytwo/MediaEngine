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
        FaceCount = 17,
        IndexCount = 18,
        UnknownMarker19 = 19,
        UnknownArray22 = 22,
        Vertices = 32,
        FacesDouble = 33,
        UnknownMarker34 = 34,
        FacesSingle = 35,
        MaterialList = 36,
        UnknownArray113 = 113,
        UnknownArray115 = 115,
        MaterialPower = 144,
        MaterialAmbient = 145,
        MaterialEmissive = 146,
        MaterialSpecular = 147,
        UnknownShort149 = 149,
        UnknownShort150 = 150,
        UnknownShort151 = 151,
        MaterialName = 177,
    }

    /// <summary>
    /// See http://www.martinreddy.net/gfx/3d/3DS.spec
    /// </summary>
    class ModelUnpacker : Unpacker<ModelField>
    {
        private byte[] _vertices;
        private byte[] _faces;

        private readonly List<byte[]> _materials = new List<byte[]>();
        private readonly Dictionary<ModelField, float[]> _material = new Dictionary<ModelField, float[]>();

        protected override void Unpack(BinaryReader source, BinaryWriter destination, ModelField field)
        {
            switch (field)
            {
                case ModelField.UnknownMarker19:
                case ModelField.UnknownMarker34:
                    break;

                case ModelField.UnknownArray22:
                    var unknown22 = source.ReadBytes(_fieldValues[ModelField.VertexCount]);
                    break;

                case ModelField.Vertices:
                    _vertices = source.ReadBytes(12 * _fieldValues[ModelField.VertexCount]);
                    break;

                case ModelField.FacesSingle:
                    _fieldValues[field] = source.ReadByte();
                    var indices = source.ReadBytes(_fieldValues[ModelField.IndexCount]);
                    UnpackFaces(indices.Select(b => (short)b).ToArray());
                    break;

                case ModelField.FacesDouble:
                    UnpackFaces(Enumerable.Range(0, _fieldValues[ModelField.IndexCount])
                        .Select(i => source.ReadInt16()).ToArray());

                    if (_fieldValues[ModelField.IndexCount] != 0)
                        switch (source.ReadByte())
                        {
                            case 34:
                                var unknown34 = Enumerable.Range(0, _fieldValues[ModelField.FaceCount])
                                    .Select(i => source.ReadInt16())
                                    .ToArray();
                                break;

                            default:
                                source.BaseStream.Position--;
                                break;
                        }

                    break;

                case ModelField.MaterialList:
                    var bitsPerItem = source.ReadByte();
                    var byteCount = _fieldValues[ModelField.FaceCount] * bitsPerItem / 32.0;
                    var materialList = source.ReadBytes((int)Math.Ceiling(byteCount));
                    if (bitsPerItem == 16)
                        materialList = materialList.SelectMany(b => new[]
                        {
                            (byte)((b & 0xF0) >> 4),
                            (byte)(b & 0x0F)
                        })
                        .ToArray();

                    if (byteCount != Math.Ceiling(byteCount))
                        materialList = materialList.Take(materialList.Length - 1).ToArray();

                    /*destination.Write(Encoding.ASCII.GetBytes(
                        "MeshMaterialList {" + Environment.NewLine +
                        (1 + materialList.Max()) + "; // number of materials" + Environment.NewLine +
                        materialList.Length + "; // material for each face" + Environment.NewLine +
                        string.Join("," + Environment.NewLine, materialList) + ";;" + Environment.NewLine));*/
                    break;

                case ModelField.UnknownArray113:
                    var unknown12 = source.ReadBytes(12);
                    break;

                case ModelField.UnknownArray115:
                    var unknown24 = Enumerable.Range(0, 6)
                        .Select(i => source.ReadUInt32())
                        .Where(i => i != uint.MaxValue)
                        .ToArray();
                    break;

                case ModelField.MaterialPower:
                case ModelField.MaterialAmbient:
                case ModelField.MaterialEmissive:
                case ModelField.MaterialSpecular:
                    _material[field] = Enumerable.Range(0, 4)
                        .Select(i => source.ReadSingle())
                        .ToArray();
                    _material[field][3] = 1;
                    break;

                case ModelField.UnknownShort149:
                case ModelField.UnknownShort150:
                case ModelField.UnknownShort151:
                    _fieldValues[field] = source.ReadInt16();
                    break;

                case ModelField.MaterialName:
                    var name = Encoding.GetEncoding(932).GetString(source.ReadBytes(source.ReadInt32()));

                    /*destination.Write(Encoding.UTF8.GetBytes(
                            "Material " + name + " {" + Environment.NewLine +
                            string.Join(";", _material[ModelField.MaterialAmbient]) + ";;" + Environment.NewLine +
                            _material[ModelField.MaterialPower][0] + ";" + Environment.NewLine +
                            string.Join(";", _material[ModelField.MaterialEmissive].Take(3)) + ";;" + Environment.NewLine +
                            "0.000000;0.000000;0.000000;;" + Environment.NewLine +
                            "}" + Environment.NewLine));*/
                    break;

                default:
                    _fieldValues[field] = source.ReadInt32();
                    break;
            }
        }

        private void UnpackFaces(short[] indices)
        {
            var faces = new List<short[]>();
            var i = 0;
            while (i < indices.Length)
                faces.Add(Enumerable.Range(0, indices[i] + 1).Select(j => indices[i++]).ToArray());

            using (var faceStream = new MemoryStream())
            {
                using (var faceWriter = new BinaryWriter(faceStream, Encoding.ASCII, true))
                    foreach (var face in faces)
                    {
                        faceWriter.Write(face[2]);
                        faceWriter.Write(face[1]);
                        faceWriter.Write(face[3]);
                        faceWriter.Write((short)0x0007); // face info

                        // Convert quads
                        if (face[0] == 4)
                        {
                            faceWriter.Write(face[3]);
                            faceWriter.Write(face[1]);
                            faceWriter.Write(face[4]);
                            faceWriter.Write((short)0x0007); // face info
                        }
                    }

                _faces = faceStream.ToArray();
            }
        }

        protected override void OnFinish(BinaryWriter destination)
        {
            var length = _faces.Length + _vertices.Length + 22;

            destination.Write((ushort)0x4D4D); // MAIN3DS chunk
            destination.Write(length + 20); // chunk length

            destination.Write((ushort)0x3D3D); // EDIT3DS chunk
            destination.Write(length + 14); // chunk length

            destination.Write((ushort)0x4000); // EDIT_OBJECT chunk
            destination.Write(length + 8); // chunk length
            destination.Write(Encoding.ASCII.GetBytes("1")); // object name
            destination.Write((byte)0); // object name terminator

            destination.Write((ushort)0x4100); // OBJECT_TRIMESH chunk
            destination.Write(length); // chunk length

            destination.Write((ushort)0x4110); // TRI_VERTEXL chunk
            destination.Write(_vertices.Length + 8); // chunk length
            destination.Write((ushort)(_vertices.Length / 12)); // total vertices
            destination.Write(_vertices);

            destination.Write((ushort)0x4120); // TRI_FACEL1 chunk
            destination.Write(_faces.Length + 8); // chunk length
            destination.Write((ushort)(_faces.Length / 8)); // total polygons
            destination.Write(_faces);
        }
    }
}
