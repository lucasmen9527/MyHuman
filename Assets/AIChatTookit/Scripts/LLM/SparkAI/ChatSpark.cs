using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class ChatSpark : LLM
{

    #region 参数
    /// <summary>
    /// 讯飞的应用设置
    /// </summary>
    [SerializeField] private XunfeiSettings m_XunfeiSettings;
    /// <summary>
    /// 选择星火大模型版本
    /// </summary>
    [Header("选择星火大模型版本")]
    [SerializeField] private ModelType m_SparkModel = ModelType.星火大模型V15;


    #endregion
    
    private void Awake()
    {
        OnInit();
    }
    /// <summary>
    /// 初始化
    /// </summary>
    private void OnInit()
    {
        m_XunfeiSettings = this.GetComponent<XunfeiSettings>();
        if (m_SparkModel == ModelType.星火大模型V15)
        {
            url= "https://spark-api.xf-yun.com/v1.1/chat";
            return;
        }

        url = "https://spark-api.xf-yun.com/v2.1/chat";
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <returns></returns>
    public override void PostMsg(string _msg, Action<string> _callback)
    {
        base.PostMsg(_msg, _callback);
    }

    /// <summary>
    /// 发送数据
    /// </summary> 
    /// <param name="_postWord"></param>
    /// <param name="_callback"></param>
    /// <returns></returns>
    public override IEnumerator Request(string _postWord, System.Action<string> _callback)
    {
        yield return null;
        //处理发送数据
        RequestData requestData = new RequestData();
        requestData.header.app_id = m_XunfeiSettings.m_AppID;

        //判断v1.5还是v2
        requestData.parameter.chat.domain = GetDomain();

        //添加对话列表
        List<PostMsgData> _tempList = new List<PostMsgData>();
        for(int i=0;i<m_DataList.Count;i++)
        {
            PostMsgData _msg = new PostMsgData()
            {
                role = m_DataList[i].role,
                content = m_DataList[i].content
            };
            _tempList.Add(_msg);
        }
       
        requestData.payload.message.text= _tempList;

        string _json = JsonUtility.ToJson(requestData);

        //websocket连接
        ConnectHost(_json, _callback);

    }

    /// <summary>
    /// 指定访问的领域
    /// general指向V1.5版本
    /// generalv2指向V2版本
    /// </summary>
    /// <returns></returns>
    private string GetDomain()
    {
        if (m_SparkModel == ModelType.星火大模型V15)
            return "general";

        return "generalv2";
    }


    #region 获取鉴权Url

    /// <summary>
    /// 获取鉴权url
    /// </summary>
    /// <returns></returns>
    private string GetAuthUrl()
    {
        string date = DateTime.UtcNow.ToString("r");

        Uri uri = new Uri(url);
        StringBuilder builder = new StringBuilder("host: ").Append(uri.Host).Append("\n").//
                                Append("date: ").Append(date).Append("\n").//
                                Append("GET ").Append(uri.LocalPath).Append(" HTTP/1.1");

        string sha = HMACsha256(m_XunfeiSettings.m_APISecret, builder.ToString());
        string authorization = string.Format("api_key=\"{0}\", algorithm=\"{1}\", headers=\"{2}\", signature=\"{3}\"", m_XunfeiSettings.m_APIKey, "hmac-sha256", "host date request-line", sha);

        string NewUrl = "https://" + uri.Host + uri.LocalPath;

        string path1 = "authorization" + "=" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(authorization));
        date = date.Replace(" ", "%20").Replace(":", "%3A").Replace(",", "%2C");
        string path2 = "date" + "=" + date;
        string path3 = "host" + "=" + uri.Host;

        NewUrl = NewUrl + "?" + path1 + "&" + path2 + "&" + path3;
        return NewUrl;
    }

    public string HMACsha256(string apiSecretIsKey, string buider)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(apiSecretIsKey);
        System.Security.Cryptography.HMACSHA256 hMACSHA256 = new System.Security.Cryptography.HMACSHA256(bytes);
        byte[] date = System.Text.Encoding.UTF8.GetBytes(buider);
        date = hMACSHA256.ComputeHash(date);
        hMACSHA256.Clear();

        return Convert.ToBase64String(date);

    }

    #endregion

    #region websocket连接
    /// <summary>
    /// websocket
    /// </summary>
    private ClientWebSocket m_WebSocket;
    private CancellationToken m_CancellationToken;
    /// <summary>
    /// 连接服务器，获取回复
    /// </summary>
    private async void ConnectHost(string text,Action<string> _callback)
    {
        try
        {
            stopwatch.Restart();

            m_WebSocket = new ClientWebSocket();
            m_CancellationToken = new CancellationToken();
            string authUrl = GetAuthUrl();
            string url = authUrl.Replace("http://", "ws://").Replace("https://", "wss://");

            //Uri uri = new Uri(GetUrl());
            Uri uri = new Uri(url);
            await m_WebSocket.ConnectAsync(uri, m_CancellationToken);

            //发送json
            string _jsonData = text;
            await m_WebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(_jsonData)), WebSocketMessageType.Binary, true, m_CancellationToken); //发送数据
            StringBuilder sb = new StringBuilder();
            //用于拼接返回的答复
            string _callBackMessage = "";

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

                    if (_responseData.header.code != 0)
                    {
                        //返回错误
                        //PrintErrorLog(_responseData.code);
                        Debug.Log("错误码：" + _responseData.header.code);
                        m_WebSocket.Abort();
                        break;
                    }
                    //没有回复数据
                    if (_responseData.payload.choices.text.Count == 0)
                    {
                        Debug.LogError("没有获取到回复的信息！");
                        m_WebSocket.Abort();
                        break;
                    }
                    //拼接回复的数据
                    _callBackMessage += _responseData.payload.choices.text[0].content;

                    if (_responseData.payload.choices.status == 2)
                    {
                        stopwatch.Stop();
                        Debug.Log("ChatSpark耗时：" + stopwatch.Elapsed.TotalSeconds);

                        //添加记录
                        m_DataList.Add(new SendData("assistant", _callBackMessage));

                        //回调
                        _callback(_callBackMessage);
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

    #region 数据定义

    //发送的数据
    [Serializable]
    private class RequestData
    {
        public HeaderData header=new HeaderData();
        public ParameterData parameter = new ParameterData();
        public MessageData payload = new MessageData();
    }
    [Serializable]
    private class HeaderData
    {
        public string app_id = string.Empty;//必填
        public string uid="admin";//选填，用户的ID
    }
    [Serializable]
    private class ParameterData
    {
        public ChatParameter chat=new ChatParameter();
    }
    [Serializable]
    private class ChatParameter
    {
        public string domain = "general";
        public float temperature = 0.5f;
        public int max_tokens = 1024;
    }

    [Serializable]
    private class MessageData
    {
        public TextData message=new TextData();
    }
    [Serializable]
    private class TextData
    {
        public List<PostMsgData> text = new List<PostMsgData>();
    }
    [Serializable]
    private class PostMsgData
    {
        public string role = string.Empty;
        public string content = string.Empty;
    }

    //接收的数据
    [Serializable]
    private class ResponseData
    {
        public ReHeaderData header = new ReHeaderData();
        public PayloadData payload = new PayloadData();
    }

    [Serializable]
    private class ReHeaderData{
        public int code;//错误码，0表示正常，非0表示出错
        public string message=string.Empty;//会话是否成功的描述信息
        public string sid=string.Empty;
        public int status;//会话状态，取值为[0,1,2]；0代表首次结果；1代表中间结果；2代表最后一个结果
    }
    [Serializable]
    private class PayloadData
    {
        public ChoicesData choices = new ChoicesData();
        //usage 暂时没用，需要的话自行拓展
    }
    [Serializable]
    private class ChoicesData
    {
        public int status;
        public int seq;
        public List<ReTextData> text = new List<ReTextData>();
    }
    [Serializable]
    private class ReTextData
    {
        public string content = string.Empty;
        public string role = string.Empty;
        public int index;
    }

    private enum ModelType
    {
        星火大模型V15,
        星火大模型V20
    }

    #endregion


}
