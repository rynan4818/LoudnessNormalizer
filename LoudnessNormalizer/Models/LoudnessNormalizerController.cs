using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System;
using UnityEngine;
using System.Text.RegularExpressions;
using LoudnessNormalizer.Configuration;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace LoudnessNormalizer.Models
{
    public class LoudnessNormalizerController
    {
        private BeatmapLevelsModel _beatmapLevelsModel;
        private SongDatabase _songDatabase;
        private FFmpegController _ffmpegController;
        public static readonly string VolumedetectOption = "-hide_banner -y -vn -af volumedetect -f null -";
        public static readonly string LoudnormSurveyOption1 = "-hide_banner -y -vn -af \"loudnorm=";
        public static readonly string LoudnormSurveyOption2 = "print_format=json\" -f null -";
        public static readonly string Ebur128Option = "-hide_banner -y -vn -filter_complex ebur128=framelog=verbose -f null -";
        public static readonly string LoudnessNormalizeOption = "";
        public static readonly Regex VolumedetectRegex = new Regex(@"\[Parsed_volumedetect[^\]]+\] (\w+): ([-\d\.]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex Ebur128ILRegex = new Regex(@" +Integrated loudness:", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex Ebur128LRRegex = new Regex(@" +Loudness range:", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex Ebur128Regex = new Regex(@" +(\w[\w ]*): +([-\d\.]+) \w+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex LoudnormRegex = new Regex(@"\[Parsed_loudnorm[^\]]+\]", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public CancellationTokenSource _cancellationTokenSource;
        public event Action<bool, string, LoudnessData> OnLoudnessSurveyUpdate;
        public event Action<string> OnLoudnormProgress;
        public event Action<float, float, float> OnLoudnessCheckResult;
        public string _selectCheckLevelID;
        public string _allCheckLevelID;
        public bool _allSongCheckerActive { get; set; } = false;
        public bool _allSongCheckerBreak { get; set; } = false;
        public bool _allSongCheckDone { get; set; } = false;
        public bool _gameSceneActive { get; set; } = false;
        public int _allSongCheckCount { get; set; } = 0;

        public LoudnessNormalizerController(BeatmapLevelsModel beatmapLevelsModel, SongDatabase songDatabase, FFmpegController ffmpegController)
        {
            this._beatmapLevelsModel = beatmapLevelsModel;
            this._songDatabase = songDatabase;
            this._ffmpegController = ffmpegController;
        }

        public IEnumerator SlectSongCheckerCoroutine(string levelID, SongData songData)
        {
            this._selectCheckLevelID = levelID;
            yield return new WaitWhile(() => this._allCheckLevelID == levelID);
            yield return this.LoudnessSurveyCoroutine(levelID, songData, true);
            this._selectCheckLevelID = null;
        }

        public IEnumerator AllSongCheckerCoroutine()
        {
            if (!this._songDatabase._init)
                yield break;
            if (this._allSongCheckerActive)
                yield break;
            var allSong = SongCore.Loader.CustomLevels;
            var max = allSong.Count;
            if (this._allSongCheckDone && this._allSongCheckCount == max)
                yield break;
            this._allSongCheckerActive = true;
            var count = 0;
            foreach (string key in allSong.Keys)
            {
                if (this._allSongCheckerBreak || this._gameSceneActive)
                {
                    this._allSongCheckerActive = false;
                    this._allSongCheckerBreak = false;
                    yield break;
                }
                count++;
                var levelID = allSong[key].levelID;
                var songPreviewAudioClipPath = allSong[key].songPreviewAudioClipPath;
                var songData = this._songDatabase.GetSongData(levelID);
                if (songData.Org == null)
                {
                    Plugin.Log.Info($"{count}/{max}:{levelID}");
                    this._allCheckLevelID = levelID;
                    if (this._selectCheckLevelID == levelID)
                        continue;
                    yield return this.LoudnessSurveyCoroutine(levelID, songData, false, true, songPreviewAudioClipPath);
                    this._allCheckLevelID = null;
                }
            }
            this._allSongCheckCount = max;
            this._allSongCheckDone = true;
            this._allSongCheckerActive = false;
        }

        public IEnumerator CheckLoudness(string levelID)
        {
            var songAudioClipPath = GetSongAudioClipPath(levelID);
            if (songAudioClipPath == null)
                yield break;
            var error = true;
            this.OnLoudnormProgress?.Invoke($"Analyzing...");
            yield return this.LoudnormCoroutine(songAudioClipPath, LoudnormSurveyOption1 + LoudnormSurveyOption2, (input_i, input_tp, input_lra, input_thresh, target_offset, dynamic) => {
                Plugin.Log.Info($"{levelID}:1:{input_i}:{input_tp}:{input_lra}:{input_thresh}:{target_offset}");
                this.OnLoudnessCheckResult?.Invoke(input_i, input_tp, input_lra);
                error = false;
            });
            if (error)
                yield break;
        }

        public IEnumerator LoudnessChangeCoroutine(string levelID)
        {
            Plugin.Log.Info($"{levelID}:0");
            var songAudioClipPath = GetSongAudioClipPath(levelID);
            if (songAudioClipPath == null)
                yield break;
            var dir = Path.GetDirectoryName(songAudioClipPath);
            var filename = Path.GetFileNameWithoutExtension(songAudioClipPath);
            var ext = Path.GetExtension(songAudioClipPath);
            var org_songfile = $"{filename}_org.ogg";
            var cng_songfile = $"{filename}_cng.ogg";
            //arguments += $" {org_songfile}";

            var option = $"{LoudnormSurveyOption1}I={PluginConfig.Instance.Itarget}:TP={PluginConfig.Instance.TPtarget}:LRA={PluginConfig.Instance.LRAtarget}:{LoudnormSurveyOption2}";
            var error = true;
            this.OnLoudnormProgress?.Invoke($"Analyzing...");
            yield return this.LoudnormCoroutine(songAudioClipPath, option, (input_i, input_tp, input_lra, input_thresh, target_offset, dynamic) => {
                Plugin.Log.Info($"{levelID}:1:{input_i}:{input_tp}:{input_lra}:{input_thresh}:{target_offset}");
                var lra = PluginConfig.Instance.LRAtarget;
                if (PluginConfig.Instance.LRAunchanged)
                    lra = input_lra;
                option = $"{LoudnormSurveyOption1}I={PluginConfig.Instance.Itarget}:TP={PluginConfig.Instance.TPtarget}:LRA={lra}:" +
                    $"measured_I={input_i}:measured_TP={input_tp}:measured_LRA={input_lra}:measured_thresh={input_thresh}:offset={target_offset}:" +
                    $"print_format=json,channelmap=channel_layout=stereo,aresample=48000\" -aq 9 -acodec libvorbis \"{dir}\\{cng_songfile}\"";
                error = false;
            });
            if (error)
                yield break;
            error = true;
            this.OnLoudnormProgress?.Invoke($"Analyzing...Normalizing...");
            yield return this.LoudnormCoroutine(songAudioClipPath, option, (input_i, input_tp, input_lra, input_thresh, target_offset, dynamic) => {
                Plugin.Log.Info($"{levelID}:2:{input_i}:{input_tp}:{input_lra}:{input_thresh}:{target_offset}:{dynamic}");
                error = false;
                if (dynamic)
                    this.OnLoudnormProgress?.Invoke($"Analyzing...Normalizing...Dynamic...Complete!");
                else
                    this.OnLoudnormProgress?.Invoke($"Analyzing...Normalizing...Linear...Complete!");
            });
            if (error)
                yield break;
            this._beatmapLevelsModel.ClearLoadedBeatmapLevelsCaches(); //終わったら曲データのClipのキャッシュをクリアする
        }

        public IEnumerator LoudnormCoroutine(string songAudioClipPath, string option, Action <float, float, float, float, float, bool> callback)
        {
            var ffmpeg_error = true;
            var builder = new StringBuilder(200);
            yield return this._ffmpegController.FFmpegRunCoroutine(songAudioClipPath, option, null, errorLine =>
            {
                var match = LoudnormRegex.Match(errorLine);
                if (match.Success)
                {
                    ffmpeg_error = false;
                    return;
                }
                if (!ffmpeg_error)
                {
                    builder.Append(errorLine);
                }
            });
            yield return new WaitWhile(() => this._ffmpegController._ffmpegProcesses.TryGetValue(songAudioClipPath, out _));
            if (ffmpeg_error)
                yield break;
            Dictionary<string, string> result;
            try
            {
                result = JsonConvert.DeserializeObject<Dictionary<string, string>>(builder.ToString());
                if (result == null)
                    throw new JsonReaderException($"Loudnorm result empty json");
            }
            catch (JsonException ex)
            {
                Plugin.Log?.Error(ex.ToString());
                yield break;
            }
            float input_i = 0;
            float input_tp = 0;
            float input_lra = 0;
            float input_thresh = 0;
            float target_offset = 0;
            if (!(result.TryGetValue("input_i", out string value) && FloatTryParce(value, ref input_i) &&
                result.TryGetValue("input_tp", out value) && FloatTryParce(value, ref input_tp) &&
                result.TryGetValue("input_lra", out value) && FloatTryParce(value, ref input_lra) &&
                result.TryGetValue("input_thresh", out value) && FloatTryParce(value, ref input_thresh) &&
                result.TryGetValue("target_offset", out value) && FloatTryParce(value, ref target_offset) &&
                result.TryGetValue("normalization_type", out value)))
                yield break;
            var dynamic = value == "dynamic";
            callback?.Invoke(input_i, input_tp, input_lra, input_thresh, target_offset, dynamic);
        }

        public IEnumerator LoudnessSurveyCoroutine(string levelID, SongData songData, bool loudnessUpdate, bool original = true, string songAudioClipPath = null)
        {
            var timer = new Stopwatch();
            timer.Start();
            var loudnessData = new LoudnessData();
            if (songAudioClipPath == null)
                songAudioClipPath = GetSongAudioClipPath(levelID);
            if (songAudioClipPath == null)
                yield break;
            var ffmpeg_error = true;
            yield return this._ffmpegController.FFmpegRunCoroutine(songAudioClipPath, VolumedetectOption, null, errorLine =>
            {
                var match = VolumedetectRegex.Match(errorLine);
                if (match.Success)
                {
                    ffmpeg_error = false;
                    switch (match.Groups[1].Value)
                    {
                        case "mean_volume":
                            FloatTryParce(match.Groups[2].Value, ref loudnessData.MEAN);
                            break;
                        case "max_volume":
                            FloatTryParce(match.Groups[2].Value, ref loudnessData.MAX);
                            break;
                    }
                }
            });
            yield return new WaitWhile(() => this._ffmpegController._ffmpegProcesses.TryGetValue(songAudioClipPath, out _));
            if (ffmpeg_error)
                yield break;
            ffmpeg_error = true;
            yield return this._ffmpegController.FFmpegRunCoroutine(songAudioClipPath, Ebur128Option, null, errorLine =>
            {
                var integratedLoundness = true;
                if (Ebur128ILRegex.Match(errorLine).Success)
                    integratedLoundness = true;
                if (Ebur128LRRegex.Match(errorLine).Success)
                    integratedLoundness = false;
                var match = Ebur128Regex.Match(errorLine);
                if (match.Success)
                {
                    ffmpeg_error = false;
                    switch (match.Groups[1].Value)
                    {
                        case "I":
                            FloatTryParce(match.Groups[2].Value, ref loudnessData.I);
                            break;
                        case "Threshold":
                            if (integratedLoundness)
                                FloatTryParce(match.Groups[2].Value, ref loudnessData.ILTh);
                            else
                                FloatTryParce(match.Groups[2].Value, ref loudnessData.LRTh);
                            break;
                        case "LRA":
                            FloatTryParce(match.Groups[2].Value, ref loudnessData.LRA);
                            break;
                        case "LRA low":
                            FloatTryParce(match.Groups[2].Value, ref loudnessData.LRAlow);
                            break;
                        case "LRA high":
                            FloatTryParce(match.Groups[2].Value, ref loudnessData.LRAhigh);
                            break;
                    }
                }
            });
            yield return new WaitWhile(() => this._ffmpegController._ffmpegProcesses.TryGetValue(songAudioClipPath, out _));
            if (ffmpeg_error)
                yield break;
            if (original)
                songData.Org = loudnessData;
            else
                songData.Now = loudnessData;
            this._songDatabase.SaveSongData(levelID, songData);
            Plugin.Log.Info($"Loudness survey time:{timer.Elapsed.TotalMilliseconds}ms  I:{loudnessData.I}  LRA:{loudnessData.LRA}  LRA low:{loudnessData.LRAlow}  LRA high:{loudnessData.LRAhigh}  MEAN V:{loudnessData.MEAN}  MAX V:{loudnessData.MAX}  id:{levelID}");
            timer.Stop();
            this.OnLoudnessSurveyUpdate?.Invoke(loudnessUpdate, levelID, loudnessData);
        }

        public string GetSongAudioClipPath(string levelID)
        {
            foreach(var customLevel in SongCore.Loader.CustomLevels)
            {
                if (customLevel.Value.levelID == levelID)
                    return customLevel.Value.songPreviewAudioClipPath;
            }
            foreach(var CustomWIPLevel in SongCore.Loader.CustomWIPLevels)
            {
                if (CustomWIPLevel.Value.levelID == levelID)
                    return CustomWIPLevel.Value.songPreviewAudioClipPath;
            }
            return null;
        }

        public async Task<string> GetSongAudioClipPathAsync(string levelID)
        {
            string result = null;
            if (this._cancellationTokenSource != null)
                this._cancellationTokenSource.Cancel();
            using (this._cancellationTokenSource = new CancellationTokenSource())
            {
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

        public static bool FloatTryParce(string text, ref float value)
        {
            if (float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out float parce))
            {
                value = parce;
                return true;
            }
            return false;
        }
    }
}
