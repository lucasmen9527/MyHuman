using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
/// <summary>
/// 麦克风实时聊天
/// </summary>
public class RTSpeechHandler : MonoBehaviour
{
    /// <summary>
    /// 麦克风名称
    /// </summary>
    public string m_MicrophoneName = null;
    /// <summary>
    /// 音量大于这个值，就开始录制
    /// </summary>
    public float m_SilenceThreshold = 0.01f;
    /// <summary>
    /// 沉默限制时长
    /// </summary>
    [Header("设置几秒没声音，就停止录制")]
    public float m_RecordingTimeLimit = 2.0f;
    /// <summary>
    /// 对话状态保持时长
    /// </summary>
    [Header("设置对话状态保持时间")]
    public float m_LossAwakeTimeLimit = 10f;
    /// <summary>
    /// 锁定状态下，不记录静默时间
    /// </summary>
    [SerializeField]private bool m_LockState = false;
    /// <summary>
    /// 音频
    /// </summary>
    private AudioClip m_RecordedClip;
    /// <summary>
    /// 唤醒关键词
    /// </summary>
    [SerializeField]private string m_AwakeKeyWord=string.Empty;
    /// <summary>
    /// 唤醒状态
    /// </summary>
    [Header("标识当前是否处于唤醒状态")]
    [SerializeField]private bool m_AwakeState = false;
    /// <summary>
    /// 监听状态
    /// </summary>
    [SerializeField] private bool m_ListeningState = false;
    /// <summary>
    /// 录制状态
    /// </summary>
    [SerializeField] private bool m_IsRecording = false;
    /// <summary>
    /// 沉默计时器
    /// </summary>
    [SerializeField]private float m_SilenceTimer = 0.0f;

    /// <summary>
    /// 聊天脚本
    /// </summary>
    [SerializeField]private RTChatSample m_ChatSample;
    /// <summary>
    /// 语音唤醒
    /// </summary>
    [SerializeField] private WOV m_VoiceAWake;

    private void Awake()
    {
        OnInit();
    }

    private void OnInit()
    {
        //AI回复结束回调
        m_ChatSample.OnAISpeakDone += SpeachDoneCallBack;

        //绑定唤醒回调
        m_VoiceAWake.OnBindAwakeCallBack(AwakeCallBack);

    }

    private void Start()
    {

        if (m_MicrophoneName == null)
        {
            // 如果没有指定麦克风名称，则使用系统默认麦克风
            m_MicrophoneName = Microphone.devices[0];
        }

        // 确保麦克风准备好
        if (Microphone.IsRecording(m_MicrophoneName))
        {
            Microphone.End(m_MicrophoneName);
        }

        // 启动麦克风监听
        m_RecordedClip = Microphone.Start(m_MicrophoneName, false,30, 16000);

        while (Microphone.GetPosition(null) <= 0) { }

        // 启动录制状态检测协程
        StartCoroutine(DetectRecording());
    }

    /// <summary>
    /// 开始检测声音
    /// </summary>
    /// <returns></returns>
    private IEnumerator DetectRecording()
    {
        while (true)
        {
            float[] samples = new float[128]; // 选择合适的样本大小
            int position = Microphone.GetPosition(null);
            if (position < samples.Length)
            {
                yield return null;
                continue;
            }

           

            try { m_RecordedClip.GetData(samples, position - samples.Length); } catch { }

            float rms = 0.0f;
            foreach (float sample in samples)
            {
                rms += sample * sample;
            }

            rms = Mathf.Sqrt(rms / samples.Length);

            if (rms > m_SilenceThreshold)
            {
                m_SilenceTimer = 0.0f; // 重置静默计时器

                //启动关键词唤醒监听
                if (!m_AwakeState&&!m_ListeningState)
                {
                    StartVoiceListening();
                }
                //已唤醒，启动录制
                if (m_AwakeState&&!m_IsRecording)
                {
                    StartRecording();
                }

            }
            else
            {

                if (!m_LockState)
                {
                    m_SilenceTimer += Time.deltaTime;
                }
               
                //结束唤醒词监听
                if (m_ListeningState&&!m_AwakeState && m_SilenceTimer >= m_RecordingTimeLimit)
                {
                    StopVoiceListening();
                }

                //唤醒状态，结束说话
                if (m_AwakeState&&m_IsRecording && m_SilenceTimer >= m_RecordingTimeLimit)
                {
                    StopRecording();
                }

                //沉默时间过长，结束对话状态，j进入等待唤醒
                if (m_AwakeState && !m_IsRecording && m_SilenceTimer >= m_LossAwakeTimeLimit)
                {
                    m_AwakeState=false;
                    PrintLog("Loss->对话连接已丢失");
                }

            }

            yield return null;
  
        }
    }
    
