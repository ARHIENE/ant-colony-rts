using System;using System.IO;using AntColony.Save;
public static class CameraSave1115 {
 public static string Main(){var text=File.ReadAllText("work/natural-campaign-1115/checkpoint-save.json");
 if(!SaveValidator.TryParse(text,out var file,out var error))throw new Exception(error);
 var log="";foreach(var value in new[]{5f,8f,30f,35f,4.99f,35.01f,float.NaN,float.PositiveInfinity}){
 file.camera.orthoSize=value;bool valid=SaveValidator.Validate(file,out error);bool expected=value>=5&&value<=35;
 if(valid!=expected)throw new Exception("Camera "+value+" valid="+valid+" "+error);log+="PASS camera "+value+" valid="+valid+"\n";}
 File.WriteAllText("work/colony-polish-1115/camera-save.txt",log);return log;}
}
