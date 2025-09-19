using System;
using UnityEngine;

namespace Foundation.Events
{
    /// <summary>
    /// Base class cho tất cả game events.
    /// Foundation layer chỉ chứa base classes, không có game-specific events.
    /// </summary>
    [Serializable]
    public abstract class GameEvent
    {
        [SerializeField] private string source;
        [SerializeField] private float timestamp;
        
        public string Source => source;
        public float Timestamp => timestamp;
        
        protected GameEvent(string source = "Unknown")
        {
            this.source = source ?? "Unknown";
            this.timestamp = Time.time;
        }
    }
    
    /// <summary>
    /// Generic game event với data payload
    /// </summary>
    [Serializable]
    public abstract class GameEvent<T> : GameEvent
    {
        [SerializeField] private T data;
        
        public T Data => data;
        
        protected GameEvent(T data, string source = "Unknown") : base(source)
        {
            this.data = data;
        }
    }
    
    /// <summary>
    /// Simple event chỉ có message
    /// </summary>
    [Serializable]
    public class SimpleGameEvent : GameEvent
    {
        [SerializeField] private string message;
        
        public string Message => message;
        
        public SimpleGameEvent(string message, string source = "Unknown") : base(source)
        {
            this.message = message ?? "No message";
        }
    }
}