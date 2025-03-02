using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
// using UnityEditor.PackageManager.Requests;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

public class GPTSoVITSTextToSpeech : TTS
{
    #region 参数定义
    [Header("挂载参考的音频，必须设置")]
    [SerializeField] private AudioClip m_ReferenceClip = null;//参考音频
    [Header("参考音频的文字内容，必须设置")]
    [SerializeField] private string m_ReferenceText="";//参考音频文本
    [Header("参考音频的语言")]
    [SerializeField] private Language m_ReferenceTextLan = Language.中文;//参考音频的语言
    [Header("合成音频的语言")]
    [SerializeField] private Language m_TargetTextLan= Language.中文;//合成音频的语言
    private string m_AudioBase64String = "";//参考音频的base64编码
    [SerializeField] private string m_SplitType = "不切";//合成文本切分方式
    [SerializeField] private int m_Top_k = 5;
    [SerializeField] private float m_Top_p = 1;
    [SerializeField] private float m_Temperature = 1;
    [SerializeField] private bool m_TextReferenceMode = false;
    #endregion

    private void Awake()
    {
        AudioTurnToBase64();
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
        string _postJson = GetPostJson(_msg);

        using (UnityWebRequest request = new UnityWebRequest(m_PostURL, "POST"))
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(_postJson);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.responseCode == 200)
            {
                string _text = request.downloadHandler.text;

                Response _response=JsonUtility.FromJson<Response>(_text);
                string _wavPath = _response.data[0].name;


                if (_wavPath == "")
                {
                    //如果合成失败，再尝试一次
                    StartCoroutine(GetVoice(_msg, _callback));
                }
                else
                {
                    StartCoroutine(GetAudioFromFile(_wavPath, _msg, _callback));
                }

            }
            else
            {
                Debug.LogError("语音合成失败: " + request.error);
            }
        }

        stopwatch.Stop();
        Debug.Log("GPT-SoVITS合成耗时：" + stopwatch.Elapsed.TotalSeconds);
    }


    /// <summary>
    /// 处理发送的Json报文
    /// </summary>
    /// <param name="_msg"></param>
    /// <param name="_lan"></param>
    /// <returns></returns>
    private string GetPostJson(string _msg)
    {

        if(m_ReferenceText==""|| m_ReferenceClip == null)
        {
            Debug.LogError("GPT-SoVITS未配置参考音频或参考文本");
            return null;
        }


        // 创建数据结构
        var jsonData = new
        {
            data = new List<object>
            {
                new { name = "audio.wav", data = "data:audio/wav;base64,"+m_AudioBase64String },
                m_ReferenceText,
                m_ReferenceTextLan.ToString(),
                _msg,
                m_TargetTextLan.ToString(),
                m_SplitType,
                m_Top_k,
                m_Top_p,
                m_Temperature,
                m_TextReferenceMode
            }
        };

        // 将数据转换为JSON格式
        string jsonString = JsonConvert.SerializeObject(jsonData, Formatting.Indented);

        return jsonString;
    }

    /// <summary>
    /// 将音频转为base64
    /// </summary>
    private void AudioTurnToBase64()
    {
        if (m_ReferenceClip == null)
        {
            Debug.LogError("GPT-SoVITS未配置参考音频");
            return;
        }
        byte[] audioData = WavUtility.FromAudioClip(m_ReferenceClip);
        string base64String = Convert.ToBase64String(audioData);
        m_AudioBase64String= base64String;
    }
    /// <summary>
    /// 从本地获取合成后的音频文件
    /// </summary>
    /// <param name="_path"></param>
    /// <param name="_msg"></param>
    /// <param name="_callback"></param>
    /// <returns></returns>
    private IEnumerator GetAudioFromFile(string _path,string _msg, Action<AudioClip, string> _callback)
    {
        string filePath = "file://" + _path;
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(filePath, AudioType.WAV))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(request);
                _callback(audioClip, _msg);
            }
            else
            {
                Debug.LogError("音频读取失败 ：" + request.error);
            }
        }


    }



    #region 数据定义

    /*
     发送的数据格式

    {
  "data": [
    {"name":"audio.wav","data":"data:audio/wav;base64,UklGRiQAAABXQVZFZm10IBAAAAABAAEARKwAAIhYAQACABAAZGF0YQAAAAA="},
    "hello world",
    "中文",
    "hello world",
    "中文",
    ]}   

    */
    /*
    
    返回的数据格式

    {
	"data": [
		{
			"name": "E:\\AIProjects\\GPT-SoVITS\\TEMP\\tmp53eoney1.wav",
			"data": null,
			"is_file": true
		}
	],
	"is_generating": true,
	"duration": 7.899699926376343,
	"average_duration": 7.899699926376343
    }

    */

    [Serializable]
    public class Response
    {
        public List<AudioBack> data=new List<AudioBack>();
        public bool is_generating = true;
        public float duration;
        public float average_duration;
    }
    [Serializable]
    public class AudioBack
    {
        public string name=string.Empty;
        public string data = string.Empty;
        public bool is_file = true;

    }

    public enum Language
    {
        中文,
        英文,
        日文,
        中英混合,
        日英混合,
        多语种混合
    }

 


    #endregion


}
