using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 影子测量系统 — 基于太阳方向计算物体影子在地面的长度与方向
/// 支持实时显示影子长度数值
/// </summary>
public class ShadowMeasurement : MonoBehaviour
{
    [Header("物体参数")]
    [Tooltip("阻挡物高度（米）")]
    public float objectHeight = 5f;

    [Tooltip("阻挡物顶部参考点")]
    public Transform poleTop;

    [Tooltip("阻挡物底部参考点")]
    public Transform poleBottom;

    [Header("地面参数")]
    [Tooltip("地面平面位置 Y 坐标")]
    public float groundY = 0f;

    [Header("标尺")]
    [Tooltip("地面标尺 GameObjects（用于可视化影子方向与长度）")]
    public Transform rulerEastWest;

    [Tooltip("影子指示器（球/柱）")]
    public Transform shadowIndicator;

    [Header("UI 显示")]
    public Text shadowLengthText;
    public Text timeText;
    public Text altitudeText;

    [Header("测量单位")]
    public string unit = "米";

    private SunController _sun;

    // 影子信息
    [HideInInspector] public float shadowLength = 0f;
    [HideInInspector] public Vector3 shadowDirection;
    [HideInInspector] public Vector3 shadowEndPoint;

    void Start()
    {
        _sun = FindObjectOfType<SunController>();

        if (poleTop == null)
            poleTop = transform.Find("Top");
        if (poleBottom == null)
            poleBottom = transform;
        if (poleTop == null)
            poleTop = transform; // fallback
    }

    void Update()
    {
        if (_sun == null) return;

        CalculateShadow();
        UpdateVisuals();
        UpdateUI();
    }

    /// <summary>
    /// 核心计算：根据太阳方向算出影子在地面的落点与长度
    /// </summary>
    public void CalculateShadow()
    {
        Vector3 sunDir = _sun.GetSunDirection();

        // 计算顶部点（使用物体高度模拟）
        Vector3 top;
        if (poleTop != null)
            top = poleTop.position;
        else
            top = (poleBottom != null ? poleBottom.position : transform.position) + Vector3.up * objectHeight;

        Vector3 bottom = poleBottom != null ? poleBottom.position : transform.position;

        // 太阳反向射线与地面（Y=groundY）的交点
        // Parametric: P = top + t * sunDir (sunDir 从上往下)
        // 求 Y=groundY 时 t 的值
        if (Mathf.Abs(sunDir.y) < 0.001f)
        {
            // 太阳在正上方或正下方，影子无限长或没有影子
            shadowLength = 0f;
            shadowEndPoint = bottom;
            shadowDirection = Vector3.zero;
            return;
        }

        float t = (groundY - top.y) / sunDir.y;

        if (t < 0)
        {
            // 光线从下往上（不会发生），影子在物体下方
            shadowEndPoint = bottom;
            shadowLength = 0f;
            shadowDirection = Vector3.zero;
            return;
        }

        // 交点 = 影子端点
        shadowEndPoint = top + sunDir * t;

        // 影子方向（从物体底部指向影子端点）
        shadowDirection = (shadowEndPoint - bottom);
        shadowDirection.y = 0;
        shadowLength = shadowDirection.magnitude;

        // 钳制最大显示长度（避免太阳高度太低时影子太长）
        shadowLength = Mathf.Clamp(shadowLength, 0f, 50f);
    }

    void UpdateVisuals()
    {
        // 更新影子指示器位置
        if (shadowIndicator != null)
        {
            Vector3 bottom = poleBottom != null ? poleBottom.position : transform.position;
            // 指示器放在从底部指向影子端点的方向
            shadowIndicator.position = bottom + (shadowDirection.normalized * Mathf.Min(shadowLength, 20f));

            // 指示器始终在地面高度
            Vector3 pos = shadowIndicator.position;
            pos.y = groundY + 0.05f;
            shadowIndicator.position = pos;

            shadowIndicator.gameObject.SetActive(shadowLength > 0.01f);
        }

        // 更新标尺（可选高级功能：根据影子方向旋转标尺）
        if (rulerEastWest != null && shadowDirection.magnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(
                shadowDirection.normalized,
                Vector3.up
            );
            rulerEastWest.rotation = Quaternion.RotateTowards(
                rulerEastWest.rotation, targetRot, 360f * Time.deltaTime
            );
        }
    }

    void UpdateUI()
    {
        if (shadowLengthText != null)
            shadowLengthText.text = $"{shadowLength:F1}{unit}";

        if (timeText != null && _sun != null)
            timeText.text = _sun.GetTimeString();

        if (altitudeText != null && _sun != null)
            altitudeText.text = $"太阳高度: {_sun.GetCurrentAltitude():F0}°";
    }

    /// <summary>
    /// 对外接口：获取当前影子长度（米）
    /// </summary>
    public float GetShadowLength()
    {
        return shadowLength;
    }
}
