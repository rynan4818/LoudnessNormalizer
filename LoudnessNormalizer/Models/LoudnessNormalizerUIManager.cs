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
        private readonly List<IBeatmapInfoUpdater> _beatmapInfoUpdaters;
        public LoudnessNormalizerUIManager(StandardLevelDetailViewController standardLevelDetailViewController, List<IBeatmapInfoUpdater> iBeatmapInfoUpdaters)
        {
            _standardLevelDetail = standardLevelDetailViewController;
            _beatmapInfoUpdaters = iBeatmapInfoUpdaters;
        }
        public void Initialize()
        {
            _standardLevelDetail.didChangeDifficultyBeatmapEvent += StandardLevelDetail_didChangeDifficultyBeatmapEvent;
            _standardLevelDetail.didChangeContentEvent += StandardLevelDetail_didChangeContentEvent;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    _standardLevelDetail.didChangeDifficultyBeatmapEvent -= StandardLevelDetail_didChangeDifficultyBeatmapEvent;
                    _standardLevelDetail.didChangeContentEvent -= StandardLevelDetail_didChangeContentEvent;
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
                DiffcultyBeatmapUpdated(arg2);
        }
        public void StandardLevelDetail_didChangeContentEvent(StandardLevelDetailViewController arg1, StandardLevelDetailViewController.ContentType arg2)
        {
            if (arg1 != null && arg1.selectedDifficultyBeatmap != null)
                DiffcultyBeatmapUpdated(arg1.selectedDifficultyBeatmap);
        }
        private void DiffcultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap)
        {
            foreach (var beatmapInfoUpdater in _beatmapInfoUpdaters)
                beatmapInfoUpdater.BeatmapInfoUpdated(difficultyBeatmap);
        }
    }
}
