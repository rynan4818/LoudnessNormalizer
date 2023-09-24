using System;
using Zenject;

namespace LoudnessNormalizer.Models
{
    public class GameplaySceneController : IInitializable, IDisposable
    {
        private LoudnessNormalizerController _loudnessNormalizerController;
        private bool _disposedValue;
        public GameplaySceneController(LoudnessNormalizerController loudnessNormalizerController)
        {
            this._loudnessNormalizerController = loudnessNormalizerController;
        }
        public void Initialize()
        {
            this._loudnessNormalizerController._allSongCheckerBreak = true;
            this._loudnessNormalizerController._gameSceneActive = true;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    this._loudnessNormalizerController._gameSceneActive = false;
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
    }
}
