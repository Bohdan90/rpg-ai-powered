using System;
using System.IO;
using System.Text;
using RPG.Core;
using UnityEngine;

namespace RPG.Presentation
{
    public static class StrategicSaveFiles
    {
        public static string ManualSlot => Path.Combine(Application.persistentDataPath,"Mission01","manual.json");
        private const long MaximumBytes=1024*1024;
        public static bool TrySave(StrategicScenario scenario,string path,out string message)
        {
            string temporary=null;
            try
            {
                if(scenario==null)throw new InvalidOperationException("No strategic session.");
                string json=JsonUtility.ToJson(scenario.CaptureSave(),true);
                if(Encoding.UTF8.GetByteCount(json)>MaximumBytes)throw new InvalidDataException("Prototype save exceeds 1 MB.");
                string full=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(full));
                temporary=full+"."+Guid.NewGuid().ToString("N")+".tmp";
                using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write))
                {var bytes=Encoding.UTF8.GetBytes(json);stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(full))File.Replace(temporary,full,null);else File.Move(temporary,full);
                message="Saved Mission 01 · Refresh "+scenario.Refresh+" · "+full;return true;
            }
            catch(Exception e){message="Save failed: "+e.Message;return false;}
            finally {if(temporary!=null&&File.Exists(temporary)){try{File.Delete(temporary);}catch(IOException){ }catch(UnauthorizedAccessException){ }}}
        }
        public static bool TryLoad(string path,out StrategicScenario scenario,out string message)
        {
            scenario=null;
            try
            {
                var file=new FileInfo(path);if(!file.Exists)throw new FileNotFoundException("No manual Mission 01 save yet.");
                if(file.Length==0||file.Length>MaximumBytes)throw new InvalidDataException("Invalid save size.");
                var data=JsonUtility.FromJson<StrategicSaveData>(File.ReadAllText(path));
                if(data==null)throw new InvalidDataException("Missing save data.");
                scenario=data.Restore();message="Loaded Mission 01 · Refresh "+scenario.Refresh+" · "+Path.GetFullPath(path);return true;
            }
            catch(Exception e){scenario=null;message="Load failed; current session unchanged: "+e.Message;return false;}
        }
    }
}
