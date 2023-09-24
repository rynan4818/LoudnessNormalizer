using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using LoudnessNormalizer.Configuration;
using LoudnessNormalizer.Models;
using System;
using TMPro;
using Zenject;

namespace LoudnessNormalizer.Views
{
    public class SettingTabViewController :  IInitializable, IDisposable
    {
        private bool _disposedValue;
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

        public void CheckSongCountUpdate(int count)
        {
            this._checkSongCount.text = $"Check Song Count : {count} / {SongCore.Loader.CustomLevels.Count}";
        }

        [UIValue("AllSongCheck")]
        public bool AllSongCheck
        {
            get => PluginConfig.Instance.AllSongCheck;
            set => PluginConfig.Instance.AllSongCheck = value;
        }

        [UIValue("Itarget")]
        public float Itarget
        {
            get => PluginConfig.Instance.Itarget;
            set => PluginConfig.Instance.Itarget = value;
        }

        [UIValue("LRAtarget")]
        public float LRAtarget
        {
            get => PluginConfig.Instance.LRAtarget;
            set => PluginConfig.Instance.LRAtarget = value;
        }

        [UIValue("TPtarget")]
        public float TPtarget
        {
            get => PluginConfig.Instance.TPtarget;
            set => PluginConfig.Instance.TPtarget = value;
        }

        [UIAction("LoudnessNormalization")]
        public void LoudnessNormalization()
        {

        }


        [UIAction("#post-parse")]
        internal void PostParse()
        {
            // Code to run after BSML finishes
        }
    }
}
