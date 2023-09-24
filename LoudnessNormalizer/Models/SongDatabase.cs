using LoudnessNormalizer.Configuration;
using System;
using System.IO;
using Zenject;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
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
        public static SemaphoreSlim SongDatabaseSemaphore = new SemaphoreSlim(1, 1);
        public ConcurrentDictionary<string, SongData> _songDatabase { get; set; } = new ConcurrentDictionary<string, SongData>();
        public bool _init;
        public bool _songDatabaseChange = false;
        public void Initialize()
        {
            _= this.InitSongDatabaseAsync();
            Plugin.OnPluginExit += BackupSongDatabase; //ファイルの書き込み処理はDisposeのときでは間に合わない
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                    Plugin.OnPluginExit -= BackupSongDatabase;
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
            this._songDatabaseChange = true;
        }

        public async Task InitSongDatabaseAsync()
        {
            this._init = false;
            Plugin.Log?.Info("Init Start");
            this._songDatabase = await this.ReadSongDatabaseJsonAsync(PluginConfig.Instance.SongDatabaseFile);
            if (this._songDatabase == null)
            {
                Plugin.Log?.Info("Restoring songdatabase backup");
                this._songDatabase = await this.ReadSongDatabaseJsonAsync(Path.ChangeExtension(PluginConfig.Instance.SongDatabaseFile, ".bak"));
                if (this._songDatabase == null)
                    this._songDatabase = new ConcurrentDictionary<string, SongData>();
                this._songDatabaseChange = true;
                await this.SaveSongDatabaseAsync();
            }
            this._init = true;
        }
        public async Task SaveSongDatabaseAsync()
        {
            if (!this._songDatabaseChange)
                return;
            this._songDatabaseChange = false;
            if (this._songDatabase.Count == 0)
                return;
            try
            {
                var serialized = JsonConvert.SerializeObject(this._songDatabase, Formatting.None);
                if (!await this.WriteAllTextAsync(PluginConfig.Instance.SongDatabaseFile, serialized))
                    throw new Exception("Failed save songdatabase");
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex.ToString());
                this._songDatabaseChange = true;
            }
        }
        public void SaveSongDatabase()
        {
            if (!this._songDatabaseChange)
                return;
            this._songDatabaseChange = false;
            if (this._songDatabase.Count == 0)
                return;
            try
            {
                var serialized = JsonConvert.SerializeObject(this._songDatabase, Formatting.None);
                File.WriteAllText(PluginConfig.Instance.SongDatabaseFile, serialized);
            }
            catch (Exception ex)
            {
                Plugin.Log?.Error(ex.ToString());
                this._songDatabaseChange = true;
            }
        }
        public bool CheckSongDatabaseFile()
        {
            try
            {
                var text = File.ReadAllText(PluginConfig.Instance.SongDatabaseFile);
                var result = JsonConvert.DeserializeObject<ConcurrentDictionary<string, SongData>>(text);
                if (result == null)
                    return false;
                else
                    return true;
            }
            catch (Exception e)
            {
                Plugin.Log?.Error(e.ToString());
                return false;
            }
        }
        public void BackupSongDatabase()
        {
            if (!this._init)
                return;
            this.SaveSongDatabase();
            if (!File.Exists(PluginConfig.Instance.SongDatabaseFile))
                return;
            Plugin.Log?.Info("Song database backup");
            if (!this.CheckSongDatabaseFile())
            {
                this._songDatabaseChange = true;
                this.SaveSongDatabase();
                if (!this.CheckSongDatabaseFile())
                    return;
            }
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
        public async Task<ConcurrentDictionary<string, SongData>> ReadSongDatabaseJsonAsync(string path)
        {
            ConcurrentDictionary<string, SongData> result;
            var json = await this.ReadAllTextAsync(path);
            try
            {
                if (json == null)
                    throw new JsonReaderException($"Json file error {path}");
                result = JsonConvert.DeserializeObject<ConcurrentDictionary<string, SongData>>(json);
                if (result == null)
                    throw new JsonReaderException($"Empty json {path}");
            }
            catch (JsonException ex)
            {
                Plugin.Log?.Error(ex.ToString());
                result = null;
            }
            return result;
        }
        public async Task<string> ReadAllTextAsync(string path)
        {
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists || fileInfo.Length == 0)
            {
                Plugin.Log?.Info($"File not found : {path}");
                return null;
            }
            string result;
            await SongDatabaseSemaphore.WaitAsync();
            try
            {
                using(var sr = new StreamReader(path))
                {
                    result = await sr.ReadToEndAsync();
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.Error(e.ToString());
                result = null;
            }
            finally
            {
                SongDatabaseSemaphore.Release();
            }
            return result;
        }
        public async Task<bool> WriteAllTextAsync(string path, string contents)
        {
            bool result;
            await SongDatabaseSemaphore.WaitAsync();
            try
            {
                using(var sw = new StreamWriter(path))
                {
                    await sw.WriteAsync(contents);
                }
                result = true;
            }
            catch (Exception e)
            {
                Plugin.Log?.Error(e.ToString());
                result = false;
            }
            finally
            {
                SongDatabaseSemaphore.Release();
            }
            return result;
        }
    }
}
