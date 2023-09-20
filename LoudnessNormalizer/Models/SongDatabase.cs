using LoudnessNormalizer.Configuration;
using LoudnessNormalizer.Util;
using System;
using System.IO;
using Zenject;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using System.Collections.Concurrent;

namespace LoudnessNormalizer.Models
{
    public class SongData
    {
        public LoudnessData Now = null;
        public LoudnessData Org = null;
        public string OrgFile = "";
    }

    public class LoudnessData
    {
        public float I = 0L;
        public float ILTh = 0;
        public float LRA = 0;
        public float LRTh = 0;
        public float LRAlow = 0;
        public float LRAhigh = 0;
        public float MEAN = 0;
        public float MAX = 0;
    }

    public class SongDatabase : IInitializable, IDisposable
    {
        private bool _disposedValue;
        public ConcurrentDictionary<string, SongData> _songDatabase { get; set; } = new ConcurrentDictionary<string, SongData>();
        public bool _init;
        public bool _writeDatabase = false;
        public void Initialize()
        {
            _= this.InitSongDatabaseAsync();
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                    this.BackupSongDatabase();
                this._disposedValue = true;
            }
        }
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public int DatabaseCount()
        {
            return this._songDatabase.Count;
        }

        public SongData GetSongData(string levelID)
        {
            if (this._songDatabase.TryGetValue(levelID, out SongData songData))
                return songData;
            return new SongData();
        }

        public void SaveSongData(string levelID, SongData songData)
        {
            if (!this._init)
                return;
            if (levelID == null || songData == null)
                return;
            if (this._songDatabase.ContainsKey(levelID))
                this._songDatabase[levelID] = songData;
            else
                this._songDatabase.TryAdd(levelID, songData);
            CoroutineStarter.Instance.StartCoroutine(SaveSongDatabaseCoroutine(levelID));
        }
        public IEnumerator SaveSongDatabaseCoroutine(string levelID)
        {
            yield return new WaitWhile(() => this._writeDatabase == true);
            yield return this.SaveSongDatabaseAsync();
        }

        public async Task InitSongDatabaseAsync()
        {
            Plugin.Log?.Info("Init Start");
            this._init = false;
            if (!File.Exists(PluginConfig.Instance.SongDatabaseFile)) {
                this._songDatabase = new ConcurrentDictionary<string, SongData>();
                await this.SaveSongDatabaseAsync();
                this._init = true;
                return;
            }
            var json = await this.ReadAllTextAsync(PluginConfig.Instance.SongDatabaseFile);
            try
            {
                if (json == null)
                    throw new JsonReaderException("Json file error songdatabase");
                this._songDatabase = JsonConvert.DeserializeObject<ConcurrentDictionary<string, SongData>>(json);
                if (this._songDatabase == null)
                    throw new JsonReaderException("Empty json songdatabase");
            }
            catch (JsonException ex)
            {
                Plugin.Log?.Error(ex.ToString());
                var backup = new FileInfo(Path.ChangeExtension(PluginConfig.Instance.SongDatabaseFile, ".bak"));
                if (backup.Exists && backup.Length > 0)
                {
                    Plugin.Log?.Info("Restoring songdatabase backup");
                    try
                    {
                        json = await this.ReadAllTextAsync(backup.FullName);
                        if (json == null)
                            throw new JsonReaderException("Backup json file error songdatabase");
                        this._songDatabase = JsonConvert.DeserializeObject<ConcurrentDictionary<string, SongData>>(json);
                        if (this._songDatabase == null)
                            throw new JsonReaderException("Failed restore songdatabase");
                    }
                    catch (JsonException ex2)
                    {
                        Plugin.Log?.Error(ex2.ToString());
                        this._songDatabase = new ConcurrentDictionary<string, SongData>();
                    }
                }
                else
                    this._songDatabase = new ConcurrentDictionary<string, SongData>();
                await this.SaveSongDatabaseAsync();
            }
            this._init = true;
        }
        public async Task SaveSongDatabaseAsync()
        {
            if (this._writeDatabase)
                return;
            this._writeDatabase = true;
            try
            {
                if (this._songDatabase.Count > 0)
                {
                    var serialized = JsonConvert.SerializeObject(this._songDatabase, Formatting.None);
                    if (!await WriteAllTextAsync(PluginConfig.Instance.SongDatabaseFile, serialized))
                        throw new Exception("Failed save songdatabase");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex.ToString());
            }
            this._writeDatabase = false;
        }
        public void BackupSongDatabase()
        {
            if (!File.Exists(PluginConfig.Instance.SongDatabaseFile))
                return;
            var backupFile = Path.ChangeExtension(PluginConfig.Instance.SongDatabaseFile, ".bak");
            try
            {
                if (File.Exists(backupFile))
                {
                    if (new FileInfo(PluginConfig.Instance.SongDatabaseFile).Length > new FileInfo(backupFile).Length)
                        File.Copy(PluginConfig.Instance.SongDatabaseFile, backupFile, true);
                    else
                        Plugin.Log?.Info("Nothing backup");
                }
                else
                {
                    File.Copy(PluginConfig.Instance.SongDatabaseFile, backupFile);
                }
            }
            catch (IOException ex)
            {
                Plugin.Log?.Error(ex.ToString());
            }
        }
        public async Task<string> ReadAllTextAsync(string path)
        {
            try
            {
                using(var sr = new StreamReader(path))
                    return await sr.ReadToEndAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }
        public async Task<bool> WriteAllTextAsync(string path, string contents)
        {
            try
            {
                using(var sw = new StreamWriter(path))
                    await sw.WriteAsync(contents);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
