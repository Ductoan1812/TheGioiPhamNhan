using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class PlayerUI : MonoBehaviour
{
    [Header("Refs")] [SerializeField] private PlayerStats stats;
    [SerializeField] private Image hpFill;
    [SerializeField] private Image manaFill;
    [SerializeField] private Image expFill;
    [SerializeField] private TMP_Text txtHp;
    [SerializeField] private TMP_Text txtMana;
    [SerializeField] private TMP_Text txtLevel;
    [SerializeField] private TMP_Text txtExpPercent;

    [Header("Định dạng")] [SerializeField] private string hpFormat = "{0}";
    [SerializeField] private string manaFormat = "{0}";
    [SerializeField] private string levelFormat = "Lv {0}";
    [SerializeField] private string expPercentFormat = "{0:P0}"; // % không chữ

    private void Awake()
    {
    if (!stats) stats = FindFirstObjectByType<PlayerStats>();
    }

    private void OnEnable()
    {
        if (stats)
        {
            stats.onDamaged.AddListener(Refresh);
            stats.onHealed.AddListener(Refresh);
            stats.onDeath.AddListener(Refresh);
            stats.onManaChanged.AddListener(Refresh);
            stats.onExpChanged.AddListener(Refresh);
            stats.onLevelUp.AddListener(Refresh);
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (stats)
        {
            stats.onDamaged.RemoveListener(Refresh);
            stats.onHealed.RemoveListener(Refresh);
            stats.onDeath.RemoveListener(Refresh);
            stats.onManaChanged.RemoveListener(Refresh);
            stats.onExpChanged.RemoveListener(Refresh);
            stats.onLevelUp.RemoveListener(Refresh);
        }
    }

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (!stats) return;
        if (hpFill)
        {
            hpFill.fillAmount = stats.MaxHealth > 0 ? (float)stats.CurrentHealth / stats.MaxHealth : 0f;
        }
        if (manaFill)
        {
            manaFill.fillAmount = stats.MaxMana > 0 ? (float)stats.CurrentMana / stats.MaxMana : 0f;
        }
        if (expFill)
        {
            expFill.fillAmount = stats.ExpPercent01;
        }
        if (txtHp)
        {
            txtHp.text = string.Format(hpFormat, stats.CurrentHealth, stats.MaxHealth);
        }
        if (txtMana)
        {
            txtMana.text = string.Format(manaFormat, stats.CurrentMana, stats.MaxMana);
        }
        if (txtLevel)
        {
            txtLevel.text = string.Format(levelFormat, stats.Level);
        }
        if (txtExpPercent)
        {
            txtExpPercent.text = string.Format(expPercentFormat, stats.ExpPercent01);
        }
    }
}
