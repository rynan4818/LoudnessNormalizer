using LoudnessNormalizer.Interfaces;
using LoudnessNormalizer.Util;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Collections.Concurrent;
using System.IO;
using IPA.Utilities;
using IPA.Utilities.Async;
using System.Text;
using System;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

namespace LoudnessNormalizer.Models
{
    public class LoudnessNormalizerController : IBeatmapInfoUpdater
    {
        private BeatmapLevelsModel _beatmapLevelsModel;
        private SongDatabase _songDatabase;
        public static readonly string VolumedetectOption = "-hide_banner -vn -af volumedetect -f null -";
        public static readonly string LoudnormSurveyOption = "-hide_banner -vn -af \"loudnorm=print_format=json\" -f null -";
        public static readonly string Ebur128Option = "-hide_banner -vn -filter_complex ebur128=framelog=verbose -f null -";
        public static readonly Regex VolumedetectRegex = new Regex(@"\[Parsed_volumedetect[^\]]+\] (\w+): ([-\d\.]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex Ebur128ILRegex = new Regex(@" +Integrated loudness:", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex Ebur128LRRegex = new Regex(@" +Loudness range:", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex Ebur128Regex = new Regex(@" +(\w[\w ]*): +([-\d\.]+) \w+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex LoudnormRegex = new Regex(@"Parsed_loudnorm", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public IDifficultyBeatmap _selectedBeatmap;
        public CancellationTokenSource _cancellationTokenSource;
        public readonly ConcurrentDictionary<string, Process> _ffmpegProcesses = new ConcurrentDictionary<string, Process>();
        public readonly string _ffmpegFilepath = Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe");

        public LoudnessNormalizerController(BeatmapLevelsModel beatmapLevelsModel, SongDatabase songDatabase)
        {
            this._beatmapLevelsModel = beatmapLevelsModel;
            this._songDatabase = songDatabase;
        }
        public void BeatmapInfoUpdated(IDifficultyBeatmap beatmap)
        {
            if (beatmap == null)
                return;
            this._selectedBeatmap = beatmap;
            var levelID = beatmap.level.levelID;
            var songData = this._songDatabase.GetSongData(levelID);
            if (songData.Org == null)
            {
                CoroutineStarter.Instance.StartCoroutine(LoudnessSurvey(levelID, songData));
            }
            else
            {
                if (songData.Now == null)
                {
                }
                else
                {
                }
            }
        }

        public IEnumerator LoudnessChange(string levelID, SongData songData)
        {
            var getSongPath = GetSongAudioClipPathAsync(levelID);
            yield return getSongPath;
            var songAudioClipPath = getSongPath.Result;
            if (songAudioClipPath == null)
                yield break;
            string org_songfile = null;
            var filename = Path.GetFileNameWithoutExtension(songAudioClipPath);
            var ext = Path.GetExtension(songAudioClipPath);
            org_songfile = $"{filename}_org{ext}";
            //arguments += $" {org_songfile}";
            this._beatmapLevelsModel.ClearLoadedBeatmapLevelsCaches();
        }

        public IEnumerator LoudnessSurvey(string levelID, SongData songData, bool original = true)
        {
            var timer = new Stopwatch();
            timer.Start();
            var loudnessData = new LoudnessData();
            var getSongPath = GetSongAudioClipPathAsync(levelID);
            yield return getSongPath;
            var songAudioClipPath = getSongPath.Result;
            if (songAudioClipPath == null)
                yield break;
            yield return FFmpegRun(outputLine => { }, errorLine =>
            {
                float value;
                var match = VolumedetectRegex.Match(errorLine);
                if (match.Success)
                {
                    switch (match.Groups[1].Value)
                    {
                        case "mean_volume":
                            if (float.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out value))
                                loudnessData.MEAN = value;
                            break;
                        case "max_volume":
                            if (float.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out value))
                                loudnessData.MAX = value;
                            break;
                    }
                }
            }, songAudioClipPath, VolumedetectOption);
            yield return new WaitWhile(() => this._ffmpegProcesses.TryGetValue(songAudioClipPath, out _));
            yield return FFmpegRun(outputLine => { }, errorLine =>
            {
                var integratedLoundness = true;
                float value;
                if (Ebur128ILRegex.Match(errorLine).Success)
                    integratedLoundness = true;
                if (Ebur128LRRegex.Match(errorLine).Success)
                    integratedLoundness = false;
                var match = Ebur128Regex.Match(errorLine);
                if (match.Success)
                {
                    switch (match.Groups[1].Value)
                    {
                        case "I":
                            if (float.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out value))
                                loudnessData.I = value;
                            break;
                        case "Threshold":
                            if (integratedLoundness)
                                if (float.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out value))
                                    loudnessData.ILTh = value;
                                else
                                if (float.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out value))
                                    loudnessData.LRTh = value;
                            break;
                        case "LRA":
                            if (float.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out value))
                                loudnessData.LRA = value;
                            break;
                        case "LRA low":
                            if (float.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out value))
                                loudnessData.LRAlow = value;
                            break;
                        case "LRA high":
                            if (float.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out value))
                                loudnessData.LRAhigh = value;
                            break;
                    }
                }
            }, songAudioClipPath, Ebur128Option);
            yield return new WaitWhile(() => this._ffmpegProcesses.TryGetValue(songAudioClipPath, out _));
            if (original)
                songData.Org = loudnessData;
            else
                songData.Now = loudnessData;
            this._songDatabase.SaveSongData(levelID, songData);
            Plugin.Log.Info($"Loudness Survey: {timer.Elapsed.TotalMilliseconds}ms : I:{loudnessData.I}  LRA:{loudnessData.LRA}  MEAN:{loudnessData.MEAN}  MAX:{loudnessData.MAX} :{levelID}");
            timer.Stop();
        }


        public IEnumerator FFmpegRun(Action<string> outputLine, Action<string> errorLine, string songAudioClipPath, string option)
        {
            var arguments = $"-i \"{songAudioClipPath}\" {option}";
            using (var ffmpegProcess = FFmpegProcess(songAudioClipPath, arguments))
            using (var ctoken = new CancellationTokenSource())
            {
                if (ffmpegProcess == null)
                    yield break;
                ffmpegProcess.Exited += (sender, e) => { ctoken.Cancel(); };
                ffmpegProcess.Disposed += (sender, e) => { FFmpegProcessDisposed((Process)sender); };
                if (!ffmpegProcess.Start())
                {
                    Plugin.Log.Info("Faile to start ffmpeg process");
                    yield break;
                }
                var outputRead = Task.Run(() =>
                {
                    while (true)
                    {
                        var l = ffmpegProcess.StandardOutput.ReadLine();
                        if (l == null)
                            break;
                        outputLine?.Invoke(l);
                    }
                });
                var errorRead = Task.Run(() =>
                {
                    while (true)
                    {
                        var l = ffmpegProcess.StandardError.ReadLine();
                        if (l == null)
                            break;
                        errorLine?.Invoke(l);
                    }
                });
                var processWait = Task.Run(() =>
                {
                    ctoken.Token.WaitHandle.WaitOne();
                    ffmpegProcess.WaitForExit();
                });
                var timeout = new TimeoutTimer(60);
                var startProcessTimeout = new TimeoutTimer(10);
                yield return new WaitUntil(() => IsProcessRunning(ffmpegProcess) || startProcessTimeout.HasTimedOut);
                startProcessTimeout.Stop();
                if (timeout.HasTimedOut)
                {
                    yield return new WaitForSeconds(5f);
                    Plugin.Log?.Warn($"[{songAudioClipPath}] Timeout reached, disposing ffmpeg process");
                }
                else
                    Task.WaitAll(outputRead, errorRead, processWait);
                timeout.Stop();
                ffmpegProcess.Close();
                DisposeProcess(ffmpegProcess);
            }
        }

        public async Task<string> GetSongAudioClipPathAsync(string levelID)
        {
            string result = null;
            if (this._cancellationTokenSource != null)
                this._cancellationTokenSource.Cancel();
            using (var cancel = new CancellationTokenSource())
            {
                this._cancellationTokenSource = cancel;
                try
                {
                    var getBeatmapLevelResult = await this._beatmapLevelsModel.GetBeatmapLevelAsync(levelID, this._cancellationTokenSource.Token);
                    if (!getBeatmapLevelResult.isError && getBeatmapLevelResult.beatmapLevel != null && getBeatmapLevelResult.beatmapLevel is CustomBeatmapLevel)
                    {
                        var customBeatmapLevel = (CustomBeatmapLevel)getBeatmapLevelResult.beatmapLevel;
                        result = customBeatmapLevel.songAudioClipPath;
                    }
                }
                finally
                {
                    this._cancellationTokenSource = null;
                }
            }
            return result;
        }

        public Process FFmpegProcess(string songAudioClipPath, string arguments)
        {
            if (this._ffmpegProcesses.TryGetValue(songAudioClipPath, out _))
            {
                Plugin.Log?.Warn("Existing process not cleaned up yet. Cancelling survey attempt.");
                return null;
            }
            if (!File.Exists(songAudioClipPath))
            {
                Plugin.Log?.Warn("No song data");
                return null;
            }
            if (!File.Exists(this._ffmpegFilepath))
            {
                Plugin.Log?.Warn("No ffmpeg.exe");
                return null;
            }
            var process = new Process
            {
                StartInfo =
                {
                    FileName = this._ffmpegFilepath,
                    Arguments = arguments,
                    RedirectStandardError = true,
                    StandardErrorEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true,
                PriorityBoostEnabled = true
            };
            this._ffmpegProcesses.TryAdd(songAudioClipPath, process);
            return process;
        }

        public void FFmpegProcessDisposed(Process sender)
        {
            var disposedProcess = (Process)sender;
            foreach (var dictionaryEntry in this._ffmpegProcesses.Where(keyValuePair => keyValuePair.Value == disposedProcess).ToList())
            {
                var songAudioClipPath = dictionaryEntry.Key;
                var success = this._ffmpegProcesses.TryRemove(dictionaryEntry.Key, out _);
                if (!success)
                    UnityMainThreadTaskScheduler.Factory.StartNew(() => { Plugin.Log?.Error("Failed to remove disposed process from list of processes!"); });
            }
        }

        public static void DisposeProcess(Process process)
        {
            if (process == null)
                return;
            int processId;
            try
            {
                processId = process.Id;
            }
            catch (Exception)
            {
                return;
            }
            Plugin.Log?.Warn($"[{processId}] Cleaning up process");
            Task.Run(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill();
                }
                catch (Exception exception)
                {
                    UnityMainThreadTaskScheduler.Factory.StartNew(() => Plugin.Log.Warn(exception));
                }
                try
                {
                    process.Dispose();
                }
                catch (Exception exception)
                {
                    UnityMainThreadTaskScheduler.Factory.StartNew(() => Plugin.Log.Warn(exception));
                }
            });
        }

        public static bool IsProcessRunning(Process process)
        {
            try
            {
                if (!process.HasExited)
                    return true;
            }
            catch (Exception e)
            {
                if (!(e is InvalidOperationException))
                {
                    UnityEngine.Debug.LogWarning(e);
                }
            }
            return false;
        }
    }
}
