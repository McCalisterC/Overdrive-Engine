using UnityEngine;
using UnityEditor;
using FightingFramework.States;

namespace FightingFramework.Editor
{
    public class StateMachineWizard : EditorWindow
    {
        private GameObject targetCharacter;
        private string characterName = "Fighter";
        private bool createBasicStates = true;
        private bool createAdvancedStates = false;
        private bool setupAnimatorController = true;
        
        private Vector2 scrollPosition;
        
        [MenuItem("Fighting Framework/State Machine Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<StateMachineWizard>("State Machine Wizard");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("State Machine Setup Wizard", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Target character selection
            EditorGUILayout.LabelField("Character Setup", EditorStyles.boldLabel);
            targetCharacter = EditorGUILayout.ObjectField("Target Character", targetCharacter, typeof(GameObject), true) as GameObject;
            characterName = EditorGUILayout.TextField("Character Name", characterName);
            EditorGUILayout.Space();
            
            // State creation options
            EditorGUILayout.LabelField("State Creation Options", EditorStyles.boldLabel);
            createBasicStates = EditorGUILayout.Toggle("Create Basic States (Idle, Walk, Jump)", createBasicStates);
            createAdvancedStates = EditorGUILayout.Toggle("Create Combat States (Attack, Block, Hitstun)", createAdvancedStates);
            EditorGUILayout.Space();
            
            // Animation setup
            EditorGUILayout.LabelField("Animation Setup", EditorStyles.boldLabel);
            setupAnimatorController = EditorGUILayout.Toggle("Setup Animator Controller", setupAnimatorController);
            EditorGUILayout.Space();
            
            // Validation
            bool canCreate = targetCharacter != null && !string.IsNullOrEmpty(characterName);
            
            if (!canCreate)
            {
                EditorGUILayout.HelpBox("Please assign a target character and enter a character name.", MessageType.Warning);
            }
            
            EditorGUI.BeginDisabledGroup(!canCreate);
            
            if (GUILayout.Button("Create State Machine", GUILayout.Height(40)))
            {
                CreateStateMachine();
            }
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // Instructions
            EditorGUILayout.LabelField("Instructions", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(
                "1. Select the character GameObject you want to add a state machine to\n" +
                "2. Enter a name for your character (used for asset naming)\n" +
                "3. Choose which states to create automatically\n" +
                "4. Click 'Create State Machine' to generate all assets and components\n\n" +
                "This wizard will:\n" +
                "• Add CharacterStateMachine component to your character\n" +
                "• Create ScriptableObject assets for each state\n" +
                "• Configure basic transitions between states\n" +
                "• Set up default animations (if animator controller is selected)",
                GUILayout.ExpandHeight(true)
            );
            
            EditorGUILayout.EndScrollView();
        }
        
        private void CreateStateMachine()
        {
            // Create folder structure
            string basePath = $"Assets/States/{characterName}";
            if (!AssetDatabase.IsValidFolder(basePath))
            {
                var pathParts = basePath.Split('/');
                string currentPath = pathParts[0];
                for (int i = 1; i < pathParts.Length; i++)
                {
                    string newPath = currentPath + "/" + pathParts[i];
                    if (!AssetDatabase.IsValidFolder(newPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, pathParts[i]);
                    }
                    currentPath = newPath;
                }
            }
            
            // Add state machine component
            var stateMachine = targetCharacter.GetComponent<CharacterStateMachine>();
            if (stateMachine == null)
            {
                stateMachine = targetCharacter.AddComponent<CharacterStateMachine>();
            }
            
            var createdStates = new System.Collections.Generic.List<CharacterState>();
            
            // Create basic states
            if (createBasicStates)
            {
                createdStates.Add(CreateState<IdleState>($"{basePath}/IdleState.asset", "Idle"));
                createdStates.Add(CreateState<MovementState>($"{basePath}/WalkState.asset", "Walk"));
                createdStates.Add(CreateState<MovementState>($"{basePath}/JumpState.asset", "Jump"));
            }
            
            // Create combat states
            if (createAdvancedStates)
            {
                createdStates.Add(CreateState<AttackState>($"{basePath}/LightPunchState.asset", "Light Punch"));
                createdStates.Add(CreateState<AttackState>($"{basePath}/HeavyPunchState.asset", "Heavy Punch"));
                createdStates.Add(CreateState<BlockState>($"{basePath}/BlockState.asset", "Block"));
                createdStates.Add(CreateState<HitstunState>($"{basePath}/HitstunState.asset", "Hitstun"));
            }
            
            // Configure state machine
            stateMachine.SetAvailableStates(createdStates.ToArray());
            
            // Set default state (first idle state found)
            var idleState = createdStates.Find(s => s.stateName.ToLower().Contains("idle"));
            if (idleState != null)
            {
                var defaultStateProperty = new SerializedObject(stateMachine).FindProperty("defaultState");
                defaultStateProperty.objectReferenceValue = idleState;
                new SerializedObject(stateMachine).ApplyModifiedProperties();
            }
            
            // Setup animator controller
            if (setupAnimatorController)
            {
                SetupAnimatorController(basePath);
            }
            
            // Configure basic transitions
            ConfigureBasicTransitions(createdStates);
            
            // Save assets
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the character in hierarchy
            Selection.activeGameObject = targetCharacter;
            
            EditorUtility.DisplayDialog("Success", 
                $"State machine created successfully!\n\n" +
                $"Created {createdStates.Count} states in {basePath}\n" +
                $"Added CharacterStateMachine component to {targetCharacter.name}", 
                "OK");
            
            Close();
        }
        
        private T CreateState<T>(string path, string stateName) where T : CharacterState
        {
            var state = ScriptableObject.CreateInstance<T>();
            state.stateName = stateName;
            state.canBeInterrupted = true;
            
            // Set some sensible defaults based on state type
            if (state is AttackState attackState)
            {
                attackState.startupFrames = 5;
                attackState.activeFrames = 3;
                attackState.recoveryFrames = 7;
                attackState.priority = 5;
            }
            else if (state is MovementState)
            {
                state.priority = 1;
            }
            else if (state is IdleState)
            {
                state.priority = 0;
            }
            
            AssetDatabase.CreateAsset(state, path);
            return state;
        }
        
        private void SetupAnimatorController(string basePath)
        {
            var animator = targetCharacter.GetComponent<Animator>();
            if (animator == null)
            {
                animator = targetCharacter.AddComponent<Animator>();
            }
            
            if (animator.runtimeAnimatorController == null)
            {
                var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath($"{basePath}/{characterName}_Controller.controller");
                animator.runtimeAnimatorController = controller;
                
                // Add basic states to animator
                var rootStateMachine = controller.layers[0].stateMachine;
                rootStateMachine.AddState("Idle");
                rootStateMachine.AddState("Walk");
                rootStateMachine.AddState("Jump");
                
                if (createAdvancedStates)
                {
                    rootStateMachine.AddState("Attack");
                    rootStateMachine.AddState("Block");
                    rootStateMachine.AddState("Hitstun");
                }
            }
        }
        
        private void ConfigureBasicTransitions(System.Collections.Generic.List<CharacterState> states)
        {
            // This is a basic example - in a real implementation, you'd want more sophisticated transition logic
            foreach (var state in states)
            {
                if (state.transitions == null)
                {
                    state.transitions = new System.Collections.Generic.List<StateTransition>();
                }
                
                // Add some basic transitions (this is very simplified)
                if (state.stateName == "Idle")
                {
                    var walkState = states.Find(s => s.stateName == "Walk");
                    if (walkState != null)
                    {
                        var transition = new StateTransition
                        {
                            targetState = walkState,
                            priority = 1
                        };
                        
                        var condition = new TransitionCondition
                        {
                            conditionType = ConditionType.InputPressed,
                            inputName = "Horizontal"
                        };
                        
                        transition.conditions.Add(condition);
                        state.transitions.Add(transition);
                    }
                }
            }
        }
    }
}