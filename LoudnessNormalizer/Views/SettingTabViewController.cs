using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.GameplaySetup;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using Zenject;

namespace LoudnessNormalizer.Views
{
    public class SettingTabViewController : BSMLAutomaticViewController, IInitializable, IDisposable
    {
        private bool _disposedValue;
        public static readonly string _buttonName = "PlayerInfoViewer";
        public string ResourceName => string.Join(".", this.GetType().Namespace, this.GetType().Name);

        public void Initialize()
        {
            GameplaySetup.instance.AddTab(Plugin.Name, this.ResourceName, this, MenuType.Solo);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue)
            {
                if (disposing)
                    GameplaySetup.instance?.RemoveTab(Plugin.Name);
                this._disposedValue = true;
            }
        }
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        [UIAction("CashClear")]
        private void CashClear()
        {
        }
        [UIAction("#post-parse")]
        internal void PostParse()
        {
            // Code to run after BSML finishes
        }
    }
}
