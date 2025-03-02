using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ChatOllama : LLM
{
    /// <summary>
    /// AI设定
    /// </summary>
    public string m_SystemSetting = string.Empty;
    /// <summary>
    /// 设置模型,模型类型自行添加
    /// </summary>
    public ModelType m_GptModel = ModelType.llama3;

    private void Start()
    {
        //运行时，添加AI设定
        m_DataList.Add(new SendData("system", m_SystemSetting));
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
    /// 调用接口
    /// </summary>
    /// <param name="_postWord"></param>
    /// <param name="_callback"></param>
    /// <returns></returns>
    public override IEnumerator Request(string _postWord, System.Action<string> _callback)
    {
        stopwatch.Restart();
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            PostData _postData = new PostData
            {
                model = m_GptModel.ToString(),
                messages = m_DataList
            };

            string _jsonText = JsonUtility.ToJson(_postData);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(_jsonText);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            //request.SetRequestHeader("Authorization", string.Format("Bearer {0}", api_key));

            yield return request.SendWebRequest();

            if (request.responseCode == 200)
            {
                string _msgBack = request.downloadHandler.text;
                MessageBack _textback = JsonUtility.FromJson<MessageBack>(_msgBack);
                if (_textback != null && _textback.message!=null)
                {

                    string _backMsg = _textback.message.content;
                    //添加记录
                    m_DataList.Add(new SendData("assistant", _backMsg));
                    _callback(_backMsg);
                }
            }
            else
            {
                string _msgBack = request.downloadHandler.text;
                Debug.LogError(_msgBack);
            }

            stopwatch.Stop();
            Debug.Log("Ollama耗时：" + stopwatch.Elapsed.TotalSeconds);
        }
    }

    #region 数据定义

    public enum ModelType
    {
        llama3
    }

    [Serializable]
    public class PostData
    {
        public string model;
        public List<SendData> messages;
        public bool stream = false;//流式
    }
    [Serializable]
    public class MessageBack
    {
        public string created_at;
        public string model;
        public Message message;
    }
 
    [Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    #endregion

}
