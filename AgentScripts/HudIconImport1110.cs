using UnityEditor;
using UnityEngine;
public static class HudIconImport1110
{
 public static string Run(){int count=0;foreach(var path in System.IO.Directory.GetFiles("Assets/_Project/Resources/UI/HudIcons","*.png")){var t=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));t.textureCompression=TextureImporterCompression.Uncompressed;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.npotScale=TextureImporterNPOTScale.None;t.filterMode=FilterMode.Bilinear;t.SaveAndReimport();count++;}return count+" uncompressed icons, no mipmaps";}
}
