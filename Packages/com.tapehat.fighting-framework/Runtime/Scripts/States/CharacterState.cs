using System.Collections.Generic;
using UnityEngine;

namespace FightingFramework.States
{
    public abstract class CharacterState : ScriptableObject
    {
        [Header("State Properties")]
        public string stateName;
        public int priority;
        public bool canBeInterrupted;
        public List<StateTransition> transitions = new List<StateTransition>();
        
        [Header("Frame Data")]
        public int startupFrames;
        public int activeFrames;
        public int recoveryFrames;
        public AnimationClip animation;
        
        public abstract void Enter(CharacterController character);
        public abstract void UpdateState(CharacterController character);
        public abstract void Exit(CharacterController character);
        public abstract bool CanTransitionTo(CharacterState newState);
        
        public virtual int GetTotalFrames()
        {
            return startupFrames + activeFrames + recoveryFrames;
        }
        
        public virtual bool IsInStartup(int currentFrame)
        {
            return currentFrame < startupFrames;
        }
        
        public virtual bool IsActive(int currentFrame)
        {
            return currentFrame >= startupFrames && currentFrame < (startupFrames + activeFrames);
        }
        
        public virtual bool IsInRecovery(int currentFrame)
        {
            return currentFrame >= (startupFrames + activeFrames);
        }
    }
}