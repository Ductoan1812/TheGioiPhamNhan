using System;
using System.Collections.Generic;
using UnityEngine;

namespace Foundation.Events
{
    /// <summary>
    /// Global Event Bus - cải tiến từ pattern hiện tại.
    /// Thay vì mỗi class có events riêng, ta có 1 central event system.
    /// 
    /// Cải tiến từ:
    /// - PlayerManager.Instance.OnPlayerDataLoaded
    /// - InventoryService.Instance.OnInventoryChanged
    /// 
    /// Thành:
    /// - EventBus.Dispatch(new PlayerDataLoadedEvent(data))
    /// - EventBus.Subscribe<PlayerDataLoadedEvent>(OnPlayerDataLoaded)
    /// </summary>
    public class EventBus : MonoBehaviour
    {
        public static EventBus Instance { get; private set; }
        
        // Dictionary lưu callbacks cho mỗi event type
        private readonly Dictionary<Type, List<Delegate>> eventCallbacks = new();
        
        // Debug tracking
        [SerializeField] private bool logEvents = false;
        [SerializeField] private int maxEventHistory = 100;
        private readonly List<GameEvent> eventHistory = new();
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (logEvents)
                Debug.Log("[EventBus] Initialized");
        }
        
        private void OnDestroy()
        {
            eventCallbacks.Clear();
            eventHistory.Clear();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Dispatch một event tới tất cả listeners
        /// </summary>
        public static void Dispatch<T>(T gameEvent) where T : GameEvent
        {
            Instance?.DispatchInternal(gameEvent);
        }
        
        /// <summary>
        /// Subscribe tới một event type
        /// </summary>
        public static void Subscribe<T>(Action<T> callback) where T : GameEvent
        {
            Instance?.SubscribeInternal(callback);
        }
        
        /// <summary>
        /// Unsubscribe từ một event type
        /// </summary>
        public static void Unsubscribe<T>(Action<T> callback) where T : GameEvent
        {
            Instance?.UnsubscribeInternal(callback);
        }
        
        /// <summary>
        /// Clear tất cả listeners (useful cho scene transitions)
        /// </summary>
        public static void ClearAllListeners()
        {
            Instance?.ClearAllListenersInternal();
        }
        
        /// <summary>
        /// Get event history for debugging
        /// </summary>
        public static List<GameEvent> GetEventHistory()
        {
            return Instance?.eventHistory ?? new List<GameEvent>();
        }
        
        #endregion
        
        #region Internal Implementation
        
        private void DispatchInternal<T>(T gameEvent) where T : GameEvent
        {
            var eventType = typeof(T);
            
            // Log event
            if (logEvents)
                Debug.Log($"[EventBus] Dispatching {eventType.Name} from {gameEvent.source}");
            
            // Add to history
            eventHistory.Add(gameEvent);
            if (eventHistory.Count > maxEventHistory)
                eventHistory.RemoveAt(0);
            
            // Dispatch to listeners
            if (eventCallbacks.TryGetValue(eventType, out var callbacks))
            {
                // Copy list to avoid modification during iteration
                var callbacksCopy = new List<Delegate>(callbacks);
                
                foreach (var callback in callbacksCopy)
                {
                    try
                    {
                        ((Action<T>)callback)?.Invoke(gameEvent);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[EventBus] Error in event callback for {eventType.Name}: {e}");
                    }
                }
            }
        }
        
        private void SubscribeInternal<T>(Action<T> callback) where T : GameEvent
        {
            var eventType = typeof(T);
            
            if (!eventCallbacks.ContainsKey(eventType))
                eventCallbacks[eventType] = new List<Delegate>();
            
            if (!eventCallbacks[eventType].Contains(callback))
            {
                eventCallbacks[eventType].Add(callback);
                
                if (logEvents)
                    Debug.Log($"[EventBus] Subscribed to {eventType.Name}. Total listeners: {eventCallbacks[eventType].Count}");
            }
        }
        
        private void UnsubscribeInternal<T>(Action<T> callback) where T : GameEvent
        {
            var eventType = typeof(T);
            
            if (eventCallbacks.TryGetValue(eventType, out var callbacks))
            {
                callbacks.Remove(callback);
                
                if (callbacks.Count == 0)
                    eventCallbacks.Remove(eventType);
                
                if (logEvents)
                    Debug.Log($"[EventBus] Unsubscribed from {eventType.Name}. Remaining listeners: {callbacks.Count}");
            }
        }
        
        private void ClearAllListenersInternal()
        {
            var totalListeners = 0;
            foreach (var callbacks in eventCallbacks.Values)
                totalListeners += callbacks.Count;
            
            eventCallbacks.Clear();
            
            if (logEvents)
                Debug.Log($"[EventBus] Cleared {totalListeners} listeners");
        }
        
        #endregion
        
        #region Debug Methods
        
        [ContextMenu("Log Event Statistics")]
        private void LogEventStatistics()
        {
            Debug.Log($"[EventBus] Event Types: {eventCallbacks.Count}");
            foreach (var kvp in eventCallbacks)
            {
                Debug.Log($"  {kvp.Key.Name}: {kvp.Value.Count} listeners");
            }
            Debug.Log($"[EventBus] Event History: {eventHistory.Count} events");
        }
        
        [ContextMenu("Clear Event History")]
        private void ClearEventHistory()
        {
            eventHistory.Clear();
            Debug.Log("[EventBus] Event history cleared");
        }
        
        #endregion
    }
}