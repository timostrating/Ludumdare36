﻿using UnityEngine;
using System;   // Actions
using System.Threading;
using System.Collections.Generic; // Queue

public class MapGenerator : MonoBehaviour {

    public enum DrawMode {NoiseMap, ColourMap, Mesh, FalloffMap}
    public DrawMode drawMode = DrawMode.Mesh;

    public Noise.NormalizeMode normalizeMode = Noise.NormalizeMode.Local;

    public const int mapChunkSize = 239;  // (240 + 1) to compensate for starting at 0   - 2 borders to compensate for the borders  = 239

    [Range(0,6)] public int EditorlevelOfDetail; // 1, 2, 4, 6, 12, 24
    [Range(2,99)] public int scale;
    [Range(1,9)] public int octaves;

    [Range(0,5)] public float persistenceMultiplier;
    [Range(0,5)] public float lacunarityMultiplier;

    public int seed;
    public Vector2 offset;

    public bool useFalloffMap;

    [Range(1,99)] public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;

    public TerrainType[] regions;

    float[,] falloffMap;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    

    void Awake() {
        falloffMap = FalloffGenerator.GenerateFalloffMap( mapChunkSize );
    }

//    public void Start() { DrawMapInEditor(); }
    public void DrawMapInEditor() {
        MapData mapData = GenerateMapData(Vector2.zero);

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
        else if (drawMode == DrawMode.ColourMap)
            display.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        else if (drawMode == DrawMode.Mesh)
            display.DrawMesh(MeshGenerator.GenerateTerrainMesh( mapData.heightMap, meshHeightMultiplier, meshHeightCurve, EditorlevelOfDetail ),  TextureGenerator.TextureFromColourMap( mapData.colourMap, mapChunkSize, mapChunkSize ) );
        else if (drawMode == DrawMode.FalloffMap)
            display.DrawTexture( TextureGenerator.TextureFromHeightMap( falloffMap ));

    }


    public void RequestMapData(Vector2 center, Action<MapData> callback) {
        ThreadStart threadstart = delegate {
            MapDataThread( center, callback );
        };

        new Thread( threadstart ).Start();
    }

    private void MapDataThread(Vector2 center, Action<MapData> callback) {
        MapData mapData = GenerateMapData(center);
        lock (mapDataThreadInfoQueue) {
            mapDataThreadInfoQueue.Enqueue( new MapThreadInfo<MapData>(callback, mapData) );
        }
    }


    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback) {
        ThreadStart threadstart = delegate {
            MeshDataThread( mapData, lod, callback );
        };

        new Thread( threadstart ).Start();
    }

    private void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback) {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock (meshDataThreadInfoQueue) {
            meshDataThreadInfoQueue.Enqueue( new MapThreadInfo<MeshData>(callback, meshData) );
        }
    }


    private void Update() {
        if(mapDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback( threadInfo.paramameter );
            }
        }

        if(meshDataThreadInfoQueue.Count > 0) {
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++) {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback( threadInfo.paramameter );
            }
        }
    }


    private MapData GenerateMapData(Vector2 center) {
        float[,] noiseMap = Noise.GenerateNoiseMap( mapChunkSize + 2, mapChunkSize + 2, seed, scale, octaves, persistenceMultiplier, lacunarityMultiplier, center + offset, normalizeMode );

        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++) {
            for (int x = 0; x < mapChunkSize; x++) {
                if(useFalloffMap)
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - falloffMap[x, y]);

                float curHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++) {
                    if (curHeight >= regions[i].height)
                        colourMap[y * mapChunkSize + x] = regions[i].color;
                    else
                        break;
                }
            }
        }

        return new MapData(noiseMap, colourMap);
    }


    void OnValidate() {
        if (lacunarityMultiplier < 0.1f)
            lacunarityMultiplier = 0.1f;

        if (octaves < 0)
            octaves = 0;

        if (meshHeightMultiplier < 1)
            meshHeightMultiplier = 1;

        if (meshHeightCurve == null || meshHeightCurve.length < 2)
            meshHeightCurve = AnimationCurve.Linear( 0.1f, 0.1f, 0.9f, 0.9f );

        if (falloffMap == null)
            falloffMap = FalloffGenerator.GenerateFalloffMap( mapChunkSize );  // falloffMap should be run on Awake but if we run it in the inspector we run early
    }


    struct MapThreadInfo<T> {
        public readonly Action<T> callback;
        public readonly T paramameter;

        public MapThreadInfo( Action<T> callback, T paramameter ) {
            this.callback = callback;
            this.paramameter = paramameter;
        }
    }

}


[System.Serializable]
public struct TerrainType {  // readonly
    public string name;
    public float height;
    public Color color;
}


[System.Serializable]
public struct MapData {
    public readonly float[,] heightMap;
    public readonly Color[] colourMap;

    public MapData( float[,] heightMap, Color[] colourMap ) {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
    }
}