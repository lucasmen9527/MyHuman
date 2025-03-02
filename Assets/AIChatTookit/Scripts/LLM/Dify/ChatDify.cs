using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ChatDify : MonoBehaviour
{
    [SerializeField] private string api_key = "YOUR-API-KEY";  // Dify API Key
    private string url = "https://api.dify.ai/v1/chat-messages"; // Dify API 端点

    /// <summary>
    /// 发送消息到 Dify
    /// </summary>
    public void PostMsg(string userInput, Action<string> callback)
    {
        StartCoroutine(Request(userInput, callback));
    }

    /// <summary>
    /// 发送请求到 Dify API
    /// </summary>
    private IEnumerator Request(string userInput, Action<string> callback)
    {
        // 构建请求数据
        PostData postData = new PostData
        {
            inputs = new Dictionary<string, string>(),
            query = userInput,
            response_mode = "streaming",
            conversation_id = "", // 你可以动态生成
            user = "unity_user"
        };

        string jsonText = JsonUtility.ToJson(postData);
        byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonText);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(data);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {api_key}");
            request.method = UnityWebRequest.kHttpVerbPOST;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                MessageBack response = JsonUtility.FromJson<MessageBack>(responseText);
                
                if (response != null && response.answer != null)
                {
                    callback(response.answer);
                }
                else
                {
                    callback("AI 没有返回有效消息");
                }
            }
            else
            {
                Debug.LogError("请求失败: " + request.error);
                callback("请求失败: " + request.error);
            }
        }
    }

    #region 数据结构
    [Serializable]
    private class PostData
    {
        public Dictionary<string, string> inputs;
        public string query;
        public string response_mode;
        public string conversation_id;
        public string user;
    }

    [Serializable]
    private class MessageBack
    {
        public string answer;
    }
    #endregion
}
