using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 时间滑块 UI — 拖动滑块控制太阳在天空中的位置
/// 支持触摸（平板）和鼠标操作
/// </summary>
public class TimeSliderUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI 组件")]
    public Slider timeSlider;
    public Text timeLabel;
    public Text shadowHintText; // 显示提示信息如"拖动滑块，观察影子变化"

    [Header("标记")]
    [Tooltip("在滑块上标记关键时间点")]
    public Transform[] timeMarkers; // 6:00, 9:00, 12:00, 15:00, 18:00

    [Header("日出日落图标")]
    public Image sunriseIcon;
    public Image sunsetIcon;

    private SunController _sun;
    private ShadowMeasurement _shadow;
    private bool _isDragging = false;

    // 记录交互数据（供外部读取）
    [HideInInspector] public int interactionCount = 0;
    [HideInInspector] public float totalDragTime = 0f;

    void Start()
    {
        _sun = FindObjectOfType<SunController>();
        _shadow = FindObjectOfType<ShadowMeasurement>();

        if (timeSlider != null)
        {
            timeSlider.onValueChanged.AddListener(OnSliderChanged);
            // 初始位置：正午 12:00
            timeSlider.value = 0.5f;
        }

        if (shadowHintText != null)
        {
            shadowHintText.text = "👆 拖动下方滑块，观察影子变化";
        }
    }

    void Update()
    {
        if (_isDragging)
        {
            totalDragTime += Time.deltaTime;
        }
    }

    void OnSliderChanged(float value)
    {
        if (_sun != null)
        {
            _sun.UpdateSunPosition(value);
        }

        if (timeLabel != null)
        {
            timeLabel.text = SliderToTimeString(value);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = true;
        interactionCount++;

        if (shadowHintText != null)
        {
            shadowHintText.text = "🔍 正在观察...";
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;

        if (_shadow != null && shadowHintText != null)
        {
            shadowHintText.text = $"当前影子长度：{_shadow.GetShadowLength():F1}米";
        }
    }

    /// <summary>
    /// 获取当前滑块对应的时间字符串
    /// </summary>
    string SliderToTimeString(float t)
    {
        float hours = 6f + t * 12f;
        int h = Mathf.FloorToInt(hours);
        int m = Mathf.FloorToInt((hours - h) * 60f);
        return $"{h:D2}:{m:D2}";
    }

    /// <summary>
    /// 快速设置时间（供外部调用）
    /// </summary>
    public void SetTime(float t)
    {
        if (timeSlider != null)
        {
            timeSlider.value = Mathf.Clamp01(t);
        }
    }

    /// <summary>
    /// 跳转到关键时间点
    /// </summary>
    public void JumpToMorning() { SetTime(1f / 6f); }     // 8:00
    public void JumpToNoon() { SetTime(0.5f); }            // 12:00
    public void JumpToAfternoon() { SetTime(4f / 6f); }    // 14:00
    public void JumpToEvening() { SetTime(5f / 6f); }       // 16:00
}
