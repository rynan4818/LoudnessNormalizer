using LoudnessNormalizer.Interfaces;
using System;
using System.Collections.Generic;
using Zenject;

namespace LoudnessNormalizer.Models
{
    public class LoudnessNormalizerUIManager : IInitializable, IDisposable
    {
        private bool _disposedValue;
        private StandardLevelDetailViewController _standardLevelDetail;
        private PlatformLeaderboardViewController _platformLeaderboardViewController;
        private readonly List<IBeatmapInfoUpdater> _beatmapInfoUpdaters;
        public bool _leaderboardActivated { get; private set; } = false;
        public LoudnessNormalizerUIManager(StandardLevelDetailViewController standardLevelDetailViewController,
            List<IBeatmapInfoUpdater> iBeatmapInfoUpdaters,
            PlatformLeaderboardViewController platformLeaderboardViewController)
        {
            this._standardLevelDetail = standardLevelDetailViewController;
            this._beatmapInfoUpdaters = iBeatmapInfoUpdaters;
            this._platformLeaderboardViewController = platformLeaderboardViewController;
        }
        public void Initialize()
        {
            this._standardLevelDetail.didChangeDifficultyBeatmapEvent += this.StandardLevelDetail_didChangeDifficultyBeatmapEvent;
            this._standardLevelDetail.didChangeContentEvent += this.StandardLevelDetail_didChangeContentEvent;
            this._platformLeaderboardViewController.didActivateEvent += this.OnLeaderboardActivated;
            this._platformLeaderboardViewController.didDeactivateEvent += this.OnLeaderboardDeactivated;
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
        public void StandardLevelDetail_didChangeDifficultyBeatmapEvent(StandardLevelDetailViewController arg1, IDifficultyBeatmap arg2)
        {
            if (arg1 != null && arg2 != null)
                this.DiffcultyBeatmapUpdated(arg2);
        }
        public void StandardLevelDetail_didChangeContentEvent(StandardLevelDetailViewController arg1, StandardLevelDetailViewController.ContentType arg2)
        {
            if (arg1 != null && arg1.selectedDifficultyBeatmap != null)
                this.DiffcultyBeatmapUpdated(arg1.selectedDifficultyBeatmap);
        }
        private void DiffcultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap)
        {
            foreach (var beatmapInfoUpdater in _beatmapInfoUpdaters)
                beatmapInfoUpdater.BeatmapInfoUpdated(difficultyBeatmap);
        }
        public void OnLeaderboardActivated(bool firstactivation, bool addedtohierarchy, bool screensystemenabling)
        {
            this._leaderboardActivated = true;
        }
        public void OnLeaderboardDeactivated(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            this._leaderboardActivated = false;
        }
    }
}
