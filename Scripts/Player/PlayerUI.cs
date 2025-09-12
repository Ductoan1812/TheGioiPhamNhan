using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Xianxia.PlayerDataSystem;

[DisallowMultipleComponent]
public class PlayerUI : MonoBehaviour
{
    [Header("Bars (Image fill or Slider)")]
    [SerializeField] private Image hpFill;
    [SerializeField] private Image manaFill;
    [SerializeField] private Image expFill;
    [Header("Optional Sliders (if you prefer sliders instead of images)")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider manaSlider;
    [SerializeField] private Slider expSlider;

    [Header("Texts")]
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text manaText;
    [SerializeField] private TMP_Text expPercentText;
    [SerializeField] private TMP_Text levelText;

    private PlayerStatsManager statsMgr;
    private PlayerData data;

    void Awake()
    {
        statsMgr = FindFirstObjectByType<PlayerStatsManager>();
        data = PlayerManager.Instance?.Data;
        if (statsMgr != null)
        {
            statsMgr.onStatsRecalculated.AddListener(RefreshImmediate);
        }
    }

    void OnDestroy()
    {
        if (statsMgr != null)
        {
            statsMgr.onStatsRecalculated.RemoveListener(RefreshImmediate);
        }
    }

    void Start()
    {
        RefreshImmediate();
    }

    void Update()
    {
        RefreshBarsOnly();
    }

    private void RefreshImmediate()
    {
        data = PlayerManager.Instance?.Data;
        RefreshBarsOnly();
        RefreshExp();
        RefreshLevel();
    }

    private void RefreshBarsOnly()
    {
        if (statsMgr == null) return;
        float hp = statsMgr.hp;
        float hpMax = statsMgr.hpMax;
        float qi = statsMgr.qi;
        float qiMax = statsMgr.qiMax;

        float hpRatio = hpMax > 0 ? hp / hpMax : 0f;
        float qiRatio = qiMax > 0 ? qi / qiMax : 0f;

        if (hpFill) hpFill.fillAmount = Mathf.Clamp01(hpRatio);
        if (manaFill) manaFill.fillAmount = Mathf.Clamp01(qiRatio);
        if (hpSlider)
        {
            hpSlider.maxValue = hpMax;
            hpSlider.value = hp;
        }
        if (manaSlider)
        {
            manaSlider.maxValue = qiMax;
            manaSlider.value = qi;
        }
        if (hpText) hpText.text = $"{Mathf.FloorToInt(hp)}/{Mathf.FloorToInt(hpMax)}";
        if (manaText) manaText.text = $"{Mathf.FloorToInt(qi)}/{Mathf.FloorToInt(qiMax)}";
    }

    private void RefreshExp()
    {
        if (data == null || data.stats == null) return;
        float need = data.stats.xpMax;
        if (need <= 0)
        {
            need = statsMgr != null ? statsMgr.ExpRequiredForNextLevel() : 100f;
            data.stats.xpMax = need;
        }
        float ratio = need > 0 ? data.stats.xp / need : 0f;
        if (expFill) expFill.fillAmount = Mathf.Clamp01(ratio);
        if (expSlider)
        {
            expSlider.maxValue = need;
            expSlider.value = data.stats.xp;
        }
        if (expPercentText)
        {
            expPercentText.text = string.Format("{0:0.#}%", Mathf.Clamp01(ratio) * 100f);
        }
    }

    private void RefreshLevel()
    {
        if (levelText == null) return;
        int level = statsMgr != null ? statsMgr.Level : (PlayerManager.Instance?.Data?.level ?? 1);
        levelText.text = $"Lv {level}";
    }
}
