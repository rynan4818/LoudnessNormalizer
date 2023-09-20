using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.ViewControllers;
using LoudnessNormalizer.Configuration;
using LoudnessNormalizer.Models;
using System;
using TMPro;
using Zenject;

namespace LoudnessNormalizer.Views
{
    public class SettingTabViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        private bool _disposedValue;
        private LoudnessNormalizerController _loudnessNormalizerController;
        public static readonly string _buttonName = "PlayerInfoViewer";
        public string ResourceName => string.Join(".", this.GetType().Namespace, this.GetType().Name);
        [UIComponent("IntegratedLoudness")]
        public readonly TextMeshProUGUI _integratedLoudness;
        [UIComponent("LoudnessRange")]
        public readonly TextMeshProUGUI _loudnessRange;
        [UIComponent("Volumedetect")]
        public readonly TextMeshProUGUI _volumedetect;
        [UIComponent("CheckSongCount")]
        private readonly TextMeshProUGUI _checkSongCount;

        [Inject]
        public void Constractor(LoudnessNormalizerController loudnessNormalizerController)
        {
            this._loudnessNormalizerController = loudnessNormalizerController;
        }

        public void Initialize()
        {
            GameplaySetup.instance.AddTab(Plugin.Name, this.ResourceName, this, MenuType.Solo);
            this._loudnessNormalizerController.OnCheckSongCountUpdate += CheckSongCountUpdate;
            this._loudnessNormalizerController.OnLoudnessUpdate += LoudnessUpdate;
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                {
                    GameplaySetup.instance?.RemoveTab(Plugin.Name);
                    this._loudnessNormalizerController.OnCheckSongCountUpdate -= CheckSongCountUpdate;
                    this._loudnessNormalizerController.OnLoudnessUpdate -= LoudnessUpdate;
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

        public void LoudnessUpdate(LoudnessData data)
        {
            if (data == null)
            {
                this._integratedLoudness.text = "Checking now...";
                this._loudnessRange.text = "";
                this._volumedetect.text = "";
            }
            else
            {
                this._integratedLoudness.text = $"Integrated loudness     I: {data.I} LUFS    Threshold: {data.ILTh} LUFS";
                this._loudnessRange.text      = $"Loudness range          LRA: {data.LRA} LU    Threshold: {data.LRTh} LUFS    LRA low: {data.LRAlow} LUFS   LRA high: {data.LRAhigh} LUFS";
                this._volumedetect.text       = $"Volumedetect            Mean : {data.MEAN} dB    Max : {data.MAX} dB";
            }
        }

        public void CheckSongCountUpdate(int count, int max)
        {
            this._checkSongCount.text = $"Check Song Count : {count} / {max}";
        }

        [UIValue("AllSongCheck")]
        public bool AllSongCheck
        {
            get => PluginConfig.Instance.AllSongCheck;
            set => PluginConfig.Instance.AllSongCheck = value;
        }

        [UIAction("#post-parse")]
        internal void PostParse()
        {
            // Code to run after BSML finishes
        }
    }
}
