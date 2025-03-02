using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LLM;
using UnityEngine.Networking;

public class OpenAITextToSpeech : TTS
{
    #region 参数定义

    [SerializeField] private string api_key=string.Empty;//apikey
    [SerializeField] private ModelType m_ModelType = ModelType.tts_1;//模型
    [SerializeField] private VoiceType m_Voice = VoiceType.onyx;//声音

    #endregion
    private void Awake()
    {
        m_PostURL = "https://api.openai.com/v1/audio/speech";
    }

    /// <summary>
    /// 语音合成，返回合成文本
    /// </summary>
    /// <param name="_msg"></param>
    /// <param name="_callback"></param>
    public override void Speak(string _msg, Action<AudioClip, string> _callback)
    {
        StartCoroutine(GetVoice(_msg, _callback));
    }

    private IEnumerator GetVoice(string _msg, Action<AudioClip, string> _callback)
    {
        stopwatch.Restart();
        using (UnityWebRequest request = UnityWebRequest.Post(m_PostURL, new WWWForm()))
        {
            PostData _postData = new PostData
            {
                model = m_ModelType.ToString().Replace('_','-'),
                input = _msg,
                voice= m_Voice.ToString()
            };

            string _jsonText = JsonUtility.ToJson(_postData).Trim();
            byte[] data = System.Text.Encoding.UTF8.GetBytes(_jsonText);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
            request.downloadHandler = new DownloadHandlerAudioClip(m_PostURL, AudioType.MPEG);

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", string.Format("Bearer {0}", api_key));

            yield return request.SendWebRequest();

            if (request.responseCode == 200)
            {
                AudioClip audioClip = ((DownloadHandlerAudioClip)request.downloadHandler).audioClip;
                _callback(audioClip, _msg);

            }
            else
            {
                Debug.LogError("语音合成失败: " + request.error);
            }

            stopwatch.Stop();
            Debug.Log("openAI语音合成：" + stopwatch.Elapsed.TotalSeconds);
        }
    }

    #region 数据定义

    /// <summary>
    /// 发送的报文
    /// </summary>
    [Serializable]
    public class PostData
    {
        public string model = string.Empty;//模型名称
        public string input = string.Empty;//文本内容
        public string voice = string.Empty;//声音
    }
    /// <summary>
    /// 模型类型
    /// </summary>
    public enum ModelType
    {
        tts_1,
        tts_1_hd
    }
    /// <summary>
    /// 声音类型
    /// </summary>
    public enum VoiceType
    {
        alloy,
        echo,
        fable,
        onyx,
        nova,
        shimmer
    }

    #endregion

}
