using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AudioStreamer : MonoBehaviour
    {
       public AudioSource audioSrc;
    [Min(1024)] public int bytesToDownloadBeforePlaying = 100000;
    [SerializeField] Animator modelAnim;

    public void PlayAudioAsync(string streamURL)
    {
        StartCoroutine(PlayAudioAsyncCoroutine(streamURL, (ulong)bytesToDownloadBeforePlaying));
    }

    IEnumerator PlayAudioAsyncCoroutine(string streamURL, ulong bytesBeforePlayback = 1024)
    {
        if (string.IsNullOrWhiteSpace(streamURL))
        {
            Debug.LogWarning("No audio stream URL provided. Playback skipped.");
            yield break;
        }

        using (var uwr = UnityWebRequestMultimedia.GetAudioClip(streamURL, AudioType.MPEG))
        {
            DownloadHandlerAudioClip dlHandler = (DownloadHandlerAudioClip)uwr.downloadHandler;

            dlHandler.streamAudio = true;
            var download = uwr.SendWebRequest();
            yield return null;

            while (uwr.downloadedBytes < bytesBeforePlayback)
            {
                if (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogWarning($"Error downloading audio stream from:{streamURL} : {uwr.error}");
                    yield break;
                }
                yield return null;
            }
            Debug.LogError(uwr.downloadedBytes);
            AudioClip audioClip = dlHandler.audioClip;

            if (audioClip == null)
                yield break;

            audioSrc.clip = audioClip;
            audioSrc.Play();
            StartCoroutine(DoLipsync());
            yield return download;
        }
    }

    [SerializeField] SkinnedMeshRenderer mainMesh;
    [SerializeField] int mouthOpenBlendshapeIndex;
    float[] _samples = new float[64];
    float clampedLipsync = 0f;

    IEnumerator DoLipsync()
    {
        if (audioSrc.isPlaying)
        {
            yield return new WaitForEndOfFrame();
            audioSrc.GetSpectrumData(_samples, 0, FFTWindow.BlackmanHarris);
            clampedLipsync = Mathf.Clamp(_samples[2] * 3000, 0, 100);
            mainMesh.SetBlendShapeWeight(mouthOpenBlendshapeIndex, clampedLipsync);
            yield return new WaitForEndOfFrame();
            StartCoroutine(DoLipsync());
        }
    }
    
}
