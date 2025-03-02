using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class STT : MonoBehaviour
{

    /// <summary>
    /// 语音识别api地址
    /// </summary>
    [SerializeField] protected string m_SpeechRecognizeURL = String.Empty;
    /// <summary>
    /// 计算方法调用的时间
    /// </summary>
    [SerializeField] protected Stopwatch stopwatch = new Stopwatch();
    /// <summary>
    /// 语音识别
    /// </summary>
    /// <param name="_clip"></param>
    /// <param name="_callback"></param>
    public virtual void SpeechToText(AudioClip _clip,Action<string> _callback)
    {
       
    }

    /// <summary>
    /// 语音识别
    /// </summary>
    /// <param name="_audioData"></param>
    /// <param name="_callback"></param>
    public virtual void SpeechToText(byte[] _audioData, Action<string> _callback)
    {

    }


}