    [SerializeField]private AudioSource m_Greeting;
    [SerializeField] private AudioClip m_GreatingVoice;
    /// <summary>
    /// 关键词监听回调
    /// </summary>
    /// <param name="_msg"></param>
    private void AwakeCallBack(string _msg)
    {
        if (_msg == m_AwakeKeyWord&&!m_AwakeState)
        {
            m_AwakeState = true;
            Debug.Log("识别到关键词：" + _msg);
            PrintLog("Link->已建立对话连接");
            if (m_Greeting)
            {
                m_Greeting.clip = m_GreatingVoice;
                m_Greeting.Play();
            }
        }
    }
    /// <summary>
    /// 开始唤醒监听
    /// </summary>
    private void StartVoiceListening()
    {
        m_ListeningState = true;
        m_VoiceAWake.StartRecognizer();
        PrintLog("开始->识别唤醒关键词");
 
    }

    /// <summary>
    /// 停止唤醒监听
    /// </summary>
    private void StopVoiceListening()
    {
        m_ListeningState = false;
        m_VoiceAWake.StopRecognizer();
        PrintLog("结束->唤醒关键词识别");
        //StartCoroutine(WaitAndStopListen());
    }
 
    private IEnumerator WaitAndStopListen()
    {
        yield return new WaitForSeconds(1);
        m_ListeningState = false;
    }

    /// <summary>
    /// 开始监听说话声音
    /// </summary>
    private void StartRecording()
    {
        m_SilenceTimer = 0.0f; // 重置静默计时器
        m_IsRecording = true;
        PrintLog("正在录制对话...");
        //停止监听，并重新开始录制，会导致唤醒的那一帧声音丢失
        Microphone.End(m_MicrophoneName);
        m_RecordedClip = Microphone.Start(m_MicrophoneName, false, 30, 16000);
    }
    /// <summary>
    /// 结束说话
    /// </summary>
    private void StopRecording()
    {
        m_IsRecording = false;

        PrintLog("会话录制结束...");

        // 停止麦克风监听
        Microphone.End(m_MicrophoneName);

        //处理音频数据
        SetRecordedAudio();


    }

    /// <summary>
    /// 开始录制监听
    /// </summary>
    public void ReStartRecord()
    {
        m_RecordedClip = Microphone.Start(m_MicrophoneName, true, 30, 16000);
  
        m_LockState = false;
    }


    private void SetRecordedAudio()
    {
        m_LockState = true;

        m_ChatSample.AcceptClip(m_RecordedClip);
        //AudioSource.clip = m_RecordedClip;
        //AudioSource.Play();
    }

    /// <summary>
    /// 对话结束回调，启动麦克风检测
    /// </summary>
    private void SpeachDoneCallBack()
    {
        ReStartRecord();
    }

    [SerializeField] private Text m_PrintText;
    /// <summary>
    /// 打印日志
    /// </summary>
    /// <param name="_log"></param>
    private void PrintLog(string _log)
    {
        m_PrintText.text = _log;
        //Debug.Log(_log);
    }

}
