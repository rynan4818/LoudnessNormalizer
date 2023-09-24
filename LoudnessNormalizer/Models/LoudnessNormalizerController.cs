using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System;
using UnityEngine;
using System.Text.RegularExpressions;

namespace LoudnessNormalizer.Models
{
    public class LoudnessNormalizerController
    {
        private BeatmapLevelsModel _beatmapLevelsModel;
        private SongDatabase _songDatabase;
        private FFmpegController _ffmpegController;
        public static readonly string VolumedetectOption = "-hide_banner -y -vn -af volumedetect -f null -";
        public static readonly string LoudnormSurveyOption = "-hide_banner -y -vn -af \"loudnorm=print_format=json\" -f null -";
        public static readonly string Ebur128Option = "-hide_banner -y -vn -filter_complex ebur128=framelog=verbose -f null -";
        public static readonly Regex VolumedetectRegex = new Regex(@"\[Parsed_volumedetect[^\]]+\] (\w+): ([-\d\.]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex Ebur128ILRegex = new Regex(@" +Integrated loudness:", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex Ebur128LRRegex = new Regex(@" +Loudness range:", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex Ebur128Regex = new Regex(@" +(\w[\w ]*): +([-\d\.]+) \w+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public static readonly Regex LoudnormRegex = new Regex(@"Parsed_loudnorm", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        public CancellationTokenSource _cancellationTokenSource;
        public event Action<bool, string, LoudnessData> OnLoudnessSurveyUpdate;
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

        public IEnumerator LoudnessSurveyCoroutine(string levelID, SongData songData, bool loudnessUpdate, bool original = true, string songAudioClipPath = null)
        {
            var timer = new Stopwatch();
            timer.Start();
            var loudnessData = new LoudnessData();
            if (songAudioClipPath == null)
            {
                //GetBeatmapLevelAsyncはメニューに今表示しているやつじゃないと検索できない
                //AllSongChekerとかで呼ぶとフリーズする
                var getSongPath = GetSongAudioClipPathAsync(levelID);
                yield return getSongPath;
                songAudioClipPath = getSongPath.Result;
            }
            if (songAudioClipPath == null)
                yield break;
            var ffmpeg_error = true;
            yield return this._ffmpegController.FFmpegRunCoroutine(outputLine => { }, errorLine =>
            {
                float value;
                var match = VolumedetectRegex.Match(errorLine);
                if (match.Success)
                {
                    ffmpeg_error = false;
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
            yield return new WaitWhile(() => this._ffmpegController._ffmpegProcesses.TryGetValue(songAudioClipPath, out _));
            if (ffmpeg_error)
                yield break;
            ffmpeg_error = true;
            yield return this._ffmpegController.FFmpegRunCoroutine(outputLine => { }, errorLine =>
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
                    ffmpeg_error = false;
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
    }
}
