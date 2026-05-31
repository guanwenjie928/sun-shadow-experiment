using UnityEngine;

/// <summary>
/// 程序化天空 — 根据时间动态渐变天空颜色
/// 配合 SunController 使用，无需外部天空盒素材
/// Tuanjie 1.9.0 / Unity 2022.3.62t8 · URP Compatible
/// </summary>
[ExecuteAlways]
public class ProceduralSky : MonoBehaviour
{
    [Header("引用")]
    public SunController sunController;
    public Material skyboxMaterial; // 可选：URP 天空盒材质

    [Header("天空颜色 — 日出/正午/日落")]
    public Color dawnColor = new Color(1f, 0.5f, 0.3f, 1f);     // 橙色
    public Color morningColor = new Color(0.7f, 0.85f, 1f, 1f);  // 淡蓝
    public Color noonColor = new Color(0.35f, 0.55f, 1f, 1f);    // 深蓝
    public Color afternoonColor = new Color(0.7f, 0.85f, 1f, 1f); // 淡蓝
    public Color duskColor = new Color(1f, 0.45f, 0.25f, 1f);    // 橙红

    [Header("地平线颜色")]
    public Color horizonDawn = new Color(1f, 0.8f, 0.5f, 1f);
    public Color horizonNoon = new Color(0.7f, 0.85f, 1f, 1f);
    public Color horizonDusk = new Color(1f, 0.75f, 0.4f, 1f);

    [Header("光照")]
    public Light sceneLight;

    [Header("雾")]
    public bool enableFog = true;
    public float fogDensityMax = 0.001f;

    // 采样天空颜色
    private Color _topColor;
    private Color _horizonColor;

    void Start()
    {
        if (sunController == null)
            sunController = FindObjectOfType<SunController>();
        if (sceneLight == null && sunController != null)
            sceneLight = sunController.GetComponent<Light>();

        // 确保场景有雾组件
        if (enableFog)
            RenderSettings.fog = true;
    }

    void Update()
    {
        if (sunController == null) return;

        float t = sunController.timeOfDay;
        UpdateSkyColors(t);
    }

    /// <summary>
    /// 根据时间参数 t 计算天空颜色
    /// t=0(6:00) → t=0.25(9:00) → t=0.5(12:00) → t=0.75(15:00) → t=1(18:00)
    /// </summary>
    void UpdateSkyColors(float t)
    {
        Color top, horizon;

        if (t < 0.15f)
        {
            // 日出阶段 (6:00 ~ 7:48)
            float lt = t / 0.15f;
            top = Color.Lerp(dawnColor, morningColor, lt);
            horizon = Color.Lerp(horizonDawn, horizonNoon, lt);
        }
        else if (t < 0.4f)
        {
            // 上午 (7:48 ~ 10:48)
            float lt = (t - 0.15f) / 0.25f;
            top = Color.Lerp(morningColor, noonColor, lt);
            horizon = Color.Lerp(horizonNoon, horizonNoon, lt);
        }
        else if (t < 0.6f)
        {
            // 正午 (10:48 ~ 13:12)
            top = noonColor;
            horizon = horizonNoon;
        }
        else if (t < 0.85f)
        {
            // 下午 (13:12 ~ 16:12)
            float lt = (t - 0.6f) / 0.25f;
            top = Color.Lerp(noonColor, afternoonColor, lt);
            horizon = Color.Lerp(horizonNoon, horizonNoon, lt);
        }
        else
        {
            // 日落阶段 (16:12 ~ 18:00)
            float lt = (t - 0.85f) / 0.15f;
            top = Color.Lerp(afternoonColor, duskColor, lt);
            horizon = Color.Lerp(horizonNoon, horizonDusk, lt);
        }

        _topColor = top;
        _horizonColor = horizon;

        // 设置相机背景色（Fallback：无天空盒时可见）
        Camera cam = Camera.main;
        if (cam != null && RenderSettings.skybox == null)
        {
            cam.backgroundColor = Color.Lerp(horizon, top, 0.3f);
        }

        // 雾色跟随天空
        if (enableFog)
        {
            RenderSettings.fogColor = horizon;
            RenderSettings.fogDensity = Mathf.Lerp(fogDensityMax, fogDensityMax * 0.3f,
                Mathf.Abs(t - 0.5f) * 2f
            );
        }

        // 环境光（间接光）跟随天空
        RenderSettings.ambientLight = Color.Lerp(horizon, top, 0.5f) * 0.5f;
        RenderSettings.ambientSkyColor = top;
        RenderSettings.ambientEquatorColor = horizon;
        RenderSettings.ambientGroundColor = horizon * 0.5f;
    }

    /// <summary>
    /// 对外接口：获取当前天空顶部颜色
    /// </summary>
    public Color GetTopColor() => _topColor;

    /// <summary>
    /// 对外接口：获取当前地平线颜色
    /// </summary>
    public Color GetHorizonColor() => _horizonColor;
}
