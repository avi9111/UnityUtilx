using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
[Serializable]
public struct MapObject
{
    public string name;
    public string objectName;
    public Vector3 pos;
    public Quaternion rot;
    public Vector3 scale;

    public MapObject(Transform obj)
    {
        name = obj.name;
        objectName = obj.name.Split('.')[0];
        var transform = obj.transform;
        pos = transform.position;
        rot = transform.rotation;
        scale = transform.localScale;
        
    }
}

[CreateAssetMenu(fileName = "New Map Data",menuName = "Test/Map Data"),Serializable]
public class MapData : ScriptableObject
{
    public List<MapObject> mapObjects = new List<MapObject>();

    public static void RecordObjects(MapData inst, GameObject g)
    {
        for (int i = 0; i < g.transform.childCount; i++)
        {
            var child = g.transform.GetChild(i);
            MapObject obj = new MapObject(child);
            obj.objectName = GetPrefabAssetPath(child.gameObject);
            inst.AddObject(obj);
        }
    }

    public void AddObject(MapObject obj)
    {
        mapObjects.Add(obj);
    }

    public GameObject InstantiantMapObject(MapObject obj)
    {
        GameObject temp = (GameObject) AssetDatabase.LoadAssetAtPath("Assets/Prefabs/Map/" + obj.objectName + ".prefab",
            typeof(GameObject));
        if (temp == null)
            return null;
        temp = Instantiate(temp);
        temp.transform.position = obj.pos;
        temp.transform.rotation = obj.rot;
        temp.transform.localScale = obj.scale;
        temp.transform.name = obj.name;

        return temp;
    }
    
    /// <summary>
    /// 获取预制体资源路径。
    /// </summary>
    /// <param name="gameObject"></param>
    /// <returns></returns>
    public static string GetPrefabAssetPath(GameObject gameObject)
    {
#if UNITY_EDITOR
        // Project中的Prefab是Asset不是Instance
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject))
        {
            // 预制体资源就是自身
            return UnityEditor.AssetDatabase.GetAssetPath(gameObject);
        }

        // Scene中的Prefab Instance是Instance不是Asset
        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(gameObject))
        {
            // 获取预制体资源
            var prefabAsset = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
            return UnityEditor.AssetDatabase.GetAssetPath(prefabAsset);
        }

        // PrefabMode中的GameObject既不是Instance也不是Asset
        var prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject);
        if (prefabStage != null)
        {
            // 预制体资源：prefabAsset = prefabStage.prefabContentsRoot
            //return prefabStage.prefabAssetPath;
            return prefabStage.assetPath;
        }
#endif

        // 不是预制体
        return null;
    }

}
