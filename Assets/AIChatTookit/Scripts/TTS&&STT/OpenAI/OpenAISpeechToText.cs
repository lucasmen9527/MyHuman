using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class OpenAISpeechToText : STT
{

    /// <summary>
    /// openai的api key
    /// </summary>
    [SerializeField] private string api_key;

    private void Awake()
    {
        m_SpeechRecognizeURL = "https://api.openai.com/v1/audio/transcriptions";
    }

    /// <summary>
    /// openai语音识别
    /// </summary>
    /// <param name="_clip"></param>
    /// <param name="_callback"></param>
    public override void SpeechToText(AudioClip _clip, Action<string> _callback)
    {
        byte[] _audioData = WavUtility.FromAudioClip(_clip);
        StartCoroutine(SendAudioData(_audioData, _callback));
    }

    /// <summary>
    /// 发送数据到api
    /// </summary>
    /// <param name="audioBytes"></param>
    /// <param name="_callback"></param>
    /// <returns></returns>
    private IEnumerator SendAudioData(byte[] audioBytes, Action<string> _callback)
    {
        stopwatch.Restart();
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", audioBytes, "sample.wav", "audio/wav");
        form.AddField("model", "whisper-1");

        UnityWebRequest www = UnityWebRequest.Post(m_SpeechRecognizeURL, form);
        www.SetRequestHeader("Authorization", "Bearer " + api_key);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error sending audio file: " + www.error);
        }
        else
        {
            Response _response = JsonUtility.FromJson<Response>(www.downloadHandler.text);
            _callback(_response.text);
        }

        stopwatch.Stop();
        Debug.Log("OpenAI语音识别耗时：" + stopwatch.Elapsed.TotalSeconds);

    }

    #region 数据定义

    [Serializable]public class Response
    {
        [SerializeField]public string text = string.Empty;
    }


    #endregion


}
