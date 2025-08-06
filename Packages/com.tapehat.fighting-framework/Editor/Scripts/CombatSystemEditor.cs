using UnityEngine;
using UnityEditor;
using FightingFramework.Combat;
using FightingFramework.States;
using FightingFramework.Animation;

namespace FightingFramework.Editor
{
    [CustomEditor(typeof(CombatSystem))]
    public class CombatSystemEditor : UnityEditor.Editor
    {
        private CombatSystem combatSystem;
        private bool showDebugInfo = false;
        private bool showSettings = true;
        private bool showEvents = true;
        
        private void OnEnable()
        {
            combatSystem = (CombatSystem)target;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.LabelField("Combat System", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Settings section
            showSettings = EditorGUILayout.Foldout(showSettings, "Combat Settings", true);
            if (showSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableCounterHits"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableClashing"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("clashWindow"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("combatLayers"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("debugMode"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Events section
            showEvents = EditorGUILayout.Foldout(showEvents, "Combat Events", true);
            if (showEvents)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onHitEvent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onBlockEvent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onCounterEvent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onClashEvent"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Runtime debug info
            if (Application.isPlaying)
            {
                DrawRuntimeInfo();
            }
            
            // Testing tools
            DrawTestingTools();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawRuntimeInfo()
        {
            showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "Runtime Debug Info", true);
            if (!showDebugInfo) return;
            
            EditorGUI.indentLevel++;
            
            EditorGUILayout.LabelField("System Status", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Instance Active: {CombatSystem.Instance != null}");
            
            if (CombatSystem.Instance != null)
            {
                EditorGUILayout.LabelField($"Active Combat Characters: {FindObjectsOfType<CombatCharacter>().Length}");
                
                // Show active combats (if any tracking exists)
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Active Combats", EditorStyles.boldLabel);
                
                var combatCharacters = FindObjectsOfType<CombatCharacter>();
                foreach (var character in combatCharacters)
                {
                    string status = character.IsAttacking ? "Attacking" : 
                                   character.IsBlocking ? "Blocking" : 
                                   character.IsInHitstun ? "In Hitstun" : "Neutral";
                    
                    EditorGUILayout.LabelField($"  {character.name}: {status}");
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawTestingTools()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Testing Tools", EditorStyles.boldLabel);
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Testing tools are only available during play mode.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Find Combat Characters"))
            {
                var characters = FindObjectsOfType<CombatCharacter>();
                Debug.Log($"Found {characters.Length} combat characters in scene:");
                foreach (var character in characters)
                {
                    Debug.Log($"  - {character.name} (Team {character.TeamId})");
                }
            }
            
            if (GUILayout.Button("Test Combat"))
            {
                TestCombatSystem();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Create Test Characters"))
            {
                CreateTestCharacters();
            }
            
            if (GUILayout.Button("Validate Setup"))
            {
                ValidateCombatSetup();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void TestCombatSystem()
        {
            var characters = FindObjectsOfType<CombatCharacter>();
            if (characters.Length < 2)
            {
                Debug.LogWarning("Need at least 2 combat characters to test combat system");
                return;
            }
            
            var attacker = characters[0];
            var defender = characters[1];
            
            // Create a test attack
            var testAttack = CreateTestAttackData();
            
            // Execute the attack
            var result = CombatSystem.Instance.ProcessAttack(testAttack, attacker.gameObject, defender.gameObject);
            
            Debug.Log($"Test Combat Result: {result.result} - {attacker.name} -> {defender.name}");
        }
        
        private AttackData CreateTestAttackData()
        {
            var attack = ScriptableObject.CreateInstance<AttackData>();
            attack.attackName = "Test Attack";
            attack.damageScaling = new DamageScaling
            {
                baseDamage = 10,
                damageMultiplier = 1f,
                comboScaling = 0.1f,
                counterHitBonus = 0.2f
            };
            attack.hitstun = 15;
            attack.blockstun = 8;
            attack.knockbackForce = Vector2.right * 3f;
            
            return attack;
        }
        
        private void CreateTestCharacters()
        {
            // Create two test characters for combat testing
            var char1 = CreateTestCharacter("Test Fighter 1", Vector3.left * 2f, 0);
            var char2 = CreateTestCharacter("Test Fighter 2", Vector3.right * 2f, 1);
            
            Debug.Log($"Created test characters: {char1.name} and {char2.name}");
        }
        
        private GameObject CreateTestCharacter(string name, Vector3 position, int teamId)
        {
            var character = new GameObject(name);
            character.transform.position = position;
            
            // Add required components
            var stateMachine = character.AddComponent<CharacterStateMachine>();
            var healthSystem = character.AddComponent<HealthSystem>();
            var combatCharacter = character.AddComponent<CombatCharacter>();
            var controller = character.AddComponent<CharacterController>();
            
            // Configure combat character
            combatCharacter.SetTeam(teamId);
            
            // Add a simple sprite renderer for visualization
            var spriteRenderer = character.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            spriteRenderer.color = teamId == 0 ? Color.blue : Color.red;
            
            return character;
        }
        
        private void ValidateCombatSetup()
        {
            var issues = new System.Collections.Generic.List<string>();
            
            // Check for CombatSystem instance
            if (CombatSystem.Instance == null)
            {
                issues.Add("No CombatSystem instance found in scene");
            }
            
            // Check combat characters
            var characters = FindObjectsOfType<CombatCharacter>();
            if (characters.Length == 0)
            {
                issues.Add("No CombatCharacter components found in scene");
            }
            else
            {
                foreach (var character in characters)
                {
                    // Validate character setup
                    if (character.GetComponent<HealthSystem>() == null)
                        issues.Add($"{character.name} missing HealthSystem component");
                    
                    if (character.GetComponent<CharacterStateMachine>() == null)
                        issues.Add($"{character.name} missing CharacterStateMachine component");
                    
                    if (character.GetComponent<HitboxManager>() == null)
                        issues.Add($"{character.name} missing HitboxManager component");
                    
                    if (character.GetAvailableAttacks().Length == 0)
                        issues.Add($"{character.name} has no available attacks configured");
                }
            }
            
            // Display results
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validation Complete", "Combat system setup is valid!", "OK");
            }
            else
            {
                string message = "Issues found:\n\n" + string.Join("\n", issues);
                EditorUtility.DisplayDialog("Validation Issues", message, "OK");
            }
        }
    }
    
    [CustomEditor(typeof(AttackData))]
    public class AttackDataEditor : UnityEditor.Editor
    {
        private AttackData attackData;
        private bool showDamageInfo = true;
        private bool showEffects = false;
        private bool showProperties = true;
        
        private void OnEnable()
        {
            attackData = (AttackData)target;
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.LabelField("Attack Data Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Basic properties
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackId"));
            
            EditorGUILayout.Space();
            
            // Damage section
            showDamageInfo = EditorGUILayout.Foldout(showDamageInfo, "Damage & Scaling", true);
            if (showDamageInfo)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("damageScaling"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("chipDamage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("damageType"));
                
                // Show calculated damage preview
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Damage Preview", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Base Damage: {attackData.CalculateDamage(1, false)}");
                EditorGUILayout.LabelField($"Counter Hit: {attackData.CalculateDamage(1, true)}");
                EditorGUILayout.LabelField($"Combo (5 hits): {attackData.CalculateDamage(5, false)}");
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Properties section
            showProperties = EditorGUILayout.Foldout(showProperties, "Attack Properties", true);
            if (showProperties)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("properties"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("armorBreaker"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("priority"));
                
                // Show quick property toggles
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Quick Properties", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Overhead: {attackData.IsOverhead}");
                EditorGUILayout.LabelField($"Low: {attackData.IsLow}");
                EditorGUILayout.LabelField($"Unblockable: {attackData.IsUnblockable}");
                EditorGUILayout.LabelField($"Launcher: {attackData.IsLauncher}");
                
                EditorGUI.indentLevel--;
            }
            
            // Rest of the properties
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hitstun"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blockstun"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hitpause"));
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("knockbackForce"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("knockbackCurve"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("knockbackDuration"));
            
            // Effects section
            showEffects = EditorGUILayout.Foldout(showEffects, "Effects & Audio", true);
            if (showEffects)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hitEffect"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("blockEffect"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hitSound"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("blockSound"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("screenShake"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("statusEffects"));
            
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("meterGainOnHit"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("meterGainOnBlock"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("meterGainOnWhiff"));
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}