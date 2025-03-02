using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR;
using static OpenAISpeechToText;

[RequireComponent(typeof(BaiduSettings))]
public class BaiduTextToSpeech : TTS
{
    #region 参数
    /// <summary>
    /// token脚本
    /// </summary>
    [SerializeField] private BaiduSettings m_Settings;
    /// <summary>
    /// 语音合成设置
    /// </summary>
    [SerializeField] private PostDataSetting m_Post_Setting;
    #endregion

    private void Awake()
    {
        m_Settings = this.GetComponent<BaiduSettings>();
        m_PostURL = "http://tsn.baidu.com/text2audio";
    }

    #region Public Method


    /// <summary>
    /// 语音合成，返回合成文本
    /// </summary>
    /// <param name="_msg"></param>
    /// <param name="_callback"></param>
    public override void Speak(string _msg, Action<AudioClip, string> _callback)
    {
        StartCoroutine(GetSpeech(_msg, _callback));
    }

    #endregion

    #region Private Method

    /// <summary>
    /// 语音合成的方法
    /// </summary>
    /// <param name="_msg"></param>
    /// <param name="callback"></param>
    /// <returns></returns>
    private IEnumerator GetSpeech(string _msg, Action<AudioClip, string> _callback)
    {
        stopwatch.Restart();
        var _url = m_PostURL;
        var _postParams = new Dictionary<string, string>();
        _postParams.Add("tex", _msg);
        _postParams.Add("tok", m_Settings.m_Token);
        _postParams.Add("cuid", SystemInfo.deviceUniqueIdentifier);
        _postParams.Add("ctp", m_Post_Setting.ctp);
        _postParams.Add("lan", m_Post_Setting.lan);
        _postParams.Add("spd", m_Post_Setting.spd);
        _postParams.Add("pit", m_Post_Setting.pit);
        _postParams.Add("vol", m_Post_Setting.vol);
        _postParams.Add("per", SetSpeeker(m_Post_Setting.per));
        _postParams.Add("aue", m_Post_Setting.aue);

        //拼接参数到链接里
        int i = 0;
        foreach (var item in _postParams)
        {
            _url += i != 0 ? "&" : "?";
            _url += item.Key + "=" + item.Value;
            i++;
        }

        //合成音频
        using (UnityWebRequest _speech = UnityWebRequestMultimedia.GetAudioClip(_url, AudioType.WAV))
        {
            yield return _speech.SendWebRequest();
            if (_speech.error == null)
            {
                var type = _speech.GetResponseHeader("Content-Type");
                if (type.Contains("audio"))
                {
                    var _clip = DownloadHandlerAudioClip.GetContent(_speech);
                    _callback(_clip, _msg);
                }
                else
                {
                    var _response = _speech.downloadHandler.data;
                    string _msgBack = System.Text.Encoding.UTF8.GetString(_response);
                    UnityEngine.Debug.LogError(_msgBack);
                }

            }

            stopwatch.Stop();
            UnityEngine.Debug.Log("百度语音合成耗时：" + stopwatch.Elapsed.TotalSeconds);
        }

    }
    //基础音库:度小宇=1，度小美=0，度逍遥（基础）=3，度丫丫=4
    /// 精品音库:度逍遥（精品）=5003，度小鹿=5118，度博文=106，度小童=110，度小萌=111，度米朵=103，度小娇=5
    private string SetSpeeker(SpeekerRole _role)
    {
        if (_role == SpeekerRole.度小宇) return "1";
        if (_role == SpeekerRole.度小美) return "0";
        if (_role == SpeekerRole.度逍遥) return "3";
        if (_role == SpeekerRole.度丫丫) return "4";
        if (_role == SpeekerRole.JP度小娇) return "5";
        if (_role == SpeekerRole.JP度逍遥) return "5003";
        if (_role == SpeekerRole.JP度小鹿) return "5118";
        if (_role == SpeekerRole.JP度博文) return "106";
        if (_role == SpeekerRole.JP度小童) return "110";
        if (_role == SpeekerRole.JP度小萌) return "111";
        if (_role == SpeekerRole.JP度米朵) return "5";

        return "0";//默认为度小美
    }

    #endregion

    #region 数据格式定义



    /// <summary>
    /// 语音合成的配置信息
    /// </summary>
    [System.Serializable]
    public class PostDataSetting
    {
        /// <summary>
        /// 客户端类型选择，web端填写固定值1
        /// </summary>
        public string ctp = "1";
        /// <summary>
        /// 固定值zh。语言选择,目前只有中英文混合模式，填写固定值zh
        /// </summary>
        [Header("语言设置，固定值zh")] public string lan = "zh";
        /// <summary>
        /// 语速，取值0-15，默认为5中语速
        /// </summary>
        [Header("语速，取值0-15，默认为5中语速")] public string spd = "5";
        /// <summary>
        /// 音调，取值0-15，默认为5中语调
        /// </summary>
        [Header("音调，取值0-15，默认为5中语调")] public string pit = "5";
        /// <summary>
        /// 音量，取值0-15，默认为5中音量（取值为0时为音量最小值，并非为无声）
        /// </summary>
        [Header("音量，取值0-15，默认为5中音量")] public string vol = "5";
        /// <summary>
        /// 基础音库:度小宇=1，度小美=0，度逍遥（基础）=3，度丫丫=4
        /// 精品音库:度逍遥（精品）=5003，度小鹿=5118，度博文=106，度小童=110，度小萌=111，度米朵=103，度小娇=5
        /// </summary>
        [Header("设置朗读的声音")] public SpeekerRole per = SpeekerRole.度小美;
        /// <summary>
        /// 3为mp3格式(默认)； 4为pcm-16k；5为pcm-8k；6为wav（内容同pcm-16k）; 注意aue=4或者6是语音识别要求的格式，
        /// 但是音频内容不是语音识别要求的自然人发音，所以识别效果会受影响。
        /// </summary>
        [Header("设置返回的音频格式")] public string aue = "6";
    }
    /// <summary>
    /// 可选声音
    /// </summary>
    public enum SpeekerRole
    {
        度小宇,
        度小美,
        度逍遥,
        度丫丫,
        JP度逍遥,
        JP度小鹿,
        JP度博文,
        JP度小童,
        JP度小萌,
        JP度米朵,
        JP度小娇
    }

    /// <summary>
    /// 语音合成结果
    /// </summary>
    public class SpeechResponse
    {
        public int error_index;
        public string error_msg;
        public string sn;
        public int idx;
        public bool Success
        {
            get { return error_index == 0; }
        }
        public AudioClip clip;
    }


    #endregion

}
