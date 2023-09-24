using UnityEngine;
using Zenject;
using LoudnessNormalizer.Configuration;
using LoudnessNormalizer.Util;

namespace LoudnessNormalizer.Models
{
    /// <summary>
    /// Monobehaviours (scripts) are added to GameObjects.
    /// For a full list of Messages a Monobehaviour can receive from the game, see https://docs.unity3d.com/ScriptReference/MonoBehaviour.html.
    /// </summary>
	public class MenuSceneComponent : MonoBehaviour
    {
        private LoudnessNormalizerController _loudnessNormalizerController;
        private UIManager _loudnessNormalizerUIManager;
        private SongDatabase _songDatabase;
        [Inject]
        public void Constractor(UIManager loudnessNormalizerUIManager, LoudnessNormalizerController loudnessNormalizerController, SongDatabase songDatabase)
        {
            this._loudnessNormalizerController = loudnessNormalizerController;
            this._songDatabase = songDatabase;
            this._loudnessNormalizerUIManager = loudnessNormalizerUIManager;
        }
        #region Monobehaviour Messages
        /// <summary>
        /// Only ever called once, mainly used to initialize variables.
        /// </summary>
        private void Awake()
        {

        }
        /// <summary>
        /// Only ever called once on the first frame the script is Enabled. Start is called after every other script's Awake() and before Update().
        /// </summary>
        private void Start()
        {

        }

        /// <summary>
        /// Called every frame if the script is enabled.
        /// </summary>
        private void Update()
        {

        }

        /// <summary>
        /// Called every frame after every other enabled script's Update().
        /// </summary>
        private void LateUpdate()
        {
            if (!this._songDatabase._init)
                return;
            if (SongCore.Loader.AreSongsLoading || !this._loudnessNormalizerUIManager._leaderboardActivated || !PluginConfig.Instance.AllSongCheck)
            {
                if (this._loudnessNormalizerController._allSongCheckerActive)
                    this._loudnessNormalizerController._allSongCheckerBreak = true;
                return;
            }
            if (this._loudnessNormalizerController._allSongCheckDone && this._loudnessNormalizerController._allSongCheckCount == SongCore.Loader.CustomLevels.Count)
                return;
            if (!this._loudnessNormalizerController._allSongCheckerActive)
                CoroutineStarter.Instance.StartCoroutine(this._loudnessNormalizerController.AllSongCheckerCoroutine());
        }

        /// <summary>
        /// Called when the script becomes enabled and active
        /// </summary>
        private void OnEnable()
        {

        }

        /// <summary>
        /// Called when the script becomes disabled or when it is being destroyed.
        /// </summary>
        private void OnDisable()
        {
            this._loudnessNormalizerController._allSongCheckerBreak = true;
        }

        /// <summary>
        /// Called when the script is being destroyed.
        /// </summary>
        private void OnDestroy()
        {
            this._loudnessNormalizerController._allSongCheckerBreak = true;
        }
        #endregion
    }
}
