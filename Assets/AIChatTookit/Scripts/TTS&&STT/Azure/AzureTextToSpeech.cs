using System;
using System.Collections;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class AzureTextToSpeech : TTS
{

    #region 参数定义
    /// <summary>
    /// Azure配置项
    /// </summary>
    [SerializeField] private AzureSettings m_AzureSettings;
    /// <summary>
    /// 朗读的角色
    /// </summary>
    [Header("朗读声音设置")]
    public string voiceName = "zh-CN-XiaomoNeural";
    /// <summary>
    /// 情绪
    /// </summary>
    [Header("朗读的情绪设置")]
    public string style = "chat";//chat  cheerful  angry  excited  sad

    #endregion
    private void Awake()
    {
        m_AzureSettings = this.GetComponent<AzureSettings>();
        m_PostURL = string.Format("https://{0}.tts.speech.microsoft.com/cognitiveservices/v1", m_AzureSettings.serviceRegion);
    }
    /// <summary>
    /// 语音合成
    /// </summary>
    /// <param name="_msg"></param>
    /// <param name="_callback"></param>
    public override void Speak(string _msg, Action<AudioClip> _callback)
    {
        StartCoroutine(GetVoice(_msg, _callback));
    }
    /// <summary>
    /// 语音合成，返回合成文本
    /// </summary>
    /// <param name="_msg"></param>
    /// <param name="_callback"></param>
    public override void Speak(string _msg, Action<AudioClip,string> _callback)
    {
        StartCoroutine(GetVoice(_msg, _callback));
    }

    /// <summary>
    /// restful api语音合成
    /// </summary>
    /// <param name="_msg"></param>
    /// <param name="_callback"></param>
    /// <returns></returns>
    private IEnumerator GetVoice(string _msg, Action<AudioClip> _callback)
    {
        stopwatch.Restart();
        //发送报文
        string textToSpeechRequestBody = GenerateTextToSpeech(m_AzureSettings.language, voiceName, style, 2, _msg);

        using (UnityWebRequest speechRequest = new UnityWebRequest(m_PostURL, "POST"))
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(textToSpeechRequestBody);
            speechRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
            speechRequest.downloadHandler = (DownloadHandler)new DownloadHandlerAudioClip(speechRequest.uri, AudioType.MPEG);

            speechRequest.SetRequestHeader("Ocp-Apim-Subscription-Key", m_AzureSettings.subscriptionKey);
            speechRequest.SetRequestHeader("X-Microsoft-OutputFormat", "audio-16khz-32kbitrate-mono-mp3");
            speechRequest.SetRequestHeader("Content-Type", "application/ssml+xml");

            yield return speechRequest.SendWebRequest();

            if (speechRequest.responseCode == 200)
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(speechRequest);
                _callback(audioClip);
            }
            else
            {
                Debug.LogError("语音合成失败: " + speechRequest.error);
            }
        }

        stopwatch.Stop();
        Debug.Log("Azure语音合成耗时：" + stopwatch.Elapsed.TotalSeconds);
    }
    /// <summary>
    ///  restful api语音合成，返回合成文本
    /// </summary>
    /// <param name="_msg"></param>
    /// <param name="_callback"></param>
    /// <returns></returns>
    private IEnumerator GetVoice(string _msg, Action<AudioClip,string> _callback)
    {
        stopwatch.Restart();
        //发送报文
        string textToSpeechRequestBody = GenerateTextToSpeech(m_AzureSettings.language, voiceName, style, 2, _msg);

        using (UnityWebRequest speechRequest = new UnityWebRequest(m_PostURL, "POST"))
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(textToSpeechRequestBody);
            speechRequest.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
            speechRequest.downloadHandler = (DownloadHandler)new DownloadHandlerAudioClip(speechRequest.uri, AudioType.MPEG);

            speechRequest.SetRequestHeader("Ocp-Apim-Subscription-Key", m_AzureSettings.subscriptionKey);
            speechRequest.SetRequestHeader("X-Microsoft-OutputFormat", "audio-16khz-32kbitrate-mono-mp3");
            speechRequest.SetRequestHeader("Content-Type", "application/ssml+xml");

            yield return speechRequest.SendWebRequest();

            if (speechRequest.responseCode == 200)
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(speechRequest);
                _callback(audioClip, _msg);
            }
            else
            {
                Debug.LogError("语音合成失败: " + speechRequest.error);
            }
        }

        stopwatch.Stop();
        Debug.Log("Azure语音合成耗时：" + stopwatch.Elapsed.TotalSeconds);
    }

    /// <summary>
    /// 报文格式转换
    /// </summary>
    /// <param name="lang"></param>
    /// <param name="name"></param>
    /// <param name="style"></param>
    /// <param name="styleDegree"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    public string GenerateTextToSpeech(string lang, string name, string style, int styleDegree, string text)
    {
        string xml = string.Format(@"<speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis""
            xmlns:mstts=""https://www.w3.org/2001/mstts"" xml:lang=""{0}"">
            <voice name=""{1}"">
                <mstts:express-as style=""{2}"" styledegree=""{3}"">
                    {4}
                </mstts:express-as>
            </voice>
        </speak>", lang, name, style, styleDegree, text);

        return xml;
    }

}
