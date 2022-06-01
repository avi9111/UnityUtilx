using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.AI;
using Object = System.Object;

public class Loader : MonoBehaviour
{
   private List<Vector3> mapDatas;

   public float xStart, yStart;

   public float xEnd = 5;

   public float yEnd = 5;

   public float step = 2;
   //Vector3> loadedMapDatas;
   [Header("View Range")]
   public float loadTolerance = 3f;
   private Dictionary<Vector3, MapDataStreamer> mapDataStreamers = new Dictionary<Vector3, MapDataStreamer>();
   [Header("Map Data Base Set")]
   public string MapDataPrePath = "Assets/BigWorld/Map Data";
   public Vector3 originPoint;
   public bool ShowGizmos;
   private void Awake()
   {
      
   }

   private void Start()
   {
      mapDatas = new List<Vector3>();
      //构建基础格子
      for (float x = xStart; x < xEnd; x+=step)
      {
         for (float y = yStart; y < yEnd; y += step)
         {
            mapDatas.Add(new Vector3(x, 0, y));
         }
      }
   }

   private void Update()
   {
      foreach (var a in mapDatas)
      {
         if (IsVector3InArea(transform.position, a, loadTolerance) && !mapDataStreamers.ContainsKey(a))
         {
            //loadedm
            mapDataStreamers.Add(a,new MapDataStreamer(MapDataPrePath+"/MapX"+a.x+"Y"+a.z+".asset"));
         }
         else if (!IsVector3InArea(transform.position, a, loadTolerance) && mapDataStreamers.ContainsKey(a))
         {
            var stream = mapDataStreamers[a];
            mapDataStreamers.Remove(a);
            stream.Destroy();
         }
      }
   }

   bool IsVector3InArea(Vector3 p, Vector3 mapPosition, float range)
   {
      return Vector3.Distance(p,mapPosition)<range;
   }

   // void OnDrawGizmos()
   // {
   //    Gizmos.color = Color.yellow;
   //    Gizmos.DrawWireSphere(Vector3.zero,2);
   //    Gizmos.DrawWireSphere(transform.position,2);
   // }

   private void OnDrawGizmosSelected()
   {
      if (ShowGizmos == false) return;
      Vector3 center = Vector3.zero;
      //var position = transform.position;
      var position = originPoint;
      center.y = position.y;
      center.x = position.x+ (xEnd - xStart) / 2;
      center.z = position.z + (yEnd - yStart) / 2;
   //   Gizmos.DrawCube(center,new Vector3(xEnd-xStart,1,yEnd-yStart));
      
      //Gizmos.DrawWireCube(center,new Vector3(3,3,3));
      //Gizmos.DrawCube(Vector3.zero,new Vector3(2,2,3));
      
      //Gizmos.DrawWireSphere(Vector3.zero,2f);

      var limitX = 100 * step;
      for (float x = xStart; x < xEnd; x += step)
      {
         for (float y = yStart; y < yEnd; y += step)
         {
            if (x >= limitX || y >= limitX) break;//容错，超过太多则不画线
            //下和右 边线
            var gridCenter = originPoint+new Vector3(x, 0, y - step / 2);
            Gizmos.DrawCube(gridCenter,new Vector3(step,6,2));
            gridCenter =originPoint+ new Vector3(x + step / 2, 0, y);
            Gizmos.DrawCube(gridCenter,new Vector3(2,6, step));
         }
      }
      //Gizmos.draw
   }
}
