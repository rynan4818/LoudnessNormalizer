using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.ViewControllers;
using LoudnessNormalizer.Models;
using LoudnessNormalizer.Util;
using System;
using Zenject;

namespace LoudnessNormalizer.Views
{
    public class SettingTabViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        private bool _disposedValue;
        private LoudnessNormalizerController _loudnessNormalizerController;
        public static readonly string _buttonName = "PlayerInfoViewer";
        public string ResourceName => string.Join(".", this.GetType().Namespace, this.GetType().Name);

        [Inject]
        public void Constractor(LoudnessNormalizerController loudnessNormalizerController)
        {
            this._loudnessNormalizerController = loudnessNormalizerController;
        }

        public void Initialize()
        {
            GameplaySetup.instance.AddTab(Plugin.Name, this.ResourceName, this, MenuType.Solo);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    this._loudnessNormalizerController._allSongCheckerActive = false;
                    GameplaySetup.instance?.RemoveTab(Plugin.Name);
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

        [UIAction("AllSongCheckStart")]
        private void AllSongCheckStart()
        {
            CoroutineStarter.Instance.StartCoroutine(this._loudnessNormalizerController.AllSongChekerCoroutine());
        }
        [UIAction("AllSongCheckStop")]
        private void AllSongCheckStop()
        {
            this._loudnessNormalizerController._allSongCheckerActive = false;
        }
        [UIAction("#post-parse")]
        internal void PostParse()
        {
            // Code to run after BSML finishes
        }
    }
}
