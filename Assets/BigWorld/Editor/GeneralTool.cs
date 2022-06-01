using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class GeneralTool
{
   public const string kBigWorld="Tools/BigWorld/";
   //[MenuItem(kBigWorld+"Change Address 2 Asset Name")]
   public static void ChangeAddress2AssetName()
   {
      for (int j = 0; j < Selection.objects.Length; j++)
      {
         var obj = Selection.objects[j];
         float progress = 1f*j / Selection.objects.Length;
         EditorUtility.DisplayProgressBar("Converting asset address to asset name:"+progress,"Converting",progress);
         AddressableAssetSettingsDefaultObject.Settings
               .FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj))).address =
            obj.name;
      }
      
      EditorUtility.ClearProgressBar();
   }

   //[MenuItem(kBigWorld+"Add Lod")]
   public static void AddLod()
   {
   }
   //[MenuItem(kBigWorld+"Vertex Count")]
   public static void PrintVertex()
   {
   }
}
