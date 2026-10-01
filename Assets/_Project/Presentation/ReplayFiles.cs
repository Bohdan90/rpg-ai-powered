using System;
using System.Collections.Generic;
using System.IO;
using RPG.Core;
using UnityEngine;

namespace RPG.Presentation
{
    // Local debug transport only; Core owns snapshots, commands, hashes and verification.
    public static class ReplayFiles
    {
        [Serializable] private sealed class LineType { public string type; }
        public static string Export(BattleJournal journal,string directory)
        {
            Directory.CreateDirectory(directory);
            string stem="gate-c-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N").Substring(0,8);
            string path=Path.Combine(directory,stem+".jsonl");
            using(var writer=new StreamWriter(path))
            {
                writer.WriteLine(JsonUtility.ToJson(journal.Header));
                foreach(var record in journal.Records)writer.WriteLine(JsonUtility.ToJson(record));
                writer.WriteLine(JsonUtility.ToJson(journal.Footer()));
            }
            File.WriteAllText(Path.Combine(directory,stem+".session.json"),JsonUtility.ToJson(journal.Session,true));return path;
        }
        public static ReplayVerification Verify(string path)
        {
            try
            {
                ReplayHeader header=null;ReplayFooter footer=null;var records=new List<ReplayRecord>();
                foreach(string line in File.ReadLines(path))
                {
                    if(footer!=null)throw new InvalidDataException("Data after footer");
                    string type=JsonUtility.FromJson<LineType>(line)?.type;
                    switch(type)
                    {
                        case "header":if(header!=null||records.Count!=0)throw new InvalidDataException("Duplicate/misplaced header");header=JsonUtility.FromJson<ReplayHeader>(line);break;
                        case "command":if(header==null)throw new InvalidDataException("Missing header");records.Add(JsonUtility.FromJson<ReplayRecord>(line));break;
                        case "footer":footer=JsonUtility.FromJson<ReplayFooter>(line);break;
                        default:throw new InvalidDataException("Unknown JSONL record");
                    }
                }
                if(header==null||footer==null)throw new InvalidDataException("Incomplete replay: header/footer required");
                return ReplayVerification.Verify(header,records,footer);
            }
            catch(Exception e){return ReplayVerification.Failure("Cannot load replay: "+e.Message);}
        }
    }
}
