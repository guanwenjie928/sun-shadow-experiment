using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 季节切换 — 春夏秋冬改变太阳轨迹高度
/// 配合课程中"同一时刻，不同季节影子不同"知识点
/// </summary>
public class SeasonToggle : MonoBehaviour
{
    [System.Serializable]
    public struct SeasonConfig
    {
        public string seasonName;
        public float maxAltitude;  // 该季节正午太阳最大高度角
        public Color labelColor;
        public string emoji;
    }

    [Header("季节配置（适合北半球中纬度）")]
    public SeasonConfig[] seasons = new SeasonConfig[]
    {
        new SeasonConfig { seasonName = "春", maxAltitude = 55f, labelColor = new Color(0.3f, 0.8f, 0.3f), emoji = "🌸" },
        new SeasonConfig { seasonName = "夏", maxAltitude = 76f, labelColor = new Color(1f, 0.6f, 0.2f), emoji = "☀️"  },
        new SeasonConfig { seasonName = "秋", maxAltitude = 50f, labelColor = new Color(1f, 0.7f, 0.3f), emoji = "🍂" },
        new SeasonConfig { seasonName = "冬", maxAltitude = 35f, labelColor = new Color(0.6f, 0.8f, 1f), emoji = "❄️"  },
    };

    [Header("UI 按钮")]
    public Button[] seasonButtons;
    public Text seasonLabel;

    private SunController _sun;
    private int _currentSeason = 1; // 默认夏季

    void Start()
    {
        _sun = FindObjectOfType<SunController>();

        // 自动绑定按钮
        if (seasonButtons != null && seasonButtons.Length == 4)
        {
            for (int i = 0; i < 4; i++)
            {
                int idx = i; // 闭包捕获
                seasonButtons[i].onClick.AddListener(() => SwitchSeason(idx));
            }
        }

        // 默认夏季
        SwitchSeason(1);
    }

    public void SwitchSeason(int index)
    {
        if (index < 0 || index >= seasons.Length) return;
        if (_sun == null) return;

        _currentSeason = index;
        SeasonConfig cfg = seasons[index];

        _sun.maxAltitude = cfg.maxAltitude;
        _sun.UpdateSunPosition(_sun.timeOfDay);

        if (seasonLabel != null)
        {
            seasonLabel.text = $"{cfg.emoji} {cfg.seasonName}季";
            seasonLabel.color = cfg.labelColor;
        }

        // 高亮当前按钮
        if (seasonButtons != null)
        {
            for (int i = 0; i < seasonButtons.Length; i++)
            {
                var colors = seasonButtons[i].colors;
                colors.normalColor = (i == index) ? cfg.labelColor * 0.5f : Color.white;
                seasonButtons[i].colors = colors;
            }
        }
    }

    public int GetCurrentSeason() => _currentSeason;
    public string GetCurrentSeasonName() => seasons[_currentSeason].seasonName;
}
