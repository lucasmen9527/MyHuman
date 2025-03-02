using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class chatGPT : LLM
{
    public chatGPT() {
        url = "https://api.openai.com/v1/completions";
    }

    /// <summary>
    /// api key
    /// </summary>
    [SerializeField] private string api_key;
    //配置参数
    [SerializeField] private PostData m_PostDataSetting;
    /// <summary>
    /// 发送消息
    /// </summary>
    /// <returns></returns>
    public override void PostMsg(string _msg,Action<string> _callback)
    {
        //提示词
        string message = "当前为角色的人物设定：" + m_Prompt +
            " 回答的语言：" + lan +
            " 接下来是我的提问：" + _msg;


        StartCoroutine(Request(message, _callback));
    }

    /// <summary>
    /// 调用接口返回数据
    /// </summary>
    /// <param name="_postWord"></param>
    /// <param name="_callback"></param>
    /// <returns></returns>
    private IEnumerator Request(string _postWord, System.Action<string> _callback)
    {
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            PostData _postData = new PostData
            {
                model = m_PostDataSetting.model,
                prompt = _postWord,
                max_tokens = m_PostDataSetting.max_tokens,
                temperature = m_PostDataSetting.temperature,
                top_p = m_PostDataSetting.top_p,
                frequency_penalty = m_PostDataSetting.frequency_penalty,
                presence_penalty = m_PostDataSetting.presence_penalty,
                stop = m_PostDataSetting.stop
            };

            string _jsonText = JsonUtility.ToJson(_postData);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(_jsonText);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", string.Format("Bearer {0}", api_key));

            yield return request.SendWebRequest();

            if (request.responseCode == 200)
            {
                string _msg = request.downloadHandler.text;
                TextCallback _textback = JsonUtility.FromJson<TextCallback>(_msg);
                if (_textback != null && _textback.choices.Count > 0)
                {

                    string _backMsg = Regex.Replace(_textback.choices[0].text, @"[\r\n]", "").Replace("？", "");
                    _callback(_backMsg);
                }

            }
        }


    }

    #region 数据定义

    [System.Serializable]
    public class PostData
    {
        public string model;
        public string prompt;
        public int max_tokens=1024;
        public float temperature=0.9f;
        public int top_p;
        public float frequency_penalty;
        public float presence_penalty;
        public string stop;
    }
    /// <summary>
    /// 返回的信息
    /// </summary>
    [System.Serializable]
    public class TextCallback
    {
        public string id;
        public string created;
        public string model;
        public List<TextSample> choices;

        [System.Serializable]
        public class TextSample
        {
            public string text;
            public string index;
            public string finish_reason;
        }

    }

    #endregion
}
