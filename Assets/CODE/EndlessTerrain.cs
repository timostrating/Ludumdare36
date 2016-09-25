using UnityEngine;
using System.Collections.Generic;
using System;  // Action

public class EndlessTerrain : MonoBehaviour {

    public const float scale = 1f;

    const float vieuwerMoveThresholdChunkUpdate = 25f;
    const float sqrtVieuwerMoveThresholdChunkUpdate = vieuwerMoveThresholdChunkUpdate * vieuwerMoveThresholdChunkUpdate;

    public LODInfo[] detailLevels;
    public static float maxVieuwDist;

    public Transform ViewerTransform;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDist;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();


    void Awake() { mapGenerator = FindObjectOfType<MapGenerator>(); }
    void Start() {
        maxVieuwDist = detailLevels[detailLevels.Length-1].visibeDistThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDist = Mathf.RoundToInt(maxVieuwDist / chunkSize);

        UpdateVisibleChuncks();  // start building the world
    }


    void Update() {
        viewerPosition = new Vector2(ViewerTransform.position.x, ViewerTransform.position.z) / scale;  // node: viewer pos is also / scale

        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrtVieuwerMoveThresholdChunkUpdate) {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChuncks();
        }
    }


    void UpdateVisibleChuncks() {

        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++) {
            terrainChunksVisibleLastUpdate [i].SetVisible (false);
        }
        terrainChunksVisibleLastUpdate.Clear ();

        int currentChunkCoordX = Mathf.RoundToInt (viewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt (viewerPosition.y / chunkSize);

        for (int yOffset = -chunksVisibleInViewDist; yOffset <= chunksVisibleInViewDist; yOffset++) {
            for (int xOffset = -chunksVisibleInViewDist; xOffset <= chunksVisibleInViewDist; xOffset++) {
                Vector2 viewedChunkCoord = new Vector2 (currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey (viewedChunkCoord))
                    terrainChunkDictionary [viewedChunkCoord].UpdateTerrainChunk ();
                else 
                    terrainChunkDictionary.Add (viewedChunkCoord, new TerrainChunk (viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
            }
        }
    }



    public class TerrainChunk {

        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;

        MapData mapData;
        bool mapDataReceived;

        int oldLODIndex = -1;


        public TerrainChunk( Vector2 coord, int size, LODInfo[] detailLevels, Transform parent , Material material) {  // Constructor
            this.detailLevels = detailLevels;

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);

            meshObject = new GameObject("Terrain Chunk");
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();

            meshRenderer.material = material;

            meshObject.transform.position = new Vector3(position.x, 0, position.y) * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < lodMeshes.Length; i++) {
                lodMeshes[i] = new LODMesh( detailLevels[i].lod, UpdateTerrainChunk );
            }

            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }


        void OnMapDataReceived( MapData mapData ) {
            this.mapData = mapData;
            mapDataReceived = true;

            Texture2D texture = TextureGenerator.TextureFromColourMap( mapData.colourMap , MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk() {
            if (mapDataReceived) {
                float vieuwerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance( viewerPosition ));
                bool visible = vieuwerDistFromNearestEdge <= maxVieuwDist;

                if(visible) {
                    int lodIndex = 0;

                    for (int i = 0; i < detailLevels.Length-1; i++) {
                        if (vieuwerDistFromNearestEdge > detailLevels[i].visibeDistThreshold) {
                            lodIndex = i + 1;
                        } else 
                            break;
                    }

                    if (lodIndex != oldLODIndex) {
                        LODMesh lodMesh = lodMeshes[lodIndex];

                        if( lodMesh.hasMesh) {
                            oldLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                            meshCollider.sharedMesh = lodMesh.meshLow;
                        }
                        else if (lodMesh.hasReguestedMesh == false){
                            lodMesh.RequestMesh( mapData );
                        }
                    }

                    terrainChunksVisibleLastUpdate.Add (this);
                }

                SetVisible(visible);
            }
        }

        public void SetVisible( bool visible ) {
            meshObject.SetActive(visible);
        }

        public bool IsVisible() {
            return meshObject.activeSelf;
        }
    }



    class LODMesh {
        public Mesh mesh;
        public Mesh meshLow;
        public bool hasReguestedMesh;
        public bool hasMesh;
        int lod;
        Action updateCallback;

        public LODMesh(int lod, Action updateCallback) {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDatareceived(MeshData meshData) {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback ();
        }

        void OnFirstMeshDatareceived(MeshData meshData) {
            meshLow = meshData.CreateMesh();

            OnFirstMeshDatareceived(meshData);
        }

        public void RequestMesh( MapData mapData ) {
            hasReguestedMesh = true;
            mapGenerator.RequestMeshData( mapData, lod, OnMeshDatareceived );
        }
    }


    [System.Serializable]
    public struct LODInfo {  // readonly
        public int lod;
        public float visibeDistThreshold;
    }
}
