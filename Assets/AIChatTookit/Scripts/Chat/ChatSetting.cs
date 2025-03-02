using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChatSetting
{
    /// <summary>
    /// 聊天模型
    /// </summary>
    [Header("根据需要挂载不同的llm脚本")]
    [SerializeField] public LLM m_ChatModel;
    /// <summary>
    /// 语音合成服务
    /// </summary>
    [Header("语音合成脚本")]
    public TTS m_TextToSpeech;
    /// <summary>
    /// 语音识别服务
    /// </summary>
    [Header("语音识别脚本")]
    public STT m_SpeechToText;
}
