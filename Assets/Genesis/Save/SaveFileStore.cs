using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Genesis.Save
{
    /// <summary>Atomic single-slot replacement with an explicit backup. Corruption never silently creates a new game.</summary>
    public static class SaveFileStore
    {
        public const int MaximumBytes = 4*1024*1024;
        [Serializable] private sealed class Envelope { public int format; public string payload,sha256; }
        private static string Hash(string text)
        { using var hash=SHA256.Create(); return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(text))); }
        public static void Write(string path,PlayerSaveData data)
        {
            string payload=JsonUtility.ToJson(data);
            byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Envelope{format=1,payload=payload,sha256=Hash(payload)}));
            if(bytes.Length>MaximumBytes)throw new InvalidDataException("Checkpoint exceeds the size limit.");
            path=Path.GetFullPath(path);Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp=path+".tmp";
            try
            {
                using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)) {stream.Write(bytes,0,bytes.Length);stream.Flush(true);}
                if(File.Exists(path))File.Replace(temp,path,path+".bak");else File.Move(temp,path);
            }
            finally { if(File.Exists(temp))File.Delete(temp); }
        }
        public static PlayerSaveData Read(string path)
        {
            var file=new FileInfo(path);
            if(!file.Exists)throw new FileNotFoundException("No checkpoint has been saved.");
            if(file.Length>MaximumBytes)throw new InvalidDataException("Checkpoint exceeds the size limit.");
            var envelope=JsonUtility.FromJson<Envelope>(File.ReadAllText(path,Encoding.UTF8));
            if(envelope==null||envelope.format!=1||string.IsNullOrEmpty(envelope.payload)||envelope.sha256!=Hash(envelope.payload))
                throw new InvalidDataException("Checkpoint is damaged or uses an unsupported format. The backup was not loaded automatically.");
            return JsonUtility.FromJson<PlayerSaveData>(envelope.payload)??throw new InvalidDataException("Checkpoint is empty.");
        }
    }
}
