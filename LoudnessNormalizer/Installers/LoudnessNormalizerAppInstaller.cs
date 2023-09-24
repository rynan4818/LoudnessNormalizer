using LoudnessNormalizer.Models;
using Zenject;

namespace LoudnessNormalizer.Installers
{
    public class LoudnessNormalizerAppInstaller : Installer
    {
        public override void InstallBindings()
        {
            this.Container.BindInterfacesAndSelfTo<SongDatabase>().AsSingle();
            this.Container.BindInterfacesAndSelfTo<FFmpegController>().AsSingle();
            this.Container.BindInterfacesAndSelfTo<LoudnessNormalizerController>().AsSingle();
        }
    }
}
