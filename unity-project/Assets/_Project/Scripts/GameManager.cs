using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏主管理器 — 协调各模块，处理 WebGL 与前端 JavaScript 通信
/// 暴露接口供答题系统获取实验数据
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("核心组件")]
    public SunController sunController;
    public ShadowMeasurement shadowMeasurement;
    public TimeSliderUI timeSliderUI;
    public SeasonToggle seasonToggle;

    [Header("快捷跳转按钮")]
    public Button btnMorning;    // 早上 8:00
    public Button btnNoon;       // 正午 12:00
    public Button btnAfternoon;  // 下午 14:00
    public Button btnEvening;    // 傍晚 16:00
    public Button btnAutoPlay;   // 自动演示

    [Header("状态文本")]
    public Text statusText;

    private bool _autoPlaying = false;

    void Start()
    {
        // 自动查找组件
        if (sunController == null) sunController = FindObjectOfType<SunController>();
        if (shadowMeasurement == null) shadowMeasurement = FindObjectOfType<ShadowMeasurement>();
        if (timeSliderUI == null) timeSliderUI = FindObjectOfType<TimeSliderUI>();
        if (seasonToggle == null) seasonToggle = FindObjectOfType<SeasonToggle>();

        // 绑定快捷按钮
        if (btnMorning != null) btnMorning.onClick.AddListener(() => JumpToTime(1f / 6f));
        if (btnNoon != null) btnNoon.onClick.AddListener(() => JumpToTime(0.5f));
        if (btnAfternoon != null) btnAfternoon.onClick.AddListener(() => JumpToTime(4f / 6f));
        if (btnEvening != null) btnEvening.onClick.AddListener(() => JumpToTime(5f / 6f));
        if (btnAutoPlay != null) btnAutoPlay.onClick.AddListener(ToggleAutoPlay);

        // 初始：正午
        JumpToTime(0.5f);
    }

    void Update()
    {
        // 更新状态文本
        if (statusText != null && sunController != null && shadowMeasurement != null)
        {
            statusText.text = $"🌞 {sunController.GetTimeString()}  |  "
                            + $"影子: {shadowMeasurement.GetShadowLength():F1}米  |  "
                            + $"季节: {seasonToggle?.GetCurrentSeasonName() ?? "夏"}";
        }
    }

    /// <summary>
    /// 跳转时间点
    /// </summary>
    public void JumpToTime(float t)
    {
        if (sunController != null)
            sunController.UpdateSunPosition(t);

        if (timeSliderUI != null)
            timeSliderUI.SetTime(t);
    }

    void ToggleAutoPlay()
    {
        _autoPlaying = !_autoPlaying;
        if (sunController != null)
            sunController.autoPlay = _autoPlaying;

        if (btnAutoPlay != null)
        {
            var txt = btnAutoPlay.GetComponentInChildren<Text>();
            if (txt != null) txt.text = _autoPlaying ? "⏸ 暂停" : "▶ 自动演示";
        }
    }

    // ─── WebGL 与前端 JS 通信接口 ───

    /// <summary>
    /// 通过 Unity 的 jslib 将数据发送给前端
    /// 供前端答题系统读取实验数据
    /// </summary>
    public string GetExperimentDataJSON()
    {
        float shadowLen = shadowMeasurement != null ? shadowMeasurement.GetShadowLength() : 0f;
        float altitude = sunController != null ? sunController.GetCurrentAltitude() : 0f;
        string timeStr = sunController != null ? sunController.GetTimeString() : "12:00";
        string season = seasonToggle != null ? seasonToggle.GetCurrentSeasonName() : "夏";

        return $"{{\"time\":\"{timeStr}\",\"shadowLength\":{shadowLen:F2},\"sunAltitude\":{altitude:F1},\"season\":\"{season}\"}}";
    }

    /// <summary>
    /// 从前端接收指令（如跳转到特定时间）
    /// </summary>
    public void ReceiveCommand(string command)
    {
        switch (command)
        {
            case "morning": JumpToTime(1f / 6f); break;
            case "noon": JumpToTime(0.5f); break;
            case "afternoon": JumpToTime(4f / 6f); break;
            case "evening": JumpToTime(5f / 6f); break;
            case "spring": seasonToggle?.SwitchSeason(0); break;
            case "summer": seasonToggle?.SwitchSeason(1); break;
            case "autumn": seasonToggle?.SwitchSeason(2); break;
            case "winter": seasonToggle?.SwitchSeason(3); break;
            default: break;
        }
    }
}
