using System.Collections.Generic;
using UnityEngine;

namespace FightingFramework.Events
{
    [CreateAssetMenu(fileName = "New Game Event", menuName = "Fighting Framework/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<GameEventListener> listeners = new List<GameEventListener>();
        
        public void Raise()
        {
            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                listeners[i]?.OnEventRaised();
            }
        }
        
        public void RegisterListener(GameEventListener listener)
        {
            if (!listeners.Contains(listener))
            {
                listeners.Add(listener);
            }
        }
        
        public void UnregisterListener(GameEventListener listener)
        {
            listeners.Remove(listener);
        }
        
        public int ListenerCount => listeners.Count;
    }
}