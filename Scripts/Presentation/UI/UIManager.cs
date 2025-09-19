using UnityEngine;
using System.Collections.Generic;
using Foundation.Events;
using Entities.Player;

namespace Presentation.UI
{
    /// <summary>
    /// Main UI manager - coordinates all UI elements
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject gameHUDPanel;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("UI Components")]
        [SerializeField] private HealthBarUI healthBar;
        [SerializeField] private InventoryUI inventoryUI;

        // UI state
        private readonly Dictionary<string, GameObject> uiPanels = new();
        private readonly Stack<GameObject> panelStack = new();
        private GameObject currentActivePanel;

        public void Initialize()
        {
            // Register UI panels
            RegisterPanel("MainMenu", mainMenuPanel);
            RegisterPanel("GameHUD", gameHUDPanel);
            RegisterPanel("Inventory", inventoryPanel);
            RegisterPanel("PauseMenu", pauseMenuPanel);
            RegisterPanel("GameOver", gameOverPanel);
            RegisterPanel("Settings", settingsPanel);

            // Subscribe to events
            EventBus.Subscribe<Infrastructure.Scene.GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Subscribe<Entities.Player.PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            EventBus.Subscribe<Entities.Player.InventoryUIToggleEvent>(OnInventoryToggle);

            // Initialize UI components
            healthBar?.Initialize();
            inventoryUI?.Initialize();

            // Hide all panels initially
            HideAllPanels();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            EventBus.Unsubscribe<Infrastructure.Scene.GameStateChangedEvent>(OnGameStateChanged);
            EventBus.Unsubscribe<Entities.Player.PlayerHealthChangedEvent>(OnPlayerHealthChanged);
            EventBus.Unsubscribe<Entities.Player.InventoryUIToggleEvent>(OnInventoryToggle);
        }

        private void RegisterPanel(string panelName, GameObject panel)
        {
            if (panel != null)
            {
                uiPanels[panelName] = panel;
                panel.SetActive(false);
            }
        }

        private void OnGameStateChanged(Infrastructure.Scene.GameStateChangedEvent gameStateEvent)
        {
            switch (gameStateEvent.NewState)
            {
                case Infrastructure.Scene.GameState.MainMenu:
                    ShowPanel("MainMenu");
                    break;
                case Infrastructure.Scene.GameState.Playing:
                    ShowPanel("GameHUD");
                    break;
                case Infrastructure.Scene.GameState.Paused:
                    ShowPanelAsOverlay("PauseMenu");
                    break;
                case Infrastructure.Scene.GameState.GameOver:
                    ShowPanel("GameOver");
                    break;
            }
        }

        private void OnPlayerHealthChanged(Entities.Player.PlayerHealthChangedEvent healthEvent)
        {
            healthBar?.UpdateHealth(healthEvent.CurrentHealth, healthEvent.MaxHealth);
        }

        private void OnInventoryToggle(Entities.Player.InventoryUIToggleEvent inventoryEvent)
        {
            TogglePanel("Inventory");
        }

        /// <summary>
        /// Show panel and hide others
        /// </summary>
        public void ShowPanel(string panelName)
        {
            HideAllPanels();
            
            if (uiPanels.TryGetValue(panelName, out var panel))
            {
                panel.SetActive(true);
                currentActivePanel = panel;
                panelStack.Clear();
                panelStack.Push(panel);
                
                EventBus.Publish(new UIPanelChangedEvent(panelName, true));
            }
        }

        /// <summary>
        /// Show panel as overlay (doesn't hide others)
        /// </summary>
        public void ShowPanelAsOverlay(string panelName)
        {
            if (uiPanels.TryGetValue(panelName, out var panel))
            {
                panel.SetActive(true);
                panelStack.Push(panel);
                
                EventBus.Publish(new UIPanelChangedEvent(panelName, true));
            }
        }

        /// <summary>
        /// Hide specific panel
        /// </summary>
        public void HidePanel(string panelName)
        {
            if (uiPanels.TryGetValue(panelName, out var panel))
            {
                panel.SetActive(false);
                
                // Remove from stack if present
                var tempStack = new Stack<GameObject>();
                while (panelStack.Count > 0)
                {
                    var stackPanel = panelStack.Pop();
                    if (stackPanel != panel)
                    {
                        tempStack.Push(stackPanel);
                    }
                }
                
                // Restore stack
                while (tempStack.Count > 0)
                {
                    panelStack.Push(tempStack.Pop());
                }
                
                EventBus.Publish(new UIPanelChangedEvent(panelName, false));
            }
        }

        /// <summary>
        /// Toggle panel visibility
        /// </summary>
        public void TogglePanel(string panelName)
        {
            if (uiPanels.TryGetValue(panelName, out var panel))
            {
                if (panel.activeSelf)
                {
                    HidePanel(panelName);
                }
                else
                {
                    ShowPanelAsOverlay(panelName);
                }
            }
        }

        /// <summary>
        /// Go back to previous panel
        /// </summary>
        public void GoBack()
        {
            if (panelStack.Count > 1)
            {
                var currentPanel = panelStack.Pop();
                currentPanel.SetActive(false);
                
                var previousPanel = panelStack.Peek();
                previousPanel.SetActive(true);
                currentActivePanel = previousPanel;
            }
        }

        /// <summary>
        /// Hide all panels
        /// </summary>
        public void HideAllPanels()
        {
            foreach (var panel in uiPanels.Values)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
            
            panelStack.Clear();
            currentActivePanel = null;
        }

        /// <summary>
        /// Check if panel is active
        /// </summary>
        public bool IsPanelActive(string panelName)
        {
            return uiPanels.TryGetValue(panelName, out var panel) && panel.activeSelf;
        }

        #region Button Callbacks
        
        public void OnStartGameClicked()
        {
            Infrastructure.Scene.GameManager.Instance?.StartNewGame();
        }

        public void OnResumeGameClicked()
        {
            Infrastructure.Scene.GameManager.Instance?.SetPause(false);
        }

        public void OnPauseGameClicked()
        {
            Infrastructure.Scene.GameManager.Instance?.TogglePause();
        }

        public void OnRestartGameClicked()
        {
            Infrastructure.Scene.GameManager.Instance?.RestartScene();
        }

        public void OnSettingsClicked()
        {
            ShowPanelAsOverlay("Settings");
        }

        public void OnQuitGameClicked()
        {
            Infrastructure.Scene.GameManager.Instance?.QuitGame();
        }

        public void OnBackClicked()
        {
            GoBack();
        }
        
        #endregion
    }

    /// <summary>
    /// UI panel changed event
    /// </summary>
    public class UIPanelChangedEvent : GameEvent<UIPanelData>
    {
        public string PanelName => Data.PanelName;
        public bool IsVisible => Data.IsVisible;

        public UIPanelChangedEvent(string panelName, bool isVisible) 
            : base(new UIPanelData(panelName, isVisible))
        {
        }
    }

    [System.Serializable]
    public class UIPanelData
    {
        public string PanelName { get; }
        public bool IsVisible { get; }

        public UIPanelData(string panelName, bool isVisible)
        {
            PanelName = panelName;
            IsVisible = isVisible;
        }
    }
}
