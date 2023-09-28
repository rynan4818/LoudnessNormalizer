using IPA.Utilities;
using IPA.Utilities.Async;
using LoudnessNormalizer.Util;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace LoudnessNormalizer.Models
{
    public class FFmpegController
    {
        public readonly string _ffmpegFilepath = Path.Combine(UnityGame.LibraryPath, "ffmpeg.exe");
        public readonly ConcurrentDictionary<string, Process> _ffmpegProcesses = new ConcurrentDictionary<string, Process>();
        public IEnumerator FFmpegRunCoroutine(string songAudioClipPath, string option, Action<string> outputLine, Action<string> errorLine)
        {
            var arguments = $"-i \"{songAudioClipPath}\" {option}";
            Plugin.Log?.Debug($"ffmpeg {arguments}");
            using (var ffmpegProcess = FFmpegProcess(songAudioClipPath, arguments))
            using (var ctoken = new CancellationTokenSource())
            {
                if (ffmpegProcess == null)
                    yield break;
                ffmpegProcess.Exited += (sender, e) => { ctoken.Cancel(); };
                ffmpegProcess.Disposed += (sender, e) => { FFmpegProcessDisposed((Process)sender); };
                if (!ffmpegProcess.Start())
                {
                    Plugin.Log.Info("Faile to start ffmpeg process");
                    yield break;
                }
                var outputRead = Task.Run(() =>
                {
                    while (true)
                    {
                        var l = ffmpegProcess.StandardOutput.ReadLine();
                        if (l == null)
                            break;
                        outputLine?.Invoke(l);
                    }
                });
                var errorRead = Task.Run(() =>
                {
                    while (true)
                    {
                        var l = ffmpegProcess.StandardError.ReadLine();
                        if (l == null)
                            break;
                        errorLine?.Invoke(l);
                    }
                });
                var processWait = Task.Run(() =>
                {
                    ctoken.Token.WaitHandle.WaitOne();
                    ffmpegProcess.WaitForExit();
                });
                var timeout = new TimeoutTimer(60);
                var startProcessTimeout = new TimeoutTimer(10);
                yield return new WaitUntil(() => IsProcessRunning(ffmpegProcess) || startProcessTimeout.HasTimedOut);
                startProcessTimeout.Stop();
                yield return new WaitUntil(() => !IsProcessRunning(ffmpegProcess) || timeout.HasTimedOut);
                if (timeout.HasTimedOut)
                {
                    yield return new WaitForSeconds(5f);
                    Plugin.Log?.Warn($"[{songAudioClipPath}] Timeout reached, disposing ffmpeg process");
                }
                else
                    Task.WaitAll(outputRead, errorRead, processWait);
                timeout.Stop();
                ffmpegProcess.Close();
                DisposeProcess(ffmpegProcess);
            }
        }

        public Process FFmpegProcess(string songAudioClipPath, string arguments)
        {
            if (this._ffmpegProcesses.TryGetValue(songAudioClipPath, out _))
            {
                Plugin.Log?.Warn("Existing process not cleaned up yet. Cancelling survey attempt.");
                return null;
            }
            if (!File.Exists(songAudioClipPath))
            {
                Plugin.Log?.Warn("No song data");
                return null;
            }
            if (!File.Exists(this._ffmpegFilepath))
            {
                Plugin.Log?.Warn("No ffmpeg.exe");
                return null;
            }
            var process = new Process
            {
                StartInfo =
                {
                    FileName = this._ffmpegFilepath,
                    Arguments = arguments,
                    RedirectStandardError = true,
                    StandardErrorEncoding = Encoding.UTF8,
                    RedirectStandardOutput = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true,
                PriorityBoostEnabled = false
            };
            this._ffmpegProcesses.TryAdd(songAudioClipPath, process);
            return process;
        }

        public void FFmpegProcessDisposed(Process sender)
        {
            var disposedProcess = (Process)sender;
            foreach (var dictionaryEntry in this._ffmpegProcesses.Where(keyValuePair => keyValuePair.Value == disposedProcess).ToList())
            {
                var songAudioClipPath = dictionaryEntry.Key;
                var success = this._ffmpegProcesses.TryRemove(dictionaryEntry.Key, out _);
                if (!success)
                    UnityMainThreadTaskScheduler.Factory.StartNew(() => { Plugin.Log?.Error("Failed to remove disposed process from list of processes!"); });
            }
        }

        public static void DisposeProcess(Process process)
        {
            if (process == null)
                return;
            int processId;
            try
            {
                processId = process.Id;
            }
            catch (Exception)
            {
                return;
            }
            Plugin.Log?.Warn($"[{processId}] Cleaning up process");
            Task.Run(() =>
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill();
                }
                catch (Exception exception)
                {
                    UnityMainThreadTaskScheduler.Factory.StartNew(() => Plugin.Log.Warn(exception));
                }
                try
                {
                    process.Dispose();
                }
                catch (Exception exception)
                {
                    UnityMainThreadTaskScheduler.Factory.StartNew(() => Plugin.Log.Warn(exception));
                }
            });
        }

        public static bool IsProcessRunning(Process process)
        {
            try
            {
                if (!process.HasExited)
                    return true;
            }
            catch (Exception e)
            {
                if (!(e is InvalidOperationException))
                {
                    UnityEngine.Debug.LogWarning(e);
                }
            }
            return false;
        }
    }
}
