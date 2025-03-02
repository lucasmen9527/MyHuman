using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Audio2LipScript : MonoBehaviour
{
    [Tooltip("Which lip sync provider to use for viseme computation.")]
    public OVRLipSync.ContextProviders provider = OVRLipSync.ContextProviders.Enhanced;
    [Tooltip("Enable DSP offload on supported Android devices.")]
    public bool enableAcceleration = true;
    [SerializeField] private uint Context = 0;
    [SerializeField] public float gain = 1.0f;
    /// <summary>
    /// 音频
    /// </summary>
    [SerializeField] private AudioSource m_AudioSource;
    /// <summary>
    /// 模型的SkinnedMeshRenderer组件
    /// </summary>
    [Header("设置带有口型的MeshRender")]
    public SkinnedMeshRenderer meshRenderer;
    /// <summary>
    /// blendshape权重倍数
    /// </summary>
    public float blendWeightMultiplier = 100f;
    /// <summary>
    /// 设置每个口型对应的blendershape的索引
    /// </summary>
    [Header("设置元音对应的blendershape的索引值")]
    public VisemeBlenderShapeIndexMap m_VisemeIndex;

    /// <summary>
    /// 音素分析结果
    /// </summary>
    private OVRLipSync.Frame frame = new OVRLipSync.Frame();
    protected OVRLipSync.Frame Frame
    {
        get
        {
            return frame;
        }
    }

    private void Awake()
    {
        m_AudioSource=this.GetComponent<AudioSource>();
        if (Context == 0)
        {
            if (OVRLipSync.CreateContext(ref Context, provider, 0, enableAcceleration)
                != OVRLipSync.Result.Success)
            {
                Debug.LogError("OVRLipSyncContextBase.Start ERROR: Could not create" +
                    " Phoneme context.");
                return;
            }
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        ProcessAudioSamplesRaw(data, channels);
    }

    /// <summary>
    /// Pass F32 PCM audio buffer to the lip sync module
    /// </summary>
    /// <param name="data">Data.</param>
    /// <param name="channels">Channels.</param>
    public void ProcessAudioSamplesRaw(float[] data, int channels)
    {
        // Send data into Phoneme context for processing (if context is not 0)
        lock (this)
        {
            if (OVRLipSync.IsInitialized() != OVRLipSync.Result.Success)
            {
                return;
            }
            var frame = this.Frame;
            OVRLipSync.ProcessFrame(Context, data, frame, channels == 2);
        }
    }

    private void Update()
    {
        if (this.Frame != null)
        {
            SetBlenderShapes();
        }
    }


    private void SetBlenderShapes()
    {
        for (int i = 0; i < this.Frame.Visemes.Length; i++)
        {
            string _name = ((OVRLipSync.Viseme)i).ToString();
            int blendShapeIndex = GetBlenderShapeIndexByName(_name);
            int blendWeight = (int)(blendWeightMultiplier * this.Frame.Visemes[i]);
            if (blendShapeIndex == 999)
                continue;

            meshRenderer.SetBlendShapeWeight(blendShapeIndex, blendWeight);
        }
    }

    /// <summary>
    /// 简单判断下，返回a i u e o 的索引
    /// </summary>
    /// <param name="_name"></param>
    /// <returns></returns>
    private int GetBlenderShapeIndexByName(string _name)
    {
        if (_name == "sil")
        {
            return 999;
        }
        if (_name == "aa")
        {
            return m_VisemeIndex.A;
        }
        if (_name == "ih")
        {
            return m_VisemeIndex.I;
        }
        if (_name == "E")
        {
            return m_VisemeIndex.E;
        }
        if (_name == "oh")
        {
            return m_VisemeIndex.O;
        }

        return m_VisemeIndex.U;
    }

    [System.Serializable]
    public class VisemeBlenderShapeIndexMap
    {
        public int A;
        public int I;
        public int U;
        public int E;
        public int O;

    }
}
