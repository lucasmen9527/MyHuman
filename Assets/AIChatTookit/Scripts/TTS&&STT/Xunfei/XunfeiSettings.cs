using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XunfeiSettings : MonoBehaviour
{
    #region 参数
    /// <summary>
    /// 讯飞的AppID
    /// </summary>
    [Header("填写app id")]
    [SerializeField] public string m_AppID = "讯飞的AppID";
    /// <summary>
    /// 讯飞的APIKey
    /// </summary>
    [Header("填写api key")]
    [SerializeField] public string m_APIKey = "讯飞的APIKey";
    /// <summary>
    /// 讯飞的APISecret
    /// </summary>
    [Header("填写secret key")]
    [SerializeField] public string m_APISecret = "讯飞的APISecret";

    #endregion
}
