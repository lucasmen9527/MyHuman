using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
// using UnityEngine.Networking.Types;

public class AppBuilderBaidu : LLM
{

	#region Params

	/// <summary>
	/// app id
	/// </summary>
	[SerializeField] private string app_id = string.Empty;
	/// <summary>
	/// api key
	/// </summary>
	[SerializeField] private string api_key = string.Empty;
	/// <summary>
	/// �½��ỰAPI��ַ
	/// </summary>
	private string m_ConversationUrl=string.Empty;
	/// <summary>
	/// �Ի�ID
	/// </summary>
	[SerializeField] private string m_ConversationID = string.Empty;

	#endregion

	#region Public Method
	/// <summary>
	/// ������Ϣ
	/// </summary>
	/// <returns></returns>
	public override void PostMsg(string _msg, Action<string> _callback)
	{
		//���淢�͵���Ϣ�б�
		m_DataList.Add(new SendData("user", _msg));
		StartCoroutine(Request(_msg, _callback));
	}


	/// <summary>
	/// ��������
	/// </summary> 
	/// <param name="_postWord"></param>
	/// <param name="_callback"></param>
	/// <returns></returns>
	public override IEnumerator Request(string _postWord, System.Action<string> _callback)
	{
		stopwatch.Restart();
		string jsonPayload = JsonConvert.SerializeObject(new RequestData
		{
			app_id= app_id,
			query = _postWord,
			conversation_id = m_ConversationID
		});

		using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
		{
			byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
			request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
			request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

			request.SetRequestHeader("Content-Type", "application/json");
			request.SetRequestHeader("X-Appbuilder-Authorization", string.Format("Bearer {0}", api_key));

			yield return request.SendWebRequest();

			if (request.responseCode == 200)
			{
				string _msg = request.downloadHandler.text;
				ResponseData response = JsonConvert.DeserializeObject<ResponseData>(_msg);

				if (response.code == string.Empty)
				{
					string _msgBack = response.answer;
					//���Ӽ�¼
					m_DataList.Add(new SendData("assistant", _msgBack));
					//�ص�
					_callback(_msgBack);
				}
				else
				{
					Debug.LogError(response.message);
				}
			}
			else
			{
				Debug.Log(request.error);
			}

		}

		stopwatch.Stop();
		Debug.Log("BaiduAppBuilder�ظ���ʱ��" + stopwatch.Elapsed.TotalSeconds);
	}

	#endregion



	#region Private Method

	void Awake()
	{
		OnInitial();
	}


	/// <summary>
	/// ��ʼ��
	/// </summary>
	private void OnInitial()
	{

		//�½��Ự��ַ
		m_ConversationUrl = "https://qianfan.baidubce.com/v2/app/conversation";

		//����api��ַ
		url = "https://qianfan.baidubce.com/v2/app/conversation/runs";

		//�½��Ự
		StartCoroutine(OnStartConversation());

	}

	/// <summary>
	/// �½��Ự
	/// </summary>
	/// <returns></returns>
	private IEnumerator OnStartConversation()
	{
		string jsonPayload = JsonUtility.ToJson(new CreateConversationData { app_id = app_id });
		using (UnityWebRequest request = new UnityWebRequest(m_ConversationUrl, "POST"))
		{
			byte[] data = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
			request.uploadHandler = (UploadHandler)new UploadHandlerRaw(data);
			request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

			request.SetRequestHeader("Content-Type", "application/json");
			request.SetRequestHeader("X-Appbuilder-Authorization", string.Format("Bearer {0}", api_key));

			yield return request.SendWebRequest();

			if (request.responseCode == 200)
			{
				string _msg = request.downloadHandler.text;
				ConversationCreateReponse response = JsonConvert.DeserializeObject<ConversationCreateReponse>(_msg);

				if (response.code == string.Empty)
				{
					//��ȡ���ỰID
					m_ConversationID = response.conversation_id;
				}
				else
				{
					Debug.LogError(response.message);
				}
			}
			else
			{
				Debug.Log(request.error);
			}

		}



	}



	#endregion


	#region Data Define

	/// <summary>
	/// �½��Ự
	/// </summary>
	[Serializable]
	public class CreateConversationData
	{
		public string app_id=string.Empty;
	}
	[Serializable]
	public class ConversationCreateReponse
	{
		public string request_id = string.Empty;
		public string conversation_id = string.Empty;
		public string code = string.Empty;
		public string message = string.Empty;
	}


	/// <summary>
	/// ��������
	/// </summary>
	[Serializable]
	public class RequestData
	{
		public string app_id = string.Empty;//appID
		public string query = string.Empty;//��������
		public bool stream = false;//�Ƿ���ʽ�ش�-������������ʽ
		public string conversation_id = string.Empty;//�Ի�ID
		public List<string> file_ids=new List<string>();//����ڶԻ����ϴ����ļ������Խ��ļ�id������ֶΣ�Ŀǰֻ������һ���ļ�
	}


	[Serializable]
	public class ResponseData
	{
		public string code = string.Empty;//������
		public string message = string.Empty;//������Ϣ
		public string request_id = string.Empty;//request_id����׷�١�
		public string date = string.Empty;//��Ϣ����ʱ���ʱ��� UTCʱ���ʽ��
		public string answer = string.Empty;//���ִ𰸡� ��ʽ���������������ݡ�
		public string conversation_id = string.Empty;//�Ի�ID
		public string message_id = string.Empty;//��Ϣid, ��ʽ�����¶������message_id����һ�¡�
		//public bool is_completion = false;//��ʽ��Ϣ���ͻش����Ƿ���ᡣ
		//content �ݲ����壬��Ҫ�Ļ��Լ���չ


	}

	#endregion


}
