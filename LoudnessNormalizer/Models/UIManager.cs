using LoudnessNormalizer.Util;
using LoudnessNormalizer.Views;
using System;
using Zenject;

namespace LoudnessNormalizer.Models
{
    public class UIManager : IInitializable, IDisposable
    {
        private bool _disposedValue;
        private StandardLevelDetailViewController _standardLevelDetail;
        private PlatformLeaderboardViewController _platformLeaderboardViewController;
        private LoudnessNormalizerController _loudnessNormalizerController;
        private SongDatabase _songDatabase;
        private SettingTabViewController _settingTabViewController;
        public IDifficultyBeatmap _selectedBeatmap;
        public bool _leaderboardActivated { get; private set; } = false;
        public UIManager(StandardLevelDetailViewController standardLevelDetailViewController,
            PlatformLeaderboardViewController platformLeaderboardViewController,
            SongDatabase songDatabase, LoudnessNormalizerController loudnessNormalizerController,
            SettingTabViewController settingTabViewController)
        {
            this._standardLevelDetail = standardLevelDetailViewController;
            this._platformLeaderboardViewController = platformLeaderboardViewController;
            this._songDatabase = songDatabase;
            this._loudnessNormalizerController = loudnessNormalizerController;
            this._settingTabViewController = settingTabViewController;
        }
        public void Initialize()
        {
            this._standardLevelDetail.didChangeDifficultyBeatmapEvent += this.StandardLevelDetail_didChangeDifficultyBeatmapEvent;
            this._standardLevelDetail.didChangeContentEvent += this.StandardLevelDetail_didChangeContentEvent;
            this._platformLeaderboardViewController.didActivateEvent += this.OnLeaderboardActivated;
            this._platformLeaderboardViewController.didDeactivateEvent += this.OnLeaderboardDeactivated;
            this._loudnessNormalizerController.OnLoudnessSurveyUpdate += this.OnLoudnessSurveyUpdate;
            this._loudnessNormalizerController.OnLoudnormProgress += this.OnLoudnormProgress;
            this._settingTabViewController.OnLoudnessNormalization += this.OnLoudnessNormalization;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    this._standardLevelDetail.didChangeDifficultyBeatmapEvent -= this.StandardLevelDetail_didChangeDifficultyBeatmapEvent;
                    this._standardLevelDetail.didChangeContentEvent -= this.StandardLevelDetail_didChangeContentEvent;
                    this._platformLeaderboardViewController.didDeactivateEvent -= this.OnLeaderboardDeactivated;
                    this._platformLeaderboardViewController.didActivateEvent -= this.OnLeaderboardActivated;
                    this._loudnessNormalizerController.OnLoudnessSurveyUpdate -= this.OnLoudnessSurveyUpdate;
                    this._loudnessNormalizerController.OnLoudnormProgress -= this.OnLoudnormProgress;
                    this._settingTabViewController.OnLoudnessNormalization -= this.OnLoudnessNormalization;
                }
                this._disposedValue = true;
            }
        }
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void BeatmapInfoUpdated(IDifficultyBeatmap beatmap)
        {
            if (!this._songDatabase._init)
                return;
            if (beatmap == null)
                return;
            this._selectedBeatmap = beatmap;
            var levelID = beatmap.level.levelID;
            var songData = this._songDatabase.GetSongData(levelID);
            LoudnessData loudnessData = null;
            if (songData.Org == null)
            {
                CoroutineStarter.Instance.StartCoroutine(this._loudnessNormalizerController.SlectSongCheckerCoroutine(levelID, songData));
            }
            else
            {
                if (songData.Now == null)
                {
                    loudnessData = songData.Org;
                }
                else
                {
                    loudnessData = songData.Now;
                }
            }
            this._settingTabViewController.LoudnessUpdate(loudnessData);
        }

        public void OnLoudnessSurveyUpdate(bool loudnessUpdate, string levelID, LoudnessData loudnessData)
        {
            if (loudnessUpdate && this._selectedBeatmap.level.levelID == levelID)
                this._settingTabViewController.LoudnessUpdate(loudnessData);
            this._settingTabViewController.CheckSongCountUpdate(this._songDatabase.DatabaseCount());
        }

        public void StandardLevelDetail_didChangeDifficultyBeatmapEvent(StandardLevelDetailViewController arg1, IDifficultyBeatmap arg2)
        {
            if (arg1 != null && arg2 != null)
                this.BeatmapInfoUpdated(arg2);
        }
        public void StandardLevelDetail_didChangeContentEvent(StandardLevelDetailViewController arg1, StandardLevelDetailViewController.ContentType arg2)
        {
            if (arg1 != null && arg1.selectedDifficultyBeatmap != null)
                this.BeatmapInfoUpdated(arg1.selectedDifficultyBeatmap);
        }
        public void OnLeaderboardActivated(bool firstactivation, bool addedtohierarchy, bool screensystemenabling)
        {
            this._leaderboardActivated = true;
        }
        public void OnLeaderboardDeactivated(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            this._leaderboardActivated = false;
            _= this._songDatabase.SaveSongDatabaseAsync();
        }
        public void OnLoudnormProgress(string progress)
        {
            this._settingTabViewController.ProgressUpdate(progress);
        }
        public void OnLoudnessNormalization()
        {
            CoroutineStarter.Instance.StartCoroutine(this._loudnessNormalizerController.LoudnessChangeCoroutine(this._selectedBeatmap.level.levelID));
        }
    }
}
