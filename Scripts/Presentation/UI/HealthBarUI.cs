using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Presentation.UI
{
    /// <summary>
    /// Health bar UI component
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private GameObject healthBarContainer;

        [Header("Visual Settings")]
        [SerializeField] private Color healthyColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.red;
        [SerializeField] private float lowHealthThreshold = 0.3f;
        [SerializeField] private bool animateChanges = true;
        [SerializeField] private float animationSpeed = 2f;

        // Animation state
        private float currentDisplayHealth;
        private float targetHealth;
        private float maxHealth = 100f;

        public void Initialize()
        {
            // Setup initial state
            if (healthSlider == null) healthSlider = GetComponent<Slider>();
            if (fillImage == null && healthSlider != null) fillImage = healthSlider.fillRect.GetComponent<Image>();
            
            // Set initial values
            currentDisplayHealth = maxHealth;
            targetHealth = maxHealth;
            
            UpdateHealthDisplay();
        }

        private void Update()
        {
            if (animateChanges && Mathf.Abs(currentDisplayHealth - targetHealth) > 0.1f)
            {
                AnimateHealthChange();
            }
        }

        /// <summary>
        /// Update health values
        /// </summary>
        public void UpdateHealth(float currentHealth, float maxHealthValue)
        {
            maxHealth = maxHealthValue;
            targetHealth = currentHealth;

            if (!animateChanges)
            {
                currentDisplayHealth = targetHealth;
                UpdateHealthDisplay();
            }
        }

        private void AnimateHealthChange()
        {
            currentDisplayHealth = Mathf.Lerp(currentDisplayHealth, targetHealth, animationSpeed * Time.unscaledDeltaTime);
            UpdateHealthDisplay();
        }

        private void UpdateHealthDisplay()
        {
            if (healthSlider != null)
            {
                healthSlider.value = maxHealth > 0 ? currentDisplayHealth / maxHealth : 0f;
            }

            if (healthText != null)
            {
                healthText.text = $"{Mathf.RoundToInt(currentDisplayHealth)}/{Mathf.RoundToInt(maxHealth)}";
            }

            if (fillImage != null)
            {
                var healthPercentage = maxHealth > 0 ? currentDisplayHealth / maxHealth : 0f;
                fillImage.color = Color.Lerp(lowHealthColor, healthyColor, healthPercentage / lowHealthThreshold);
            }
        }

        /// <summary>
        /// Show/hide health bar
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (healthBarContainer != null)
            {
                healthBarContainer.SetActive(visible);
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }

        /// <summary>
        /// Flash health bar (damage indication)
        /// </summary>
        public void FlashDamage()
        {
            if (fillImage != null)
            {
                StartCoroutine(FlashEffect());
            }
        }

        private System.Collections.IEnumerator FlashEffect()
        {
            var originalColor = fillImage.color;
            var flashColor = Color.white;
            var flashDuration = 0.2f;
            var elapsed = 0f;

            while (elapsed < flashDuration)
            {
                var t = elapsed / flashDuration;
                fillImage.color = Color.Lerp(flashColor, originalColor, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            fillImage.color = originalColor;
        }
    }
}
