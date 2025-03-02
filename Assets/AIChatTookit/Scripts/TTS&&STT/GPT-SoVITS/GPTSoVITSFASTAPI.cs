using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using static GPTSoVITSTextToSpeech;

public class GPTSoVITSFASTAPI : TTS
{
    #region 参数定义
    [Header("参考音频路径，是GPT-SoVITS项目下的相对路径")]
    [SerializeField] private string m_ReferWavPath=string.Empty;//参考音频路径
    [Header("参考音频的文字内容")]
    [SerializeField] private string m_ReferenceText = "";//参考音频文本
    [Header("参考音频的语言")]
    [SerializeField] private Language m_ReferenceTextLan = Language.中文;//参考音频的语言
    [Header("合成音频的语言")]
    [SerializeField] private Language m_TargetTextLan = Language.中文;//合成音频的语言

    #endregion

    /// <summary>
    /// 语音合成，返回合成文本
    /// </summary>
    /// <param name="_msg"></param>
    /// <param name="_callback"></param>
    public override void Speak(string _msg, Action<AudioClip, string> _callback)
    {
        StartCoroutine(GetVoice(_msg, _callback));
    }

    /// <summary>
    /// 合成音频
    /// </summary>
    /// <param name="_msg"></param>
    /// <param name="_callback"></param>
    /// <returns></returns>
    private IEnumerator GetVoice(string _msg, Action<AudioClip, string> _callback)
    {
        stopwatch.Restart();
        //发送报文
        RequestData _requestData = new RequestData
        {
            refer_wav_path=m_ReferWavPath,
            prompt_text=m_ReferenceText,
            prompt_language= m_ReferenceTextLan.ToString(),
            text= _msg,
            text_language= m_TargetTextLan.ToString()
        };

        string _postJson = JsonUtility.ToJson(_requestData);//报文

        using (UnityWebRequest request = new UnityWebRequest(m_PostURL, "POST"))
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(_postJson);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
            request.downloadHandler = new DownloadHandlerAudioClip(m_PostURL, AudioType.WAV);

            request.SetRequestHeader("Content-Type", "application/json");

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
        }

    }


    #region 数据定义

    [Serializable]
    public class RequestData
    {
        public string refer_wav_path=string.Empty;//参考音频路径
        public string prompt_text = string.Empty;//参考音频文本
        public string prompt_language=string.Empty;//参考音频语言
        public string text = string.Empty;//合成文本
        public string text_language=string.Empty;//合成语言设置
    }



    #endregion



}
