using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ChatGLM_Turbo : LLM
{

    #region 参数
    /// <summary>
    /// 选择的模型
    /// </summary>
    [SerializeField] private ModelType m_Type= ModelType.chatglm_turbo;
    /// <summary>
    /// 调用方式  invoke/async-invoke/sse-invoke  先实现同步模式
    /// </summary>
    [SerializeField] private string m_InvokeMethod = "invoke";

    /// <summary>
    /// 智普AI的apikey
    /// </summary>
    [Header("填写智普AI的apikey")]
    [SerializeField] private string m_Key = string.Empty;
    //api key
    [SerializeField] private string m_ApiKey = string.Empty;
    //secret key
    [SerializeField] private string m_SecretKey = string.Empty;
    #endregion

    private void Awake()
    {
        OnInit();
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
        stopwatch.Restart();
        string jsonPayload = JsonConvert.SerializeObject(new RequestData
        {
            model=m_Type.ToString(),
            prompt = m_DataList
        });

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", GetToken());

            yield return request.SendWebRequest();

            if (request.responseCode == 200)
            {
                string _msg = request.downloadHandler.text;
                ResponseData response = JsonConvert.DeserializeObject<ResponseData>(_msg);

                if (response.data.choices.Count > 0)
                {
                    string _msgBack = response.data.choices[0].content;

                    //添加记录
                    m_DataList.Add(new SendData("assistant", _msgBack));
                    //回调
                    _callback(_msgBack);
                }
                else
                {
                    Debug.Log(_msg);
                }
            }

        }

        stopwatch.Stop();
        Debug.Log("chatGLM Turbo耗时：" + stopwatch.Elapsed.TotalSeconds);
    }



    /// <summary>
    /// 初始化
    /// </summary>
    private void OnInit()
    {
        SplitKey();
        GetEndPointUrl();
    }
    /// <summary>
    /// 获取资源路径
    /// </summary>
    private void GetEndPointUrl()
    {
        url = String.Format("https://open.bigmodel.cn/api/paas/v3/model-api/{0}/{1}", m_Type, m_InvokeMethod);
    }
    /// <summary>
    /// 处理key
    /// </summary>
    private void SplitKey()
    {
        try {
            if (m_Key == "")
                return;

            string[] _split = m_Key.Split('.');
            m_ApiKey = _split[0];
            m_SecretKey = _split[1];
        } 
        catch { }


    }

    #region 生成api鉴权token

    /// <summary>
    /// 生成api鉴权 token
    /// </summary>
    /// <returns></returns>
    private string GetToken()
    {
        long expirationMilliseconds = DateTimeOffset.Now.AddHours(1).ToUnixTimeMilliseconds();
        long timestampMilliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        string jwtToken = GenerateJwtToken(m_ApiKey, expirationMilliseconds, timestampMilliseconds);
        return jwtToken;
    }
    //获取token
    private string GenerateJwtToken(string apiKeyId, long expirationMilliseconds, long timestampMilliseconds)
    {
        // 构建Header
        string _headerJson = "{\"alg\":\"HS256\",\"sign_type\":\"SIGN\"}";

        string encodedHeader = Base64UrlEncode(_headerJson);

        // 构建Payload
        string _playLoadJson = string.Format("{{\"api_key\":\"{0}\",\"exp\":{1}, \"timestamp\":{2}}}", apiKeyId, expirationMilliseconds, timestampMilliseconds);

        string encodedPayload = Base64UrlEncode(_playLoadJson);

        // 构建签名
        string signature = HMACsha256(m_SecretKey, $"{encodedHeader}.{encodedPayload}");
        // 组合Header、Payload和Signature生成JWT令牌
        string jwtToken = $"{encodedHeader}.{encodedPayload}.{signature}";

        return jwtToken;
    }
    // Base64 URL编码
    private string Base64UrlEncode(string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        string base64 = Convert.ToBase64String(inputBytes);
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
    // 使用HMAC SHA256生成签名
    private string HMACsha256(string apiSecretIsKey, string buider)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(apiSecretIsKey);
        HMACSHA256 hMACSHA256 = new System.Security.Cryptography.HMACSHA256(bytes);
        byte[] date = Encoding.UTF8.GetBytes(buider);
        date = hMACSHA256.ComputeHash(date);
        hMACSHA256.Clear();

        return Convert.ToBase64String(date);

    }
    #endregion



    #region 数据定义
    /// <summary>
    /// 模型类型
    /// </summary>
    public enum ModelType
    {
        chatglm_turbo,
        characterglm
    }

    [Serializable]
    private class RequestData
    {
        [SerializeField] public string model;
        [SerializeField] public List<SendData> prompt;
        [SerializeField] public float temperature = 0.7f;
    }

    [Serializable]
    private class ResponseData
    {
        [SerializeField] public int code;
        [SerializeField] public string msg = string.Empty;
        [SerializeField] public string success = string.Empty;
        [SerializeField] public ReData data=new ReData();

    }

    [Serializable]
    private class ReData
    {
        [SerializeField] public string task_id = string.Empty;
        [SerializeField] public string request_id = string.Empty;
        [SerializeField] public string task_status = string.Empty;
        [SerializeField] public List<SendData> choices=new List<SendData>();
    }

    #endregion

}
