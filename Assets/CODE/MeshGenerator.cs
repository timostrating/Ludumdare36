using System.ComponentModel;
using UnityEngine;

public class MeshGenerator : MonoBehaviour {

    public static MeshData GenerateTerrainMesh( float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail ) {
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;   // 1, 2, 4, 6, 8, 10, 12,

        int borderdSize = heightMap.GetLength( 0 );
        int meshSize = borderdSize -2 * meshSimplificationIncrement;
        int meshSizeUnSimplefied = borderdSize -2;

        float topLeftX = (meshSizeUnSimplefied-1) / -2F;
        float topLeftZ = (meshSizeUnSimplefied-1) / 2F;

        int verticesPerLine = (meshSize -1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData( verticesPerLine );
        int i = 0;

        int[,] vertexIndicesMap = new int[borderdSize,borderdSize];
        int meshI = 0;
        int borderI = -1;


        for (int y = 0; y < borderdSize; y += meshSimplificationIncrement) {
            for (int x = 0; x < borderdSize; x += meshSimplificationIncrement) {
                bool isBorderVertex = y == 0 || y == borderdSize - 1  ||  x == 0 || x == borderdSize - 1;

                if (isBorderVertex) {
                    vertexIndicesMap[x,y] = borderI;
                    borderI --;
                }
                else {
                    vertexIndicesMap[x, y] = meshI;
                    meshI ++;
                }
            }
        }


        for (int y = 0; y < borderdSize; y += meshSimplificationIncrement) {
            for (int x = 0; x < borderdSize; x += meshSimplificationIncrement) {
                int vertexIndex = vertexIndicesMap[x,y];

                Vector2 percent = new Vector2((x-meshSimplificationIncrement) / (float)meshSize, (y-meshSimplificationIncrement) / (float)meshSize);
                float height = heightCurve.Evaluate(Mathf.Clamp01(heightMap[x, y])) * heightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnSimplefied,   height,   topLeftZ - percent.y * meshSizeUnSimplefied);

                meshData.AddVertex( vertexPosition, percent, vertexIndex );

                if (x < borderdSize - 1 && y < borderdSize - 1) {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement,  y];
                    int c = vertexIndicesMap[x,  y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement,  y + meshSimplificationIncrement];

                    meshData.AddTriangle(a, d, c);      //   a -    a b  
                    meshData.AddTriangle(d, a, b);      //   c d    - d
                }

                i++;
            }
        }

        return meshData;
    }
}


public class MeshData {
    Vector3[] vertices;
    int[] trianglePoints;
    Vector2[] uvs;

    Vector3[] borderVertices;
    int[] borderTrianglePoints;

    int triangleIndex;
    int borderTriangleIndex;


    public MeshData( int size ) {
        vertices = new Vector3[size * size];
        uvs = new Vector2[size * size];
        trianglePoints = new int[(size - 1) * (size - 1) * 6];  // first we calculate the amount of squires   each has 2 trianglePoints   each of those have 3 points  2*3 = 6

        borderVertices = new Vector3[size*4 + 4];  // size * 4 sides  +  4 corners
        borderTrianglePoints = new int[6 * 4 * size];
    }

    public void AddVertex(Vector3 vertexPos, Vector2 uv, int vertexIndex) {
        if(vertexIndex < 0) {
            borderVertices[-vertexIndex-1] = vertexPos;
        } else {
            vertices [ vertexIndex] = vertexPos;
            uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle( int a, int b, int c ) {
        if (a < 0 || b < 0 || c < 0) {
            borderTrianglePoints[borderTriangleIndex] = a;
            borderTrianglePoints[borderTriangleIndex + 1] = b;
            borderTrianglePoints[borderTriangleIndex + 2] = c;

            borderTriangleIndex += 3;
        } else {
            trianglePoints[triangleIndex] = a;
            trianglePoints[triangleIndex + 1] = b;
            trianglePoints[triangleIndex + 2] = c;

            triangleIndex += 3;
        }
    }

    public Mesh CreateMesh() {
        Mesh mesh = new Mesh {
            vertices = vertices,
            triangles = trianglePoints,
            uv = uvs
        };

        mesh.normals = CalculateNormals();
        return mesh;
    }


    private Vector3[] CalculateNormals() {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        int triangleCount = trianglePoints.Length / 3;  // triangle points contains  a,b,c  1,2,3,  / 3 gets us the amount of triangles

        for (int i = 0; i < triangleCount; i++) {
            int normalTriangleIndex = i * 3;  // back to a index in the points

            int vertexIndexA = trianglePoints[normalTriangleIndex];
            int vertexIndexB = trianglePoints[normalTriangleIndex + 1];
            int vertexIndexC = trianglePoints[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromNormals( vertexIndexA, vertexIndexB, vertexIndexC );

            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTrianglePoints.Length / 3;  // triangle points contains  a,b,c  1,2,3,  / 3 gets us the amount of triangles

        for (int i = 0; i < borderTriangleCount; i++) {
            int normalTriangleIndex = i * 3;  // back to a index in the points

            int vertexIndexA = borderTrianglePoints[normalTriangleIndex];
            int vertexIndexB = borderTrianglePoints[normalTriangleIndex + 1];
            int vertexIndexC = borderTrianglePoints[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromNormals( vertexIndexA, vertexIndexB, vertexIndexC );

            if (vertexIndexA >= 0)  // A
                vertexNormals[vertexIndexA] += triangleNormal;
            if (vertexIndexB >= 0)  // B
                vertexNormals[vertexIndexB] += triangleNormal;
            if (vertexIndexC >= 0)  // C
                vertexNormals[vertexIndexC] += triangleNormal;
        }

        for (int i = 0; i < vertexNormals.Length; i++) {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    private Vector3 SurfaceNormalFromNormals(int indexA, int indexB, int indexC) {
        Vector3 pointA = (indexA < 0)? borderVertices[-indexA-1] : vertices[indexA];
        Vector3 pointB = (indexB < 0)? borderVertices[-indexB-1] : vertices[indexB];
        Vector3 pointC = (indexC < 0)? borderVertices[-indexC-1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }
}