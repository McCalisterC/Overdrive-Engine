using UnityEngine;
using UnityEditor;
using FightingFramework.States;
using System.Linq;

namespace FightingFramework.Editor
{
    [CustomEditor(typeof(CharacterStateMachine))]
    public class CharacterStateMachineEditor : UnityEditor.Editor
    {
        private CharacterStateMachine stateMachine;
        private SerializedProperty currentStateProperty;
        private SerializedProperty availableStatesProperty;
        private SerializedProperty defaultStateProperty;
        private SerializedProperty debugModeProperty;
        
        private bool showStateDetails = true;
        private bool showTransitions = true;
        private bool showDebugInfo = false;
        
        private void OnEnable()
        {
            stateMachine = (CharacterStateMachine)target;
            currentStateProperty = serializedObject.FindProperty("currentState");
            availableStatesProperty = serializedObject.FindProperty("availableStates");
            defaultStateProperty = serializedObject.FindProperty("defaultState");
            debugModeProperty = serializedObject.FindProperty("debugMode");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.LabelField("Character State Machine", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Debug mode toggle
            EditorGUILayout.PropertyField(debugModeProperty);
            EditorGUILayout.Space();
            
            // Current and default states
            EditorGUILayout.LabelField("State Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(currentStateProperty);
            EditorGUILayout.PropertyField(defaultStateProperty);
            EditorGUILayout.Space();
            
            // Available states
            EditorGUILayout.PropertyField(availableStatesProperty, true);
            EditorGUILayout.Space();
            
            // Runtime information
            if (Application.isPlaying)
            {
                DrawRuntimeInfo();
            }
            
            // State details
            showStateDetails = EditorGUILayout.Foldout(showStateDetails, "State Details", true);
            if (showStateDetails)
            {
                DrawStateDetails();
            }
            
            // Quick actions
            DrawQuickActions();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawRuntimeInfo()
        {
            EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
            
            GUI.enabled = false;
            EditorGUILayout.TextField("Current State", stateMachine.CurrentState?.stateName ?? "None");
            EditorGUILayout.IntField("Current Frame", stateMachine.CurrentFrame);
            
            if (stateMachine.CurrentState != null)
            {
                var currentState = stateMachine.CurrentState;
                EditorGUILayout.TextField("State Type", currentState.GetType().Name);
                EditorGUILayout.IntField("Priority", currentState.priority);
                EditorGUILayout.Toggle("Can Be Interrupted", currentState.canBeInterrupted);
                
                // Frame data
                EditorGUILayout.LabelField("Frame Data", EditorStyles.miniBoldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.IntField("Startup", currentState.startupFrames);
                EditorGUILayout.IntField("Active", currentState.activeFrames);
                EditorGUILayout.IntField("Recovery", currentState.recoveryFrames);
                EditorGUILayout.IntField("Total", currentState.GetTotalFrames());
                EditorGUI.indentLevel--;
                
                // Current phase
                int currentFrame = stateMachine.CurrentFrame;
                string currentPhase = "Unknown";
                if (currentState.IsInStartup(currentFrame)) currentPhase = "Startup";
                else if (currentState.IsActive(currentFrame)) currentPhase = "Active";
                else if (currentState.IsInRecovery(currentFrame)) currentPhase = "Recovery";
                
                EditorGUILayout.TextField("Current Phase", currentPhase);
            }
            
            GUI.enabled = true;
            EditorGUILayout.Space();
        }
        
        private void DrawStateDetails()
        {
            if (stateMachine.GetAvailableStates() == null || stateMachine.GetAvailableStates().Length == 0)
            {
                EditorGUILayout.HelpBox("No available states configured.", MessageType.Warning);
                return;
            }
            
            EditorGUI.indentLevel++;
            
            foreach (var state in stateMachine.GetAvailableStates())
            {
                if (state == null) continue;
                
                bool isCurrentState = Application.isPlaying && stateMachine.CurrentState == state;
                
                var style = isCurrentState ? EditorStyles.helpBox : EditorStyles.label;
                var color = isCurrentState ? Color.green : GUI.color;
                
                GUI.color = color;
                EditorGUILayout.BeginVertical(style);
                GUI.color = Color.white;
                
                EditorGUILayout.LabelField($"{state.stateName} ({state.GetType().Name})", EditorStyles.boldLabel);
                
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"Priority: {state.priority}");
                EditorGUILayout.LabelField($"Can Be Interrupted: {state.canBeInterrupted}");
                EditorGUILayout.LabelField($"Transitions: {state.transitions?.Count ?? 0}");
                
                if (state.animation != null)
                {
                    EditorGUILayout.LabelField($"Animation: {state.animation.name}");
                }
                
                // Frame data
                if (state.GetTotalFrames() > 0)
                {
                    EditorGUILayout.LabelField($"Frame Data: {state.startupFrames}+{state.activeFrames}+{state.recoveryFrames} = {state.GetTotalFrames()}");
                }
                
                EditorGUI.indentLevel--;
                
                // Runtime state change button
                if (Application.isPlaying && !isCurrentState)
                {
                    if (GUILayout.Button($"Change to {state.stateName}"))
                    {
                        stateMachine.TryChangeState(state);
                    }
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawQuickActions()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Create Idle State"))
            {
                CreateStateAsset<IdleState>("Idle State");
            }
            
            if (GUILayout.Button("Create Movement State"))
            {
                CreateStateAsset<MovementState>("Movement State");
            }
            
            if (GUILayout.Button("Create Attack State"))
            {
                CreateStateAsset<AttackState>("Attack State");
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Create Block State"))
            {
                CreateStateAsset<BlockState>("Block State");
            }
            
            if (GUILayout.Button("Create Hitstun State"))
            {
                CreateStateAsset<HitstunState>("Hitstun State");
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Validate State Machine"))
            {
                ValidateStateMachine();
            }
        }
        
        private void CreateStateAsset<T>(string defaultName) where T : CharacterState
        {
            string path = EditorUtility.SaveFilePanelInProject(
                $"Create {typeof(T).Name}",
                defaultName,
                "asset",
                $"Please enter a name for the {typeof(T).Name}"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                var asset = ScriptableObject.CreateInstance<T>();
                asset.stateName = System.IO.Path.GetFileNameWithoutExtension(path);
                
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();
                
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
                
                Debug.Log($"Created {typeof(T).Name} asset at {path}");
            }
        }
        
        private void ValidateStateMachine()
        {
            var issues = new System.Collections.Generic.List<string>();
            
            // Check for null states
            var availableStates = stateMachine.GetAvailableStates();
            if (availableStates != null)
            {
                for (int i = 0; i < availableStates.Length; i++)
                {
                    if (availableStates[i] == null)
                    {
                        issues.Add($"Available state at index {i} is null");
                    }
                }
            }
            
            // Check for default state
            if (stateMachine.GetAvailableStates()?.Length > 0 && availableStates != null)
            {
                var defaultState = serializedObject.FindProperty("defaultState").objectReferenceValue as CharacterState;
                if (defaultState == null)
                {
                    issues.Add("No default state assigned");
                }
                else if (!availableStates.Contains(defaultState))
                {
                    issues.Add("Default state is not in available states list");
                }
            }
            
            // Check for duplicate state names
            if (availableStates != null)
            {
                var stateNames = availableStates.Where(s => s != null).Select(s => s.stateName).ToArray();
                var duplicates = stateNames.GroupBy(x => x).Where(g => g.Count() > 1).Select(y => y.Key);
                foreach (var duplicate in duplicates)
                {
                    issues.Add($"Duplicate state name: {duplicate}");
                }
            }
            
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation Complete", "State machine configuration is valid!", "OK");
            }
            else
            {
                string message = "Issues found:\n\n" + string.Join("\n", issues);
                EditorUtility.DisplayDialog("Validation Issues", message, "OK");
            }
        }
    }
}