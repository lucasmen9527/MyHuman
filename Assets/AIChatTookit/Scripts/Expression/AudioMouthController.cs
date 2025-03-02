using UnityEngine;
using System.Collections;

public class AudioMouthController : MonoBehaviour
{

    public SkinnedMeshRenderer meshRenderer; // 模型的SkinnedMeshRenderer组件
    public int blendShapeIndex; // blendshape索引
    public float blendWeightMultiplier = 100f; // blendshape权重倍增器
    public float smoothTime = 0.1f; // 平滑过渡时间

    [SerializeField]private AudioSource audioSource; // 音频源

    private float blendWeight; // blendshape权重
    private float blendWeightVelocity; // blendshape权重的速度

    void Update()
    {
        // 如果音频正在播放，则平滑设置blendshape的权重
        if (audioSource.isPlaying)
        {
            float amplitude = GetAmplitude();
            blendWeight = Mathf.SmoothDamp(blendWeight, amplitude * blendWeightMultiplier, ref blendWeightVelocity, smoothTime);
            meshRenderer.SetBlendShapeWeight(blendShapeIndex, blendWeight);
        }
        else
        {
            blendWeight = Mathf.SmoothDamp(blendWeight, 0f, ref blendWeightVelocity, smoothTime);
            meshRenderer.SetBlendShapeWeight(blendShapeIndex, blendWeight);
        }
    }

    // 获取音频振幅
    float GetAmplitude()
    {
        float[] samples = new float[512];
        audioSource.GetOutputData(samples, 0);
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += Mathf.Abs(samples[i]);
        }
        return sum / samples.Length;
    }
}
