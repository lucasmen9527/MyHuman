using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class KnowledgeBaidu : LLM
{

    #region 定义变量
    [SerializeField] private string m_ApiKey = string.Empty;//密钥
    [SerializeField] private string m_ConversationID = string.Empty;//对话ID
    #endregion

    private void Awake()
    {
        url = "https://appbuilder.baidu.com/rpc/2.0/cloud_hub/v1/ai_engine/agi_platform/v1/instance/integrated";
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <returns></returns>
    public override void PostMsg(string _msg, Action<string> _callback)
    {
        //缓存发送的信息列表
        m_DataList.Add(new SendData("user", _msg));
        StartCoroutine(Request(_msg, _callback));
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
            query = _postWord,
            conversation_id = m_ConversationID
        });

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("X-Appbuilder-Authorization", string.Format("Bearer {0}", m_ApiKey));

            yield return request.SendWebRequest();

            if (request.responseCode == 200)
            {
                string _msg = request.downloadHandler.text;
                ResponseData response = JsonConvert.DeserializeObject<ResponseData>(_msg);

                if (response.code == 0)
                {
                    string _msgBack = response.result.answer;
                    m_ConversationID = response.result.conversation_id;
                    //添加记录
                    m_DataList.Add(new SendData("assistant", _msgBack));
                    //回调
                    _callback(_msgBack);
                }
                else
                {
                    LogError(response.code);
                }
            }
            else
            {
                Debug.Log(request.error);
            }

        }

        stopwatch.Stop();
        Debug.Log("文心知识库回复耗时：" + stopwatch.Elapsed.TotalSeconds);
    }

    /// <summary>
    /// 打印错误信息
    /// </summary>
    /// <param name="_code"></param>
    private void LogError(int _code)
    {
        if (_code == 400)
        {
            Debug.Log("请求参数错误");
            return;
        }
        if (_code == 401)
        {
            Debug.Log("权限错误");
            return;
        }
        if (_code == 404)
        {
            Debug.Log("账户、应用、模型、模版等无法找到");
            return;
        }
        if (_code == 500)
        {
            Debug.Log("服务器内部错误");
            return;
        }
        if (_code == 1001)
        {
            Debug.Log("调用超限，免费额度不足");
            return;
        }
        if (_code == 1004)
        {
            Debug.Log("模型服务报错");
            return;
        }
        if (_code == 1005)
        {
            Debug.Log("模版参数校验错误");
            return;
        }
        if (_code == 1006)
        {
            Debug.Log("免费额度已过期");
            return;
        }
        if (_code == 1007)
        {
            Debug.Log("千帆服务无法访问");
            return;
        }
        if (_code == 1008)
        {
            Debug.Log("千帆服务访问失败，一般是权限错误");
            return;
        }
    }


    #region 数据定义
    /// <summary>
    /// 发送数据
    /// </summary>
    [Serializable]public class RequestData
    {
        [SerializeField] public string query=string.Empty;//提问内容
        [SerializeField] public string response_mode = "blocking";//响应模式，支持以下两种：1. streaming：流式响应，使用SSE协议 2. blocking：阻塞响应
        [SerializeField] public string conversation_id=string.Empty;//对话ID，仅对话型应用生效。在对话型应用中：1. 空：表示表新建会话 2. 非空：表示在对应的会话中继续进行对话，服务内部维护对话历史
    }

    /// <summary>
    /// 返回的数据
    /// </summary>
    [Serializable]
    public class ResponseData
    {
        [SerializeField] public int code;//错误码。非0为错误，请参考错误码说明
        [SerializeField] public string message=string.Empty;//报错信息
        [SerializeField] public Result result = new Result();//回复信息
    }

    [Serializable]
    public class Result
    {
        [SerializeField] public string answer = string.Empty;//回复内容
        [SerializeField] public string conversation_id = string.Empty;//对话ID
    }

    #endregion


}
