using UnityEngine;

namespace FightingFramework.States
{
    public enum ConditionType
    {
        InputPressed,
        InputHeld,
        InputReleased,
        FrameRange,
        HealthThreshold,
        VelocityThreshold,
        OnGround,
        OnHit,
        OnBlock,
        OnWhiff,
        AnimationFinished,
        Custom
    }
    
    [System.Serializable]
    public class TransitionCondition
    {
        [Header("Condition Settings")]
        public ConditionType conditionType;
        public bool negate = false;
        
        [Header("Input Conditions")]
        public string inputName;
        
        [Header("Frame Conditions")]
        public int minFrame = -1;
        public int maxFrame = -1;
        
        [Header("Numeric Conditions")]
        public float threshold;
        public ComparisonType comparison = ComparisonType.GreaterThan;
        
        [Header("Custom Condition")]
        public string customConditionName;
        
        public virtual bool IsConditionMet(CharacterController character, int currentFrame)
        {
            bool result = EvaluateCondition(character, currentFrame);
            return negate ? !result : result;
        }
        
        protected virtual bool EvaluateCondition(CharacterController character, int currentFrame)
        {
            switch (conditionType)
            {
                case ConditionType.FrameRange:
                    return IsFrameInRange(currentFrame);
                    
                case ConditionType.AnimationFinished:
                    return currentFrame >= character.GetComponent<CharacterStateMachine>().GetCurrentStateFrameCount();
                    
                case ConditionType.OnGround:
                    return character.isGrounded;
                    
                case ConditionType.InputPressed:
                    return CheckInputPressed(character);
                    
                case ConditionType.InputHeld:
                    return CheckInputHeld(character);
                    
                case ConditionType.InputReleased:
                    return CheckInputReleased(character);
                    
                case ConditionType.VelocityThreshold:
                    return CompareValue(character.velocity.magnitude, threshold, comparison);
                    
                default:
                    return false;
            }
        }
        
        private bool IsFrameInRange(int currentFrame)
        {
            if (minFrame >= 0 && currentFrame < minFrame) return false;
            if (maxFrame >= 0 && currentFrame > maxFrame) return false;
            return true;
        }
        
        private bool CheckInputPressed(CharacterController character)
        {
            return !string.IsNullOrEmpty(inputName) && UnityEngine.Input.GetButtonDown(inputName);
        }
        
        private bool CheckInputHeld(CharacterController character)
        {
            return !string.IsNullOrEmpty(inputName) && UnityEngine.Input.GetButton(inputName);
        }
        
        private bool CheckInputReleased(CharacterController character)
        {
            return !string.IsNullOrEmpty(inputName) && UnityEngine.Input.GetButtonUp(inputName);
        }
        
        private bool CompareValue(float value, float threshold, ComparisonType comparison)
        {
            switch (comparison)
            {
                case ComparisonType.GreaterThan:
                    return value > threshold;
                case ComparisonType.LessThan:
                    return value < threshold;
                case ComparisonType.EqualTo:
                    return Mathf.Approximately(value, threshold);
                case ComparisonType.GreaterThanOrEqual:
                    return value >= threshold;
                case ComparisonType.LessThanOrEqual:
                    return value <= threshold;
                default:
                    return false;
            }
        }
    }
    
    public enum ComparisonType
    {
        GreaterThan,
        LessThan,
        EqualTo,
        GreaterThanOrEqual,
        LessThanOrEqual
    }
}