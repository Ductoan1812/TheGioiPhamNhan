using System;
using UnityEngine;

namespace Foundation.Events
{
    /// <summary>
    /// Base class cho tất cả game events.
    /// Cải tiến từ pattern hiện tại: Action<PlayerData> OnPlayerDataLoaded
    /// Thành strongly-typed events với metadata.
    /// </summary>
    [Serializable]
    public abstract class GameEvent
    {
        public float timestamp;
        public string source;
        
        protected GameEvent(string source = null)
        {
            this.timestamp = Time.time;
            this.source = source ?? "Unknown";
        }
    }
    
    /// <summary>
    /// Generic base class cho events có data payload
    /// </summary>
    [Serializable]
    public abstract class GameEvent<T> : GameEvent
    {
        public T Data { get; private set; }
        
        protected GameEvent(T data, string source = null) : base(source)
        {
            Data = data;
        }
    }
    
    /// <summary>
    /// Interface cho objects có thể dispatch events
    /// </summary>
    public interface IEventDispatcher
    {
        void DispatchEvent<T>(T gameEvent) where T : GameEvent;
    }
    
    /// <summary>
    /// Interface cho objects có thể listen events
    /// </summary>
    public interface IEventListener
    {
        void RegisterEventListener<T>(Action<T> callback) where T : GameEvent;
        void UnregisterEventListener<T>(Action<T> callback) where T : GameEvent;
    }
}