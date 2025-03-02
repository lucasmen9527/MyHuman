using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class TTS : MonoBehaviour
{
    /// <summary>
    /// 语音合成的api地址
    /// </summary>
    [SerializeField] protected string m_PostURL = string.Empty;
    /// <summary>
    /// 计算方法调用的时间
    /// </summary>
    [SerializeField] protected Stopwatch stopwatch = new Stopwatch();
    /// <summary>
    /// 语音合成，返回音频
    /// </summary>
    /// <param name="_msg"></param>
    /// <param name="_callback"></param>
    public virtual void Speak(string _msg,Action<AudioClip> _callback) {}
    /// <summary>
    /// 合成语音返回音频，同时返回合成的文本
    /// </summary>
    /// <param name="_msg"></param>
    /// <param name="_callback"></param>
    public virtual void Speak(string _msg, Action<AudioClip,string> _callback) { }


}
