using System.Threading.Tasks;
using HarmonyLib;
using IPA.Utilities.Async;
using UnityEngine;

namespace LoudnessNormalizer.HarmonyPatches
{
    [HarmonyPatch(typeof(MediaAsyncLoader), nameof(MediaAsyncLoader.LoadAudioClipFromFilePathAsync))]
    public class MediaAsyncLoaderPattch
    {
        public static void Postfix(string filePath,ref Task<AudioClip> __result)
        {
            //UnityMainThreadTaskScheduler.Factory.StartNew(() => Plugin.Log.Info(filePath));
            __result.ContinueWith(_ =>
            {
                //UnityMainThreadTaskScheduler.Factory.StartNew(() => Plugin.Log.Info($"{filePath}:Comp"));
            });
        }
    }
}
