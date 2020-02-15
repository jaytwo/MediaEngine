using MediaEngine.Unpackers;
using System.Collections.Generic;
using System.Text;

namespace MediaEngine.Exporters
{
    static class TriangleExporter
    {
        public static Dictionary<int, List<short[]>> Export(List<Group> groups, List<short[]> allFaces, short[] faceGroupIds, int[] objectIndices)
        {
            var faceIndex = 0;
            short triangleCount = 0;

            var triangles = new List<short>[allFaces.Count];
            var faceVertices = new List<short[]>();
            
            foreach (var face in allFaces)
            {
                // Reverse winding direction
                triangles[faceIndex] = new List<short>(new[] { triangleCount++ });
                faceVertices.Add(new[] { face[1], face[3], face[2], (short)0 });

                // Convert quads
                if (face[0] == 4)
                {
                    triangles[faceIndex].Add(triangleCount++);
                    faceVertices.Add(new[] { face[4], face[3], face[1], (short)0 });
                }

                faceIndex++;
            }

            var facesByObject = new Dictionary<int, List<short[]>>();

            foreach (var i in objectIndices)
            {
                var faces = faceVertices;

                if (faceGroupIds != null)
                {
                    faces = new List<short[]>();

                    for (short f = 0; f < faceGroupIds.Length; f++)
                        if (faceGroupIds[f] == i)
                            foreach (var triangleIndex in triangles[f])
                                faces.Add(faceVertices[triangleIndex]);
                }

                facesByObject[i] = faces;
            }

            return facesByObject;
        }
    }
}