using LoudnessNormalizer.Installers;
using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using SiraUtil.Zenject;
using System;
using IPALogger = IPA.Logging.Logger;
using System.Reflection;

namespace LoudnessNormalizer
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        public static Harmony _harmony;
        public const string HARMONY_ID = "com.github.rynan4818.LoudnessNormalizer";
        public static string Name => "LoudnessNormalizer";
        public static event Action OnPluginExit;
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }

        [Init]
        /// <summary>
        /// IPAによってプラグインが最初にロードされたときに呼び出される（ゲームが開始されたとき、またはプラグインが無効な状態で開始された場合は有効化されたときのいずれか）
        /// [Init]コンストラクタを使用するメソッドや、InitWithConfigなどの通常のメソッドの前に呼び出されるメソッド
        /// [Init]は1つのコンストラクタにのみ使用してください
        /// </summary>
        public void Init(IPALogger logger, Config conf, Zenjector zenjector)
        {
            Instance = this;
            Log = logger;
            Log.Info("LoudnessNormalizer initialized.");
            _harmony = new Harmony(HARMONY_ID);

            //BSIPAのConfigを使用する場合はコメントを外します
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Log.Debug("Config loaded");

            //使用するZenjectのインストーラーのコメントを外します
            zenjector.Install<LoudnessNormalizerAppInstaller>(Location.App);
            zenjector.Install<LoudnessNormalizerMenuInstaller>(Location.Menu);
            zenjector.Install<LoudnessNormalizerPlayerInstaller>(Location.Player);
        }

        [OnStart]
        public void OnApplicationStart()
        {
            Log.Debug("OnApplicationStart");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [OnExit]
        public void OnApplicationQuit()
        {
            Log.Debug("OnApplicationQuit");
            OnPluginExit?.Invoke();
            _harmony?.UnpatchSelf();
        }
    }
}
