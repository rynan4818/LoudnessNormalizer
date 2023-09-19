using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.ViewControllers;
using LoudnessNormalizer.Configuration;
using LoudnessNormalizer.Models;
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

        [UIValue("AllSongCheck")]
        public bool AllSongCheck
        {
            get => PluginConfig.Instance.AllSongCheck;
            set => PluginConfig.Instance.AllSongCheck = value;
        }
        [UIValue("GameSceneCheckStop")]
        public bool GameSceneCheckStop
        {
            get => PluginConfig.Instance.GameSceneCheckStop;
            set => PluginConfig.Instance.GameSceneCheckStop = value;
        }
        //CoroutineStarter.Instance.StartCoroutine(this._loudnessNormalizerController.AllSongChekerCoroutine());

        [UIAction("#post-parse")]
        internal void PostParse()
        {
            // Code to run after BSML finishes
        }
    }
}
