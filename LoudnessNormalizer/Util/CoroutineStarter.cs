﻿using UnityEngine;

namespace LoudnessNormalizer.Util
{
    public class CoroutineStarter : MonoBehaviour
    {
        private static CoroutineStarter instance;
        public static CoroutineStarter Instance
        {
            get
            {
                if (instance is null)
                {
                    instance = new GameObject("CoroutineStarter").AddComponent<CoroutineStarter>();
                    DontDestroyOnLoad(instance.gameObject);
                }
                return instance;
            }
        }
    }
}
