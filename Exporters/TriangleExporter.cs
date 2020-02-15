using lib3ds.Net;
using System.Collections.Generic;

namespace MediaEngine.Exporters
{
    static class TriangleExporter
    {
        public static Dictionary<int, List<Lib3dsFace>> Export(List<ushort[]> faces, short[] faceGroupIds, int[] objectIndices)
        {
            var equivalentTriangles = new List<int>[faces.Count];
            var triangleFaces = new List<Lib3dsFace>();

            for (int i = 0; i < faces.Count; i++)
            {
                var face = faces[i];

                // Reverse winding direction
                equivalentTriangles[i] = new List<int>(new[] { triangleFaces.Count });
                triangleFaces.Add(new Lib3dsFace { index = new[] { face[1], face[3], face[2] } });

                // Split quads into triangles
                if (face[0] == 4)
                {
                    equivalentTriangles[i].Add(triangleFaces.Count);
                    triangleFaces.Add(new Lib3dsFace { index = new[] { face[4], face[3], face[1] } });
                }
            }

            var facesByObject = new Dictionary<int, List<Lib3dsFace>>();

            foreach (var i in objectIndices)
            {
                var objectFaces = triangleFaces;

                if (faceGroupIds != null)
                {
                    objectFaces = new List<Lib3dsFace>();

                    for (short f = 0; f < faceGroupIds.Length; f++)
                        if (faceGroupIds[f] == i)
                            foreach (var triangle in equivalentTriangles[f])
                                objectFaces.Add(triangleFaces[triangle]);
                }

                facesByObject[i] = objectFaces;
            }

            return facesByObject;
        }
    }
}