using System.Collections.Generic;
using UnityEngine;

namespace FightingFramework.States
{
    [System.Serializable]
    public class StateTransition
    {
        public CharacterState targetState;
        public List<TransitionCondition> conditions = new List<TransitionCondition>();
        public int priority;
        
        public bool CanTransition(CharacterController character, int currentFrame)
        {
            if (conditions.Count == 0) return false;
            
            foreach (var condition in conditions)
            {
                if (!condition.IsConditionMet(character, currentFrame))
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}