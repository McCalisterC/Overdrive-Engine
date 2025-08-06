using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FightingFramework.States
{
    public class CharacterStateMachine : MonoBehaviour
    {
        [Header("State Machine")]
        [SerializeField] private CharacterState currentState;
        [SerializeField] private CharacterState[] availableStates;
        [SerializeField] private CharacterState defaultState;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private string currentStateName;
        
        private int frameCounter;
        private CharacterController characterController;
        private Queue<StateTransition> pendingTransitions = new Queue<StateTransition>();
        
        public CharacterState CurrentState => currentState;
        public int CurrentFrame => frameCounter;
        public bool IsInState(CharacterState state) => currentState == state;
        
        public event System.Action<CharacterState, CharacterState> OnStateChanged;
        
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                Debug.LogError($"CharacterStateMachine on {gameObject.name} requires a CharacterController component!", this);
            }
        }
        
        private void Start()
        {
            if (currentState == null && defaultState != null)
            {
                ChangeState(defaultState);
            }
        }
        
        private void Update()
        {
            if (currentState == null) return;
            
            currentState.UpdateState(characterController);
            frameCounter++;
            
            CheckTransitions();
            ProcessPendingTransitions();
            
            if (debugMode)
            {
                currentStateName = currentState.stateName;
            }
        }
        
        public void ChangeState(CharacterState newState)
        {
            if (newState == null) return;
            
            if (currentState != null && !currentState.CanTransitionTo(newState))
            {
                if (debugMode)
                {
                    Debug.Log($"Cannot transition from {currentState.stateName} to {newState.stateName}", this);
                }
                return;
            }
            
            var previousState = currentState;
            
            currentState?.Exit(characterController);
            currentState = newState;
            frameCounter = 0;
            currentState.Enter(characterController);
            
            OnStateChanged?.Invoke(previousState, currentState);
            
            if (debugMode)
            {
                Debug.Log($"State changed from {previousState?.stateName ?? "None"} to {currentState.stateName}", this);
            }
        }
        
        public bool TryChangeState(CharacterState newState)
        {
            if (newState == null) return false;
            
            if (currentState != null && !currentState.CanTransitionTo(newState))
            {
                return false;
            }
            
            ChangeState(newState);
            return true;
        }
        
        public void ForceChangeState(CharacterState newState)
        {
            if (newState == null) return;
            
            var previousState = currentState;
            
            currentState?.Exit(characterController);
            currentState = newState;
            frameCounter = 0;
            currentState.Enter(characterController);
            
            OnStateChanged?.Invoke(previousState, currentState);
            
            if (debugMode)
            {
                Debug.Log($"State force changed from {previousState?.stateName ?? "None"} to {currentState.stateName}", this);
            }
        }
        
        private void CheckTransitions()
        {
            if (currentState == null || currentState.transitions == null) return;
            
            var validTransitions = currentState.transitions
                .Where(t => t.targetState != null && t.CanTransition(characterController, frameCounter))
                .OrderByDescending(t => t.priority);
            
            foreach (var transition in validTransitions)
            {
                pendingTransitions.Enqueue(transition);
            }
        }
        
        private void ProcessPendingTransitions()
        {
            if (pendingTransitions.Count == 0) return;
            
            var highestPriorityTransition = pendingTransitions
                .OrderByDescending(t => t.priority)
                .FirstOrDefault();
            
            pendingTransitions.Clear();
            
            if (highestPriorityTransition != null)
            {
                ChangeState(highestPriorityTransition.targetState);
            }
        }
        
        public int GetCurrentStateFrameCount()
        {
            return currentState?.GetTotalFrames() ?? 0;
        }
        
        public bool IsCurrentStateInterruptible()
        {
            return currentState?.canBeInterrupted ?? true;
        }
        
        public CharacterState FindStateByName(string stateName)
        {
            return availableStates?.FirstOrDefault(s => s.stateName.Equals(stateName, System.StringComparison.OrdinalIgnoreCase));
        }
        
        public bool HasState(CharacterState state)
        {
            return availableStates != null && availableStates.Contains(state);
        }
        
        public void SetAvailableStates(CharacterState[] states)
        {
            availableStates = states;
        }
        
        public CharacterState[] GetAvailableStates()
        {
            return availableStates ?? new CharacterState[0];
        }
        
        private void OnValidate()
        {
            if (availableStates != null)
            {
                for (int i = 0; i < availableStates.Length; i++)
                {
                    if (availableStates[i] == null)
                    {
                        Debug.LogWarning($"Available state at index {i} is null on {gameObject.name}", this);
                    }
                }
            }
        }
    }
}