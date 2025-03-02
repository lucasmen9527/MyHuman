using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using UnityEngine;
//using UnityEngine.Networking.Types;

public class XunfeiTextToSpeech : TTS
{
    #region 参数
    /// <summary>
    /// 讯飞的应用设置
    /// </summary>
    [SerializeField]private XunfeiSettings m_XunfeiSettings;
    /// <summary>
    /// host地址
    /// </summary>
    [SerializeField] private string m_HostUrl = "tts-api.xfyun.cn";

    /// <summary>
    /// 音频编码，可选值：
    ///raw：未压缩的pcm
    ///lame：mp3(当aue= lame时需传参sfl = 1)
    ///speex-org-wb;7： 标准开源speex（for speex_wideband，即16k）数字代表指定压缩等级（默认等级为8）
    ///speex-org-nb;7： 标准开源speex（for speex_narrowband，即8k）数字代表指定压缩等级（默认等级为8）
    ///speex;7：压缩格式，压缩等级1 ~10，默认为7（8k讯飞定制speex）
    ///speex-wb;7：压缩格式，压缩等级1 ~10，默认为7（16k讯飞定制speex）
    /// </summary>
    [SerializeField] private string m_Aue = "raw";
    /// <summary>
    /// 发音人
    /// </summary>
    [Header("选择朗读的声音")]
    [SerializeField] private Speaker m_Vcn = Speaker.讯飞小燕;
    /// <summary>
    /// 音量，可选值：[0-100]，默认为50
    /// </summary>
    [SerializeField] private int m_Volume = 50;
    /// <summary>
    /// 语音高，可选值：[0-100]，默认为50
    /// </summary>
    [SerializeField] private int m_Pitch = 50;
    /// <summary>
    /// 语速，可选值：[0-100]，默认为50
    /// </summary>
    [SerializeField] private int m_Speed = 50;

    #endregion

    private void Awake()
    {
        m_XunfeiSettings = this.GetComponent<XunfeiSettings>();
        m_PostURL= "wss://tts-api.xfyun.cn/v2/tts";
    }

    /// <summary>
    /// 语音合成，返回合成文本
    /// </summary>
    /// <param name="_msg"></param>
    /// <param name="_callback"></param>
    public override void Speak(string _msg, Action<AudioClip, string> _callback)
    {
        StartCoroutine(GetSpeech(_msg, _callback));
    }

    /// <summary>
    /// websocket
    /// </summary>
    private ClientWebSocket m_WebSocket;
    private CancellationToken m_CancellationToken;

    #region 获取鉴权Url

    /// <summary>
    /// 获取鉴权url
    /// </summary>
    /// <returns></returns>
    private string GetUrl()
    {
        //获取时间戳
        string date = DateTime.Now.ToString("r");
        //拼接原始的signature
        string signature_origin = string.Format("host: " + m_HostUrl + "\ndate: " + date + "\nGET /v2/tts HTTP/1.1");
        //hmac-sha256算法-签名，并转换为base64编码
        string signature = Convert.ToBase64String(new HMACSHA256(Encoding.UTF8.GetBytes(m_XunfeiSettings.m_APISecret)).ComputeHash(Encoding.UTF8.GetBytes(signature_origin)));
        //拼接原始的authorization
        string authorization_origin = string.Format("api_key=\"{0}\",algorithm=\"hmac-sha256\",headers=\"host date request-line\",signature=\"{1}\"", m_XunfeiSettings.m_APIKey, signature);
        //转换为base64编码
        string authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(authorization_origin));
        //拼接鉴权的url
        string url = string.Format("{0}?authorization={1}&date={2}&host={3}", m_PostURL, authorization, date, m_HostUrl);

