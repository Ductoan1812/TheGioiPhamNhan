using System;
using System.Collections.Generic;
using UnityEngine;

namespace Foundation.Events
{
    /// <summary>
    /// Central event bus for decoupled communication between systems
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> eventSubscribers = new();
        private static readonly Queue<GameEvent> eventQueue = new();
        private static bool isProcessingEvents = false;

        /// <summary>
        /// Subscribe to an event type
        /// </summary>
        public static void Subscribe<T>(Action<T> callback) where T : GameEvent
        {
            var eventType = typeof(T);
            
            if (!eventSubscribers.ContainsKey(eventType))
            {
                eventSubscribers[eventType] = new List<Delegate>();
            }
            
            eventSubscribers[eventType].Add(callback);
        }

        /// <summary>
        /// Unsubscribe from an event type
        /// </summary>
        public static void Unsubscribe<T>(Action<T> callback) where T : GameEvent
        {
            var eventType = typeof(T);
            
            if (eventSubscribers.ContainsKey(eventType))
            {
                eventSubscribers[eventType].Remove(callback);
                
                if (eventSubscribers[eventType].Count == 0)
                {
                    eventSubscribers.Remove(eventType);
                }
            }
        }

        /// <summary>
        /// Publish an event immediately
        /// </summary>
        public static void Publish<T>(T gameEvent) where T : GameEvent
        {
            if (gameEvent == null)
            {
                Debug.LogWarning("Attempted to publish null event");
                return;
            }

            var eventType = typeof(T);
            
            if (eventSubscribers.ContainsKey(eventType))
            {
                var subscribers = eventSubscribers[eventType];
                
                // Create a copy to avoid modification during iteration
                var subscribersCopy = new List<Delegate>(subscribers);
                
                foreach (var subscriber in subscribersCopy)
                {
                    try
                    {
                        ((Action<T>)subscriber)?.Invoke(gameEvent);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error in event subscriber for {eventType.Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Queue an event for later processing
        /// </summary>
        public static void QueueEvent<T>(T gameEvent) where T : GameEvent
        {
            if (gameEvent == null)
            {
                Debug.LogWarning("Attempted to queue null event");
                return;
            }

            eventQueue.Enqueue(gameEvent);
        }

        /// <summary>
        /// Process all queued events
        /// </summary>
        public static void ProcessQueuedEvents()
        {
            if (isProcessingEvents)
            {
                Debug.LogWarning("Already processing events, avoiding recursion");
                return;
            }

            isProcessingEvents = true;

            try
            {
                while (eventQueue.Count > 0)
                {
                    var gameEvent = eventQueue.Dequeue();
                    var eventType = gameEvent.GetType();
                    
                    if (eventSubscribers.ContainsKey(eventType))
                    {
                        var subscribers = eventSubscribers[eventType];
                        
                        foreach (var subscriber in subscribers)
                        {
                            try
                            {
                                subscriber.DynamicInvoke(gameEvent);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError($"Error processing queued event {eventType.Name}: {ex.Message}");
                            }
                        }
                    }
                }
            }
            finally
            {
                isProcessingEvents = false;
            }
        }

        /// <summary>
        /// Clear all subscribers and queued events
        /// </summary>
        public static void Clear()
        {
            eventSubscribers.Clear();
            eventQueue.Clear();
            isProcessingEvents = false;
        }

        /// <summary>
        /// Get subscriber count for a specific event type
        /// </summary>
        public static int GetSubscriberCount<T>() where T : GameEvent
        {
            var eventType = typeof(T);
            return eventSubscribers.ContainsKey(eventType) ? eventSubscribers[eventType].Count : 0;
        }
    }
}
