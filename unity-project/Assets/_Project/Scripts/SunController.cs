using UnityEngine;

/// <summary>
/// 太阳控制器 — 沿天空弧线运动，模拟一天中太阳的轨迹
/// Tuanjie 1.9.0 / Unity 2022.3.62t8
/// </summary>
public class SunController : MonoBehaviour
{
    [Header("时间参数")]
    [Range(0f, 1f)]
    [Tooltip("0 = 6:00 AM / 0.5 = 12:00 正午 / 1 = 18:00 PM")]
    public float timeOfDay = 0.5f;

    [Tooltip("是否自动运行太阳动画")]
    public bool autoPlay = false;

    [Tooltip("自动播放速度（完整一天需要的秒数）")]
    public float autoPlaySpeed = 30f;

    [Header("太阳轨迹")]
    [Tooltip("日出方位角（东 = 90°）")]
    public float sunriseAzimuth = 90f;

    [Tooltip("日落方位角（西 = 270°）")]
    public float sunsetAzimuth = 270f;

    [Tooltip("太阳轨迹弧线半径")]
    public float orbitRadius = 100f;

    [Tooltip("正午太阳最大高度角（度），夏季约 76°，冬季约 36°")]
    [Range(20f, 80f)]
    public float maxAltitude = 60f;

    [Header("光照强度")]
    public AnimationCurve intensityCurve = AnimationCurve.EaseInOut(0f, 0.3f, 1f, 0.3f);
    public float maxIntensity = 2f;

    [Header("颜色渐变")]
    public Gradient sunColorGradient;

    [Header("组件引用")]
    public Light sunLight;
    public Transform sunVisual; // 太阳的可视化球体（可选）

    private float _angle = 0f;

    void OnValidate()
    {
        if (sunLight == null)
            sunLight = GetComponent<Light>();

        if (sunLight == null)
            return;

        UpdateSunPosition(timeOfDay);
    }

    void Start()
    {
        if (sunLight == null)
            sunLight = GetComponent<Light>();

        // 默认强度曲线：日出/日落弱，正午强
        if (intensityCurve.keys.Length <= 1)
        {
            intensityCurve = new AnimationCurve(
                new Keyframe(0f, 0.2f),
                new Keyframe(0.25f, 0.8f),
                new Keyframe(0.5f, 1.2f),
                new Keyframe(0.75f, 0.8f),
                new Keyframe(1f, 0.2f)
            );
        }
    }

    void Update()
    {
        if (autoPlay)
        {
            timeOfDay += Time.deltaTime / autoPlaySpeed;
            if (timeOfDay > 1f)
                timeOfDay -= 1f;
            UpdateSunPosition(timeOfDay);
        }
    }

    /// <summary>
    /// 根据时间参数更新太阳位置
    /// </summary>
    /// <param name="t">0~1，映射 6:00~18:00</param>
    public void UpdateSunPosition(float t)
    {
        timeOfDay = Mathf.Clamp01(t);

        // 太阳方位角：从东(90°) → 南(180°) → 西(270°)
        float azimuth = Mathf.Lerp(sunriseAzimuth, sunsetAzimuth, t);

        // 太阳高度角：日出/日落低，正午最高（正弦曲线模拟）
        float altitude = Mathf.Sin(t * Mathf.PI) * maxAltitude;

        // 球坐标 → 世界坐标
        Vector3 sunDir = SphericalToCartesian(azimuth, altitude, orbitRadius);

        // 太阳位置在世界空间（场景中心上方）
        Vector3 sunPos = Vector3.zero + sunDir;

        // 设置 Directional Light 的旋转
        // 光照方向 = 从太阳位置指向场景原点
        transform.position = sunPos;
        transform.LookAt(Vector3.zero);

        // 光照强度
        if (sunLight != null)
        {
            sunLight.intensity = intensityCurve.Evaluate(t) * maxIntensity;
            sunLight.color = sunColorGradient.Evaluate(t);
        }

        // 可视化球体
        if (sunVisual != null)
        {
            sunVisual.position = sunPos;
        }
    }

    /// <summary>
    /// 球坐标 → 笛卡尔坐标
    /// </summary>
    Vector3 SphericalToCartesian(float azimuthDeg, float altitudeDeg, float radius)
    {
        float azimuthRad = azimuthDeg * Mathf.Deg2Rad;
        float altitudeRad = altitudeDeg * Mathf.Deg2Rad;

        float x = radius * Mathf.Cos(altitudeRad) * Mathf.Cos(azimuthRad);
        float y = radius * Mathf.Sin(altitudeRad);
        float z = radius * Mathf.Cos(altitudeRad) * Mathf.Sin(azimuthRad);

        return new Vector3(x, y, z);
    }

    /// <summary>
    /// 获取当前时间字符串（HH:mm）
    /// </summary>
    public string GetTimeString()
    {
        float hours = 6f + timeOfDay * 12f; // 6:00 ~ 18:00
        int h = Mathf.FloorToInt(hours);
        int m = Mathf.FloorToInt((hours - h) * 60f);
        return $"{h:D2}:{m:D2}";
    }

    /// <summary>
    /// 获取太阳方向向量（指向太阳的反方向 = 光照方向）
    /// </summary>
    public Vector3 GetSunDirection()
    {
        return -transform.forward;
    }

    /// <summary>
    /// 获取当前太阳高度角
    /// </summary>
    public float GetCurrentAltitude()
    {
        float t = Mathf.Clamp01(timeOfDay);
        return Mathf.Sin(t * Mathf.PI) * maxAltitude;
    }
}
