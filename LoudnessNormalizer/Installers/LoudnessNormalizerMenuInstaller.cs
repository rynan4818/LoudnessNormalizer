using LoudnessNormalizer.Views;
using LoudnessNormalizer.Models;
using Zenject;

namespace LoudnessNormalizer.Installers
{
    public class LoudnessNormalizerMenuInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<SettingTabViewController>().FromNewComponentAsViewController().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<LoudnessNormalizerUIManager>().AsSingle().NonLazy();
            this.Container.BindInterfacesAndSelfTo<MenuSceneComponent>().FromNewComponentOnNewGameObject().AsSingle().NonLazy();
            //this.Container.BindInterfacesAndSelfTo<ConfigViewController>().FromNewComponentAsViewController().AsSingle().NonLazy();
        }
    }
}
