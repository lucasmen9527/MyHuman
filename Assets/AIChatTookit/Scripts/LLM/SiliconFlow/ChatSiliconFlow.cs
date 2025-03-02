using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ChatSiliconFlow : LLM
{
    public ChatSiliconFlow()
    {
        url = "https://api.siliconflow.cn/v1/chat/completions";
    }

    /// <summary>
    /// API Key
    /// </summary>
    [SerializeField] private string api_key;
    
    /// <summary>
    /// AI设定
    /// </summary>
    public string m_SystemSetting = string.Empty;
    
    /// <summary>
    /// 模型名称
    /// </summary>
    public string m_ModelName = "Pro/deepseek-ai/DeepSeek-R1";

    private void Start()
    {
        // 运行时，添加 AI 设定
        m_DataList.Add(new SendData("system", m_SystemSetting));
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    public override void PostMsg(string _msg, Action<string> _callback)
    {
        base.PostMsg(_msg, _callback);
    }

    /// <summary>
    /// 调用接口
    /// </summary>
    public override IEnumerator Request(string _postWord, System.Action<string> _callback)
    {
        stopwatch.Restart();
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            // 构建请求数据
            PostData _postData = new PostData
            {
                model = m_ModelName,
                messages = m_DataList,
                stream = false,
                max_tokens = 512,
                stop = null,
                temperature = 0.7f,
                top_p = 0.7f,
                top_k = 50,
                frequency_penalty = 0.5f,
                n = 1,
                response_format = new ResponseFormat { type = "text" }
            };

            // 序列化 JSON
            string _jsonText = JsonUtility.ToJson(_postData);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(_jsonText);
            request.uploadHandler = new UploadHandlerRaw(data);
            request.downloadHandler = new DownloadHandlerBuffer();

            // 设置请求头
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {api_key}");

            // 发送请求
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string _msgBack = request.downloadHandler.text;
                MessageBack _textback = JsonUtility.FromJson<MessageBack>(_msgBack);

                if (_textback != null && _textback.choices.Count > 0)
                {
                    string _backMsg = _textback.choices[0].message.content;
                    // 添加对话记录
                    m_DataList.Add(new SendData("assistant", _backMsg));
                    _callback(_backMsg);
                }
            }
            else
            {
                Debug.LogError("请求失败: " + request.downloadHandler.text);
            }

            stopwatch.Stop();
            Debug.Log("SiliconFlow 耗时：" + stopwatch.Elapsed.TotalSeconds);
        }
    }

    #region 数据包

    [Serializable]
    public class PostData
    {
        public string model;
        public List<SendData> messages;
        public bool stream;
        public int max_tokens;
        public object stop;
        public float temperature;
        public float top_p;
        public int top_k;
        public float frequency_penalty;
        public int n;
        public ResponseFormat response_format;
    }

    [Serializable]
    public class ResponseFormat
    {
        public string type;
    }

    [Serializable]
    public class MessageBack
    {
        public string id;
        public string model;
        public List<MessageBody> choices;
    }

    [Serializable]
    public class MessageBody
    {
        public Message message;
        public string finish_reason;
    }

    [Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    #endregion
}