        return url;
    }

    #endregion

    #region 语音合成

    /// <summary>
    /// 音频长度
    /// </summary>
    private int m_AudioLenth;
    /// <summary>
    /// 数据队列
    /// </summary>
    Queue<float> m_AudioQueue = new Queue<float>();

    /// <summary>
    /// 获取语音合成
    /// </summary>
    /// <param name="_text"></param>
    /// <param name="_callback"></param>
    /// <returns></returns>
    public IEnumerator GetSpeech(string _text, Action<AudioClip, string> _callback)
    {
        stopwatch.Restart();
        yield return null;

        if (m_WebSocket != null) { m_WebSocket.Abort(); }

        ConnectHost(_text);
        AudioClip _audioClip = AudioClip.Create("audio", 16000 * 60, 1, 16000, true, OnAudioRead);

        //回调
        _callback(_audioClip, _text);

        stopwatch.Stop();
        UnityEngine.Debug.Log("讯飞语音合成耗时：" + stopwatch.Elapsed.TotalSeconds);
    }
    void OnAudioRead(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            if (m_AudioQueue.Count > 0)
            {
                data[i] = m_AudioQueue.Dequeue();
            }
            else
            {
                if (m_WebSocket == null || m_WebSocket.State != WebSocketState.Aborted) m_AudioLenth++;
                data[i] = 0;
            }
        }
    }


    /// <summary>
    /// 连接服务器，合成语音
    /// </summary>
    private async void ConnectHost(string text)
    {
        try
        {
            m_WebSocket = new ClientWebSocket();
            m_CancellationToken = new CancellationToken();
            Uri uri = new Uri(GetUrl());
            await m_WebSocket.ConnectAsync(uri, m_CancellationToken);
            text = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
            //发送的数据
            PostData _postData = new PostData()
            {
                common = new CommonTag(m_XunfeiSettings.m_AppID),
                business = new BusinessTag(m_Aue, GetVoice(m_Vcn), m_Volume, m_Pitch, m_Speed),
                data = new DataTag(2, text)
            };
            //转成json格式
            string _jsonData = JsonUtility.ToJson(_postData);
            await m_WebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(_jsonData)), WebSocketMessageType.Binary, true, m_CancellationToken); //发送数据
            StringBuilder sb = new StringBuilder();
            //播放队列.Clear();
            while (m_WebSocket.State == WebSocketState.Open)
            {
                var result = new byte[4096];
                await m_WebSocket.ReceiveAsync(new ArraySegment<byte>(result), m_CancellationToken);//接受数据
                List<byte> list = new List<byte>(result); while (list[list.Count - 1] == 0x00) list.RemoveAt(list.Count - 1);//去除空字节  
                var str = Encoding.UTF8.GetString(list.ToArray());
                sb.Append(str);
                if (str.EndsWith("}"))
                {
                    //获取返回的数据
                    ResponseData _responseData = JsonUtility.FromJson<ResponseData>(sb.ToString());
                    sb.Clear();

                    if (_responseData.code != 0)
                    {
                        //返回错误
                        PrintErrorLog(_responseData.code);
                        m_WebSocket.Abort();
                        break;
                    }
                    //如果没数据，直接结束
                    if (_responseData.data == null)
                    {
                        Debug.LogError("返回的音频数据为空");
                        m_WebSocket.Abort();
                        break;
                    }
                    //拿到音频数据
                    float[] fs = BytesToFloat(Convert.FromBase64String(_responseData.data.audio));
                    m_AudioLenth += fs.Length;
                    foreach (float f in fs) m_AudioQueue.Enqueue(f);

                    if (_responseData.data.status == 2)
                    {

                        m_WebSocket.Abort();
                        break;
                    }
                }
            }

        }
        catch (Exception ex)
        {
            Debug.LogError("报错信息: " + ex.Message);
            m_WebSocket.Dispose();
        }
    }


    #endregion



    #region 工具方法
    /// <summary>
    /// 打印错误日志
    /// </summary>
    /// <param name="status"></param>
    private void PrintErrorLog(int status)
    {
        if (status == 10005)
        {
            Debug.LogError("appid授权失败");
            return;
        }
        if (status == 10006)
        {
            Debug.LogError("请求缺失必要参数");
            return;
        }
        if (status == 10007)
        {
            Debug.LogError("请求的参数值无效");
            return;
        }
        if (status == 10010)
        {
            Debug.LogError("引擎授权不足");
            return;
        }
        if (status == 10109)
        {
            Debug.LogError("请求文本长度非法");
            return;
        }
        if (status == 10019)
        {
            Debug.LogError("session超时");
            return;
        }
        if (status == 10101)
        {
            Debug.LogError("引擎会话已结束");
            return;
        }
        if (status == 10313)
        {
            Debug.LogError("appid不能为空");
            return;
        }
        if (status == 10317)
        {
            Debug.LogError("版本非法");
            return;
        }
        if (status == 11200)
        {
            Debug.LogError("没有权限");
            return;
        }
        if (status == 11201)
        {
            Debug.LogError("日流控超限");
            return;
        }
        if (status == 10160)
        {
            Debug.LogError("请求数据格式非法");
            return;
        }
        if (status == 10161)
        {
            Debug.LogError("base64解码失败");
            return;
        }
        if (status == 10163)
        {
            Debug.LogError("缺少必传参数，或者参数不合法，具体原因见详细的描述");
            return;
        }
        if (status == 10200)
        {
            Debug.LogError("读取数据超时");
            return;
        }
        if (status == 10222)
        {
            Debug.LogError("网络异常");
            return;
        }
    }

    /// <summary>
    /// byte[]数组转化为AudioClip可读取的float[]类型
    /// </summary>
    /// <param name="byteArray"></param>
    /// <returns></returns>
    public float[] BytesToFloat(byte[] byteArray)
    {
        float[] sounddata = new float[byteArray.Length / 2];
        for (int i = 0; i < sounddata.Length; i++)
        {
            sounddata[i] = BytesToFloat(byteArray[i * 2], byteArray[i * 2 + 1]);
        }
        return sounddata;
    }

    private float BytesToFloat(byte firstByte, byte secondByte)
    {
        //小端和大端顺序要调整
        short s;
        if (BitConverter.IsLittleEndian)
            s = (short)((secondByte << 8) | firstByte);
        else
            s = (short)((firstByte << 8) | secondByte);
        // convert to range from -1 to (just below) 1
        return s / 32768.0F;
    }


    #endregion


    #region 数据定义
    /// <summary>
    /// 发送的数据
    /// </summary>
    [Serializable]
    public class PostData
    {
        [SerializeField] public CommonTag common;
        [SerializeField] public BusinessTag business;
        [SerializeField] public DataTag data;
    }


    [Serializable]
    public class CommonTag
    {
        [SerializeField] public string app_id = string.Empty;
        public CommonTag(string app_id)
        {
            this.app_id = app_id;
        }
    }
    [Serializable]
    public class BusinessTag
    {
        [SerializeField] public string aue = string.Empty;
        [SerializeField] public string vcn = string.Empty;
        [SerializeField] public int volume = 50;
        [SerializeField] public int pitch = 50;
        [SerializeField] public int speed = 50;
        [SerializeField] public string tte = "UTF8";
        public BusinessTag(string aue, string vcn, int volume, int pitch, int speed)
        {
            this.aue = aue;
            this.vcn = vcn;
            this.volume = volume;
            this.pitch = pitch;
            this.speed = speed;
        }
    }

    [Serializable]
    public class DataTag
    {
        [SerializeField] public int status = 2;
        [SerializeField] public string text = string.Empty;
        public DataTag(int status, string text)
        {
            this.status = status;
            this.text = text;
        }
    }

    [Serializable]
    public class ResponseData
    {
        [SerializeField] public int code = 0;
        [SerializeField] public string message = string.Empty;
        [SerializeField] public string sid = string.Empty;
        [SerializeField] public ResponcsedataTag data;
    }
    [Serializable]
    public class ResponcsedataTag
    {
        [SerializeField] public string audio = string.Empty;
        [SerializeField] public string ced = string.Empty;
        [SerializeField] public int status = 2;
    }

    #endregion

    #region 设置项
    public enum Speaker
    {
        讯飞小燕,
        讯飞许久,
        讯飞小萍,
        讯飞小婧,
        讯飞许小宝
    }
    /// <summary>
    /// 设置声音
    /// </summary>
    /// <param name="_speeker"></param>
    /// <returns></returns>
    private string GetVoice(Speaker _speeker)
    {
        if (_speeker == Speaker.讯飞小燕)
        {
            return "xiaoyan";
        }
        if (_speeker == Speaker.讯飞许久)
        {
            return "aisjiuxu";
        }
        if (_speeker == Speaker.讯飞小萍)
        {
            return "aisxping";
        }
        if (_speeker == Speaker.讯飞小婧)
        {
            return "aisjinger";
        }
        if (_speeker == Speaker.讯飞许小宝)
        {
            return "aisbabyxu";
        }

        return "xiaoyan";
    }
    #endregion
}
