using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SocialPlatforms;
using Object = UnityEngine.Object;

public class MapDataStreamer
{
    public string mapDataName;
    public static GameObject enviorment;
    private List<GameObject> loadedGameObjects = new List<GameObject>();

    public MapDataStreamer(string mapDataAsset)
    {
        mapDataName = mapDataAsset;
        if(enviorment==null)
            enviorment = new GameObject("enviorment");
        InstantiateAsync();
    }

    void InstantiateAsync()
    {
        //AsyncOperationHandle<GameObject> goHandle = Addressables.LoadAssetAsync<GameObject>("gameObjectKey");

        Addressables.LoadAssetsAsync<MapData>(mapDataName, (asset) =>
        {
            foreach (MapObject a in asset.mapObjects)
            {
                var map = new GameObject(asset.name);
                map.transform.parent = enviorment.transform;
      
                if (AddressResourceExist(a.objectName))
                {
                    //Addressables.InstantiateAsync(a.objectName,map.transform, (prefab) =>
                    Addressables.LoadAssetsAsync<GameObject>(a.objectName, (prefab) =>
                    {
                        var o = GameObject.Instantiate(prefab, map.transform, false);
                        o.name = a.name;
                        o.transform.position = a.pos;
                        o.transform.rotation = a.rot;
                        o.transform.localScale = a.scale;
                        loadedGameObjects.Add(o);
                    });
                }else{
                    Debug.LogWarning("Address 不存在：" +a.objectName);
                }
            }
        });
    }

    public static bool AddressResourceExist(string key)
    {
        foreach (var l in Addressables.ResourceLocators)
        {
           // IList<IResourceLocator> locs;
            if (l.Locate(key, typeof(GameObject), out var locs))
            {
                return true;
            }
        }

        return false;
    }

    public void Destroy()
    {
        foreach (var a in loadedGameObjects)
        {
            //Debug.LogError("remove ==" + a.name);
            //Debug.LogError("remove ??" + loadedGameObjects.);
            if (a != enviorment)
                Addressables.ReleaseInstance(a);
            var mapNode = a.transform.parent;
            int before = mapNode.childCount;
            Object.DestroyImmediate(a);
            
            Debug.LogError(mapNode.name+ " remove b=" +before+" nodeCount=" + mapNode.childCount);
            if(mapNode.childCount==0)
                Object.Destroy(mapNode.gameObject);
            
        }
        //Object.Destroy(enviorment);
    }
}
