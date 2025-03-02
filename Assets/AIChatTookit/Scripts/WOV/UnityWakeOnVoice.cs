using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_STANDALONE_WIN
using UnityEngine.Windows.Speech;
#endif
/// <summary>
/// unity内置语音唤醒 windows有效
/// </summary>
public class UnityWakeOnVoice : WOV
{
    /// <summary>
    /// 关键词
    /// </summary>
    [SerializeField]
    private string[] m_Keywords = { "玲玲" };//关键字
    /// <summary>
    /// 关键字识别器
    /// </summary>
#if UNITY_STANDALONE_WIN
    private KeywordRecognizer m_Recognizer;
    // Use this for initialization
    void Start()
    {
        //创建一个关键字识别器
        m_Recognizer = new KeywordRecognizer(m_Keywords);
        Debug.Log("创建识别器成功");
        m_Recognizer.OnPhraseRecognized += OnPhraseRecognized;

    }
    
    /// <summary>
    /// 开始识别
    /// </summary>
    public override void StartRecognizer()
    {
        if (m_Recognizer == null)
            return;

        m_Recognizer.Start();
    }
    /// <summary>
    /// 结束识别
    /// </summary>
    public override void StopRecognizer()
    {
        if (m_Recognizer == null)
            return;

        m_Recognizer.Stop();
    }

    /// <summary>
    /// 识别关键词回调
    /// </summary>
    /// <param name="args"></param>
    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendFormat("{0}", args.text);
        string _keyWord = builder.ToString();
        Debug.Log("识别器捕捉到关键词："+_keyWord);
        OnAwakeOnVoice(_keyWord);
    }
    #endif
}
