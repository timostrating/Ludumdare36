using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor {

    public  override void OnInspectorGUI() {
        MapGenerator mapGenerator = (MapGenerator)target;  // target must be type casted

        if (DrawDefaultInspector())
            if(mapGenerator.autoUpdate)
                mapGenerator.DrawMapInEditor();

        if( GUILayout.Button( "Generate" ) ) {
            mapGenerator.DrawMapInEditor();
        }
    }
}
