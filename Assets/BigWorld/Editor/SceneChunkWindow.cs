using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Authentication.ExtendedProtection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Rendering.LookDev;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public class SceneChunkWindow : EditorWindow
{
    [Serializable]
    public struct SceneChunkData
    {
        public float xStart;
        public float yStart;
        public float xEnd;
        public float yEnd;
        public float step;
        public int enviormentGUID;
    }

    private SceneChunkData _data;
  //  private float xStart, yStart;
 //   float step, xEnd, yEnd;
    /// <summary>
    /// 大世界的根节点
    /// </summary>
    private GameObject enviorment;
    /// <summary>
    /// TODO:不能有路径不能识别的奇怪符号
    /// </summary>
    [SerializeField]
    private string chunkGroupName;
    private bool bolResetChunkFirst = true;
    private Dictionary<string, GameObject> chunkMap = new Dictionary<string, GameObject>();
    [MenuItem("Tools/BigWorld/Scene Chunk",priority = 1)]
    public static  void ShowWindow()
    {
        GetWindow<SceneChunkWindow>().Show();
    
    }

    public SceneChunkWindow()
    {
        _data.xEnd = 3;
        _data.yEnd = 3;
        _data.step = 0.5f;
    }
    SerializedProperty m_IntProp;

    private string PrefsKey
    {
        get => "BigWorldOpenWorld_" + chunkGroupName;
    }
    

    private void Awake()
    {
        chunkGroupName = SceneManager.GetActiveScene().name;
        //string str = JsonUtility.ToJson(_data);
        //EditorPrefs.SetString("BigWorldOpenWorld_" + chunkGroupName, str);
        string str =EditorPrefs.GetString(PrefsKey);
        if (!string.IsNullOrEmpty(str))
        {
            _data = JsonUtility.FromJson<SceneChunkData>(str);
            
        }

        if (_data.enviormentGUID > 0)
        {
            Object[] allObjects = Object.FindObjectsOfType<GameObject>();//一个很龌龊的遍历方法
            foreach (var obj in allObjects)
            {
                if (obj.GetInstanceID() == _data.enviormentGUID)
                {
                    enviorment = obj as GameObject;
                    break;
                }

            }
        }
    }

    private void OnGUI()
    {
        //TODO:根据场景 + enviorment 做key避免多次进入，每次都需要设置。。。。。长宽
        EditorGUILayout.LabelField("Position of the bottom left corner:");
        EditorGUILayout.Space(2);
        _data.xStart = EditorGUILayout.FloatField("xStart:", _data.xStart);
        _data.yStart = EditorGUILayout.FloatField("yStart:", _data.yStart);
 
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Position of the top right corner:");
        EditorGUILayout.Space(2);
        _data.xEnd = EditorGUILayout.FloatField("x end:", _data.xEnd);
        _data.yEnd = EditorGUILayout.FloatField("y end:", _data.yEnd);
        
        EditorGUILayout.Space(10);
        _data.step = EditorGUILayout.FloatField("step:", _data.step);
        EditorGUILayout.Space(10);
        
        //GUILayout.BeginHorizontal();
        GUILayout.Label("根节点");
        GUILayout.Space(2);
        enviorment = (GameObject)EditorGUILayout.ObjectField(enviorment, typeof(GameObject), true);
        //GUILayout.EndHorizontal();
        if (GUI.changed)
        {
            if (enviorment == null)
                _data.enviormentGUID = -1;
            else
            {
                _data.enviormentGUID = enviorment.GetInstanceID();
            }
            
            EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(_data));
        }
        string resetNameText = "先清空【根节点】已有Chunk";
        bolResetChunkFirst = GUILayout.Toggle(bolResetChunkFirst, resetNameText);
        if (enviorment != null)
        {
            var color = GUI.color;
            GUI.color = Color.blue;
            GUILayout.Label("根节点下的子节点会被自动设置，不可还原！！为了避免影响已有场景数据！！注意需要选对节点");
            GUI.color = color;
        }

        //Button 1
        if (GUILayout.Button("Chunk the map"))
        {
            if (enviorment == null)
            {
                EditorUtility.DisplayDialog("", "【根节点】enviorment 不能为空", "Ok");
                return;
            }

            if (bolResetChunkFirst == false)
            {
                if (EditorUtility.DisplayDialog("", "没勾选 - " + resetNameText +" 如果多次导入会有冗余，继续？", "Ok", "Cancel") == false)
                    return;
            }

            Undo.IncrementCurrentGroup();

            if (bolResetChunkFirst)
                ResetChunkAll(null,enviorment);
            
            PlaceObjectsIntoChunk();
            
            Undo.SetCurrentGroupName("Move To Chunk");
        }

        GUILayout.Space(5);
        if (Selection.gameObjects == null)
        {

            GUIRedLabel("[Map Data]select-gameObjects==null");//几乎不会有这个情况
        }
        else if(Selection.gameObjects.Length==0)
        {
            GUIRedLabel("[Map Data]Selection:null(need map Chunk");
        }
        else if (Selection.gameObjects[0].name.StartsWith("Map") == false)
        {
            GUIRedLabel(string.Format("[Map Data]Selection:{0} but it's not Map___ Chunk",Selection.gameObjects.Length));
        }
        else
        {
            GUILayout.Label("[Map Data]Selection:" +Selection.gameObjects.Length);
        }


        chunkGroupName = EditorGUILayout.TextField( "Group Name",chunkGroupName);
        //Button 2
        if (GUILayout.Button("Create Map Data"))
        {
   
            //TODO:若选择不对时，不能点击状态（显示）
            CreateMapData();
        }
        
        //Button - 3-0 定位到已生成的 map Data.asset
        GUILayout.Space(5);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Pin Created"))
        {
            PingToMapDataByGroupName();
        }

        GUILayout.Label("     by " + chunkGroupName == "" ? "-" : chunkGroupName);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        //Button 3
        if (GUILayout.Button("Create Scene"))
        {
            CreateSceneByMapData();
        }

    }
    /// <summary>
    /// 初始化场景 Cam,注意最远距离设定了为，Loader的 loadTolerance 开外的300
    /// </summary>
    /// <returns></returns>
    public GameObject CreateCamera()
    {
        var CameraObj = new GameObject("myCamera");
        Camera cam = CameraObj.AddComponent<Camera>();
        cam.transform.localPosition = new Vector3(0, 1, -10);
        //cam.depth = ;
        //cam.cullingMask = 1 << CAM_LAYER;
        //cam.gameObject.layer = CAM_LAYER;
        cam.clearFlags = CameraClearFlags.Depth;
        
        //cam.orthographic = true;        //投射方式：orthographic正交//
        cam.orthographicSize = 1;       //投射区域大小//
        cam.nearClipPlane = 0.3f;      //前距离//
        cam.farClipPlane = _data.step*5 + 300f;       //后距离//
        cam.rect = new Rect(0, 0, 1f, 1f);
 
 
       // UICamera uiCam = CameraObj.AddComponent<UICamera>();
       // uiCam.eventReceiverMask = 1 << CAM_LAYER;
       return CameraObj;
    }
    
    void CreateSceneByMapData()
    {
        var obj = GetOneMapData();
        if (obj == null)
        {
            if (chunkGroupName.Contains("BigWorld"))
            {
                EditorUtility.DisplayDialog("", "Please Create Map Data First" + "\n if There is BigWorld.unity, need origin scene", "Ok");
            }
            else
            {
                EditorUtility.DisplayDialog("", "Please Create Map Data First", "Ok");    
            }
            
            return;
        }

        var defaultName = string.IsNullOrEmpty(chunkGroupName) ? "_BigWorld" : chunkGroupName + "BigWorld";
        var savePath = EditorUtility.SaveFilePanel("Save Scene", "Assets/", defaultName, "unity");
        if (!string.IsNullOrEmpty(savePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = defaultName;
   

            var cam = CreateCamera();
            var loader = cam.AddComponent<Loader>();
            loader.step = _data.step;
            loader.xStart = _data.xStart;
            loader.xEnd = _data.xEnd;
            loader.yStart = _data.yStart;
            loader.yEnd = _data.yEnd;
            loader.loadTolerance = _data.step * 5;
            var mapDataPath = AssetDatabase.GetAssetPath(obj);//obj==mapData.asset 
            loader.MapDataPrePath = Path.GetDirectoryName(mapDataPath).Replace("\\","/");
            SceneManager.MoveGameObjectToScene(cam,scene);
           // scene.ad
            //GameObject obj = new GameObject("BigWorldLoader");
            //obj.transform.SetParent(scene.s);
            //Scene scene = new Scene {name = defaultName};
            //AssetDatabase.CreateAsset(scene,savePath);
            
            EditorSceneManager.SaveScene(scene, savePath);
            EditorSceneManager.CloseScene(scene, true);
            AssetDatabase.Refresh();
            
            
            int index = savePath.IndexOf("/Assets");
            string relativePath = savePath.Substring(index+1, savePath.Length - index-1);
            Object newScene = AssetDatabase.LoadAssetAtPath<Object>(relativePath);
            EditorGUIUtility.PingObject(newScene);
        }
    }

    Object GetOneMapData()
    {
        string fileName = "MapX0Y0";
        string path;
        if (string.IsNullOrEmpty(chunkGroupName))
        {
            path = "Assets/BigWorld/Map Data/" + fileName + ".asset";
        }
        else
        {
            path = "Assets/BigWorld/Map Data/"+chunkGroupName+  "/"+fileName +".asset";
        }
        
        return AssetDatabase.LoadAssetAtPath<Object>(path);

    }

    void PingToMapDataByGroupName()
    {
        var obj = GetOneMapData();
        if (obj == null)
        {
            EditorUtility.DisplayDialog("", "Please Create Map Data first","Ok");
            return;
        }

        EditorGUIUtility.PingObject(obj);
    }

    void GUIRedLabel(string msg)
    {
        var color = GUI.color;
        GUI.color = Color.red;
            
        GUILayout.Label(msg);
        GUI.color = color;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="root">传入null，则取场景下所有根节点做 Reset</param>
    void ResetChunkAll(GameObject root,GameObject resetRoot)
    {
        List<GameObject> lst = new List<GameObject>();
        if (root == null)
        {
            lst.AddRange(SceneManager.GetActiveScene().GetRootGameObjects());
        }
        else
        {
            for (int i = 0; i < root.transform.childCount; i++)
            {
                lst.Add(root.transform.GetChild(i).gameObject);
            }
        }
    

        for (int i =lst.Count-1; i >= 0; i--)
        {
            var child = lst[i].transform;
            if (!child.name.StartsWith("MapX"))
                continue;
            

            for (int j = child.childCount-1; j >=0; j--)
            {

                Undo.SetTransformParent(child.GetChild(j),resetRoot.transform,true,"Reset Chunk Child");
            }

            if (child.childCount != 0)
            {
                Debug.LogWarning("测试异常："+child.name);//加点容错
            }
            else
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }
    }

    private void PlaceObjectsIntoChunk()
    {
        if (enviorment == null) return;
        //Undo.IncrementCurrentGroup();
        //Undo.RecordObject(enviorment,"Curr Root");//用了Undo.SetTransformParent，不要这代码
        Transform[] childs;
        for(float x =_data.xStart;x<_data.xEnd;x+=_data.step)
        {
            for (float y = _data.yStart; y < _data.yEnd; y += _data.step)
            {
                //parentMap 缓存(chunkMap)，避免点击多次
                string mapName = "MapX" + x + "Y" + y;
                GameObject parentMap;
                if (chunkMap.ContainsKey(mapName))//chunkMap 这个字典，按时不需要
                {
                    parentMap = chunkMap[mapName];
                }
                else
                {
                    parentMap = new GameObject(mapName);
            //        chunkMap.Add(mapName,parentMap);
                }

                Undo.RegisterCreatedObjectUndo(parentMap,string.Format("Create Map {0}-{1}",x,y));
                parentMap.transform.position =enviorment.transform.position + new Vector3(x, 0, y);
                childs = enviorment.GetComponentsInChildren<Transform>();
                for (int j = 0; j < childs.Length; j++)
                {
                    MeshRenderer temp;
                    if (childs[j].gameObject != enviorment && childs[j].TryGetComponent<MeshRenderer>(out temp))
                    {
                        if (IsVector3InArea(childs[j].transform.position,
                            enviorment.transform.position+new Vector3(x - _data.step / 2, 0, y - _data.step / 2),
                            enviorment.transform.position + new Vector3(x + _data.step / 2, 0, y + _data.step / 2)))
                        {
                            Undo.SetTransformParent(childs[j], parentMap.transform, "Update node to Chunk");
                        }
                    }
                }
                //这行代码作用不明确。。。
                //https://docs.unity3d.com/ScriptReference/Undo.RegisterCreatedObjectUndo.html
               // Undo.RegisterFullObjectHierarchyUndo(parentMap, "hierarchy");
            }
        }
        
       // Undo.SetCurrentGroupName("Move To Chunk");
    }

    bool IsVector3InArea(Vector3 p, Vector3 leftBottom,Vector3 rightTop)
    {
        return (p.x >= leftBottom.x && p.x <= rightTop.x && p.z >= leftBottom.z && p.z <= rightTop.z);
    }
    /// <summary>
    /// 分完 chunk 后保存到本地
    /// </summary>
    void CreateMapData()
    {
        List<GameObject> gos = new List<GameObject>();
        //Selection.gameObjects
        //gos.Add(enviorment);
        gos.AddRange(Selection.gameObjects);
        int cursor = 0;
        foreach (var a in gos)
        {
            MapData mapData = ScriptableObject.CreateInstance<MapData>();
            string fileName = a.name;
            MapData.RecordObjects(mapData, a);
            //比较稳定了。。。可以自行生成目录了（注意！！下面路径不要写错，或经常更改）
            string path = "";
            if (string.IsNullOrEmpty(chunkGroupName))
            {
                path = "Assets/BigWorld/Map Data/" + fileName + ".asset";
                
            }
            else
            {
                path = "Assets/BigWorld/Map Data/"+chunkGroupName+  "/"+fileName +".asset";
            }
           
            var dir = Path.GetDirectoryName(path);
            //需减去 dataPath 里的 /Assets (/ 不需要减）
            var fullPathDir = Application.dataPath.Substring(0,Application.dataPath.Length-6) + dir;
            if (!Directory.Exists(fullPathDir))
            {
                Directory.CreateDirectory(fullPathDir);
                EditorUtility.DisplayProgressBar("","Create Group Dir(map data)",1f*cursor/gos.Count);
            }
            
            EditorUtility.DisplayProgressBar("",string.Format("Creating...map data : {0}/{1}",cursor,gos.Count),1f*cursor/gos.Count);
            cursor++;
            AssetDatabase.CreateAsset(mapData,path);
        }
        EditorUtility.ClearProgressBar();
    }
    
    
}
