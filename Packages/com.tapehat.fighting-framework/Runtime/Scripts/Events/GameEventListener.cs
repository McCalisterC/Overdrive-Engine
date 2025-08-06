using UnityEngine;
using UnityEngine.Events;

namespace FightingFramework.Events
{
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent gameEvent;
        [SerializeField] private UnityEvent response;
        
        private void OnEnable()
        {
            if (gameEvent != null)
            {
                gameEvent.RegisterListener(this);
            }
        }
        
        private void OnDisable()
        {
            if (gameEvent != null)
            {
                gameEvent.UnregisterListener(this);
            }
        }
        
        public void OnEventRaised()
        {
            response?.Invoke();
        }
        
        public void SetGameEvent(GameEvent newEvent)
        {
            if (gameEvent != null)
            {
                gameEvent.UnregisterListener(this);
            }
            
            gameEvent = newEvent;
            
            if (gameEvent != null && enabled)
            {
                gameEvent.RegisterListener(this);
            }
        }
    }
}