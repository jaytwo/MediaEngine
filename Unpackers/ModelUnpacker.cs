using MediaEngine.Exporters;
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
        UnknownInt112 = 112,
        UnknownArray113 = 113,
        UnknownInt114 = 114,
        UnknownArray115 = 115,
        Texture = 128,
        UnknownInt129 = 129,
        UnknownInt130 = 130,
        UnknownInt131 = 131,
        MaterialPower = 144,
        MaterialAmbient = 145,
        MaterialEmissive = 146,
        MaterialSpecular = 147,
        UnknownFloat148 = 148,
        UnknownShort149 = 149,
        UnknownShort150 = 150,
        UnknownShort151 = 151,
        UnknownFloat160 = 160,
        UnknownFloat161 = 161,
        UnknownFloat162 = 162,
        UnknownFloat163 = 163,
        UnknownInt164 = 164,
        UnknownInt165 = 165,
        UnknownInt166 = 166,
        UnknownInt176 = 176,
        Name = 177,
        UnknownInt178 = 178,
        UnknownInt192 = 192,
        UnknownInt193 = 193,
        UnknownInt194 = 194,
        UnknownInt195 = 195,
        UnknownFloat196 = 196,
        UnknownFloat197 = 197,
        UnknownInt198 = 198,
        UnknownInt199 = 199,
        UnknownInt200 = 200,
        UnknownInt201 = 201,
        UnknownInt202 = 202,
    }

    class SceneObject : Dictionary<ModelField, object> { }

    class ModelUnpacker : Unpacker<ModelField>
    {
        private byte[] _vertices;
        private byte[] _faces;
        private byte[] _faceMaterials;
        private List<short>[] _faceTriangles;
        
        private readonly List<SceneObject> _sceneObjects = new List<SceneObject>();

        protected override void Unpack(BinaryReader source, BinaryWriter destination, ModelField field)
        {
            switch (field)
            {
                case ModelField.UnknownMarker19:
                case ModelField.UnknownMarker34:
                    break;

                case ModelField.UnknownShort150:
                    _fieldValues.Add(field, source.ReadInt16());
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
                    _faceMaterials = source.ReadBytes((int)Math.Ceiling(byteCount));
                    if (bitsPerItem == 16)
                        _faceMaterials = _faceMaterials.SelectMany(b => new[]
                        {
                            (byte)((b & 0xF0) >> 4),
                            (byte)(b & 0x0F)
                        })
                        .ToArray();

                    if (byteCount != Math.Ceiling(byteCount))
                        _faceMaterials = _faceMaterials.Take(_faceMaterials.Length - 1).ToArray();
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
                    _sceneObjects.Last().Add(field, Enumerable.Range(0, 4)
                        .Select(i => source.ReadSingle())
                        .ToArray());
                    break;

                case ModelField.UnknownShort149:
                case ModelField.UnknownShort151:
                    _sceneObjects.Last().Add(field, source.ReadInt16());
                    break;

                case ModelField.UnknownInt112:
                case ModelField.UnknownInt114:
                case ModelField.Texture:
                case ModelField.UnknownInt129:
                case ModelField.UnknownInt130:
                case ModelField.UnknownInt131:
                case ModelField.UnknownInt164:
                case ModelField.UnknownInt165:
                case ModelField.UnknownInt166:
                case ModelField.UnknownInt176:
                case ModelField.UnknownInt178:
                case ModelField.UnknownInt192:
                case ModelField.UnknownInt193:
                case ModelField.UnknownInt194:
                case ModelField.UnknownInt195:
                case ModelField.UnknownInt198:
                case ModelField.UnknownInt199:
                case ModelField.UnknownInt200:
                case ModelField.UnknownInt201:
                case ModelField.UnknownInt202:
                    if (_sceneObjects.Count == 0 || _sceneObjects.Last().ContainsKey(field))
                        _sceneObjects.Add(new SceneObject());
                    _sceneObjects.Last().Add(field, source.ReadInt32());
                    break;

                case ModelField.UnknownFloat148:
                case ModelField.UnknownFloat160:
                case ModelField.UnknownFloat161:
                case ModelField.UnknownFloat162:
                case ModelField.UnknownFloat163:
                case ModelField.UnknownFloat196:
                case ModelField.UnknownFloat197:
                    _sceneObjects.Last().Add(field, source.ReadSingle());
                    break;

                case ModelField.Name:
                    _sceneObjects.Last().Add(field, Translator.ReadString(source));
                    break;

                default:
                    _fieldValues.Add(field, source.ReadInt32());
                    break;
            }
        }

        private void UnpackFaces(short[] indices)
        {
            var faces = new List<short[]>();
            var i = 0;
            while (i < indices.Length)
                faces.Add(Enumerable.Range(0, indices[i] + 1).Select(j => indices[i++]).ToArray());

            i = 0;
            short t = 0;
            _faceTriangles = new List<short>[faces.Count];

            using (var faceStream = new MemoryStream())
            {
                using (var faceWriter = new BinaryWriter(faceStream, Encoding.ASCII, true))
                    foreach (var face in faces)
                    {
                        _faceTriangles[i] = new List<short>(new[] { t++ });   
                        faceWriter.Write(face[1]);
                        faceWriter.Write(face[2]);
                        faceWriter.Write(face[3]);
                        faceWriter.Write((short)0); // face info

                        // Convert quads
                        if (face[0] == 4)
                        {
                            _faceTriangles[i].Add(t++);
                            faceWriter.Write(face[4]);
                            faceWriter.Write(face[1]);
                            faceWriter.Write(face[3]);
                            faceWriter.Write((short)0); // face info
                        }

                        i++;
                    }

                _faces = faceStream.ToArray();
            }
        }

        protected override bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            ModelExporter.Export(_sceneObjects, _vertices, _faces, _faceMaterials, _faceTriangles, destination);
            return true;
        }
    }
}
