using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class chatGLM : LLM
{
    public chatGLM()
    {
        url = "http://localhost:8000";
    }

 
    /// <summary>
    /// 历史对话
    /// </summary>
    [SerializeField] private List<List<string>> m_History = new List<List<string>>();
 
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
            prompt = _postWord,
            history = m_History
        });

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.responseCode == 200)
            {
                string _msg = request.downloadHandler.text;
                ResponseData response = JsonConvert.DeserializeObject<ResponseData>(_msg);

                //记录历史对话
                m_History = response.history;
                //添加记录
                m_DataList.Add(new SendData("assistant", _msg));
                //回调
                _callback(response.response);

            }

        }

        stopwatch.Stop();
        Debug.Log("chatGLM耗时：" + stopwatch.Elapsed.TotalSeconds);
    }

    #region 报文定义

    [Serializable]
    private class RequestData
    {
        public string prompt;
        public List<List<string>> history;
    }

    [Serializable]
    private class ResponseData
    {
        public string response;
        public List<List<string>> history;
        public int status;
        public string time;
    }
    #endregion


}
