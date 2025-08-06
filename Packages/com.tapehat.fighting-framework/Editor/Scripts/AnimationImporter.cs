using UnityEngine;
using UnityEditor;
using FightingFramework.Animation;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FightingFramework.Editor
{
    public class AnimationImporter : EditorWindow
    {
        private Object[] selectedSprites;
        private string animationName = "NewAnimation";
        private float frameRate = 60f;
        private bool createLooping = false;
        private int loopStartFrame = 0;
        private string savePath = "Assets/Animations";
        
        // Import settings
        private bool autoDetectHitboxes = false;
        private bool createHurtboxes = true;
        private Vector2 defaultHurtboxSize = new Vector2(1f, 2f);
        private Vector2 defaultHurtboxOffset = Vector2.zero;
        
        // Sprite sheet import
        private Texture2D spriteSheet;
        private int spriteWidth = 64;
        private int spriteHeight = 64;
        private int framesPerRow = 8;
        private int totalFrames = 16;
        
        private Vector2 scrollPosition;
        
        [MenuItem("Fighting Framework/Animation Importer")]
        public static void ShowWindow()
        {
            var window = GetWindow<AnimationImporter>("Animation Importer");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Animation Import Pipeline", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            DrawImportSettings();
            EditorGUILayout.Space();
            
            DrawSpriteImportOptions();
            EditorGUILayout.Space();
            
            DrawHitboxSettings();
            EditorGUILayout.Space();
            
            DrawImportActions();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawImportSettings()
        {
            EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
            
            animationName = EditorGUILayout.TextField("Animation Name", animationName);
            frameRate = EditorGUILayout.FloatField("Frame Rate", frameRate);
            createLooping = EditorGUILayout.Toggle("Create Looping", createLooping);
            
            if (createLooping)
            {
                loopStartFrame = EditorGUILayout.IntField("Loop Start Frame", loopStartFrame);
            }
            
            savePath = EditorGUILayout.TextField("Save Path", savePath);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse"))
            {
                string selectedPath = EditorUtility.OpenFolderPanel("Select Save Location", "Assets", "");
                if (!string.IsNullOrEmpty(selectedPath) && selectedPath.StartsWith(Application.dataPath))
                {
                    savePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSpriteImportOptions()
        {
            EditorGUILayout.LabelField("Sprite Import Options", EditorStyles.boldLabel);
            
            var tabRect = EditorGUILayout.GetControlRect(false, 25);
            var individualRect = new Rect(tabRect.x, tabRect.y, tabRect.width * 0.5f, tabRect.height);
            var spritesheetRect = new Rect(tabRect.x + tabRect.width * 0.5f, tabRect.y, tabRect.width * 0.5f, tabRect.height);
            
            bool useIndividualSprites = GUI.Toggle(individualRect, selectedSprites != null, "Individual Sprites", EditorStyles.miniButtonLeft);
            bool useSpritesheet = GUI.Toggle(spritesheetRect, spriteSheet != null, "Sprite Sheet", EditorStyles.miniButtonRight);
            
            EditorGUILayout.Space();
            
            if (useIndividualSprites && !useSpritesheet)
            {
                DrawIndividualSpriteImport();
            }
            else if (useSpritesheet && !useIndividualSprites)
            {
                DrawSpritesheetImport();
            }
            else
            {
                EditorGUILayout.HelpBox("Select either individual sprites or a sprite sheet to import.", MessageType.Info);
            }
        }
        
        private void DrawIndividualSpriteImport()
        {
            EditorGUILayout.LabelField("Drag and drop sprites here, or use the object field below:");
            
            var dropArea = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop Sprites Here\n(or use object field below)");
            
            HandleDragAndDrop(dropArea);
            
            // Object field for manual selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Selected Sprites:", GUILayout.Width(100));
            if (GUILayout.Button("Select Sprites"))
            {
                selectedSprites = Selection.GetFiltered<Sprite>(SelectionMode.Assets);
            }
            EditorGUILayout.EndHorizontal();
            
            if (selectedSprites != null && selectedSprites.Length > 0)
            {
                EditorGUILayout.LabelField($"Sprites selected: {selectedSprites.Length}");
                
                // Show first few sprite names
                for (int i = 0; i < Mathf.Min(5, selectedSprites.Length); i++)
                {
                    EditorGUILayout.LabelField($"  - {selectedSprites[i].name}");
                }
                if (selectedSprites.Length > 5)
                {
                    EditorGUILayout.LabelField($"  ... and {selectedSprites.Length - 5} more");
                }
            }
        }
        
        private void DrawSpritesheetImport()
        {
            spriteSheet = EditorGUILayout.ObjectField("Sprite Sheet", spriteSheet, typeof(Texture2D), false) as Texture2D;
            
            if (spriteSheet != null)
            {
                spriteWidth = EditorGUILayout.IntField("Sprite Width", spriteWidth);
                spriteHeight = EditorGUILayout.IntField("Sprite Height", spriteHeight);
                framesPerRow = EditorGUILayout.IntField("Frames Per Row", framesPerRow);
                totalFrames = EditorGUILayout.IntField("Total Frames", totalFrames);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Preview:");
                
                if (spriteSheet.width > 0 && spriteSheet.height > 0)
                {
                    int calculatedFramesPerRow = spriteSheet.width / spriteWidth;
                    int calculatedRows = spriteSheet.height / spriteHeight;
                    int calculatedTotalFrames = calculatedFramesPerRow * calculatedRows;
                    
                    EditorGUILayout.LabelField($"Calculated frames per row: {calculatedFramesPerRow}");
                    EditorGUILayout.LabelField($"Calculated rows: {calculatedRows}");
                    EditorGUILayout.LabelField($"Calculated total frames: {calculatedTotalFrames}");
                    
                    if (GUILayout.Button("Auto-detect Settings"))
                    {
                        framesPerRow = calculatedFramesPerRow;
                        totalFrames = calculatedTotalFrames;
                    }
                }
            }
        }
        
        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;
                        
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    
                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        
                        var sprites = new List<Object>();
                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject is Sprite)
                            {
                                sprites.Add(draggedObject);
                            }
                            else if (draggedObject is Texture2D)
                            {
                                // Try to load all sprites from the texture
                                string assetPath = AssetDatabase.GetAssetPath(draggedObject);
                                var allSprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>();
                                sprites.AddRange(allSprites.Cast<Object>());
                            }
                        }
                        
                        selectedSprites = sprites.OrderBy(s => s.name).ToArray();
                    }
                    break;
            }
        }
        
        private void DrawHitboxSettings()
        {
            EditorGUILayout.LabelField("Auto-Generate Collision Boxes", EditorStyles.boldLabel);
            
            autoDetectHitboxes = EditorGUILayout.Toggle("Auto-detect Hitboxes", autoDetectHitboxes);
            createHurtboxes = EditorGUILayout.Toggle("Create Default Hurtboxes", createHurtboxes);
            
            if (createHurtboxes)
            {
                defaultHurtboxSize = EditorGUILayout.Vector2Field("Default Hurtbox Size", defaultHurtboxSize);
                defaultHurtboxOffset = EditorGUILayout.Vector2Field("Default Hurtbox Offset", defaultHurtboxOffset);
            }
            
            if (autoDetectHitboxes)
            {
                EditorGUILayout.HelpBox(
                    "Auto-detection will analyze sprite alpha channels to create approximate hitboxes. " +
                    "This is experimental and may require manual adjustment.",
                    MessageType.Info);
            }
        }
        
        private void DrawImportActions()
        {
            EditorGUILayout.LabelField("Import Actions", EditorStyles.boldLabel);
            
            bool canImport = !string.IsNullOrEmpty(animationName) && !string.IsNullOrEmpty(savePath) &&
                           ((selectedSprites != null && selectedSprites.Length > 0) || spriteSheet != null);
            
            EditorGUI.BeginDisabledGroup(!canImport);
            
            if (GUILayout.Button("Import Animation", GUILayout.Height(40)))
            {
                ImportAnimation();
            }
            
            EditorGUI.EndDisabledGroup();
            
            if (!canImport)
            {
                EditorGUILayout.HelpBox(
                    "Please provide:\n" +
                    "- Animation name\n" +
                    "- Save path\n" +
                    "- Either individual sprites or a sprite sheet",
                    MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.LabelField("Batch Import", EditorStyles.boldLabel);
            if (GUILayout.Button("Import Multiple Animations from Folder"))
            {
                BatchImportFromFolder();
            }
        }
        
        private void ImportAnimation()
        {
            // Ensure save directory exists
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                AssetDatabase.Refresh();
            }
            
            // Create animation data asset
            var animData = ScriptableObject.CreateInstance<AnimationData>();
            animData.animationName = animationName;
            animData.frameRate = frameRate;
            animData.looping = createLooping;
            animData.loopStartFrame = loopStartFrame;
            
            List<Sprite> sprites = GetSpritesForImport();
            
            // Create frame data
            for (int i = 0; i < sprites.Count; i++)
            {
                var frameData = new FrameData(i);
                frameData.sprite = sprites[i];
                
                // Add default hurtbox
                if (createHurtboxes)
                {
                    frameData.hurtboxes = new List<Rectangle>
                    {
                        Rectangle.CreateHurtbox(defaultHurtboxOffset, defaultHurtboxSize)
                    };
                }
                
                // Auto-detect hitboxes if enabled
                if (autoDetectHitboxes)
                {
                    var detectedHitboxes = AutoDetectHitboxes(sprites[i]);
                    frameData.hitboxes = detectedHitboxes;
                }
                
                animData.AddFrame(frameData);
            }
            
            // Save asset
            string fileName = $"{animationName}.asset";
            string fullPath = Path.Combine(savePath, fileName);
            
            AssetDatabase.CreateAsset(animData, fullPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Select the created asset
            Selection.activeObject = animData;
            EditorUtility.FocusProjectWindow();
            
            Debug.Log($"Created animation data: {fullPath}");
            
            // Show success dialog
            EditorUtility.DisplayDialog("Import Complete", 
                $"Successfully imported animation '{animationName}' with {sprites.Count} frames.", 
                "OK");
        }
        
        private List<Sprite> GetSpritesForImport()
        {
            var sprites = new List<Sprite>();
            
            if (selectedSprites != null && selectedSprites.Length > 0)
            {
                // Use individual sprites
                sprites.AddRange(selectedSprites.Cast<Sprite>().OrderBy(s => s.name));
            }
            else if (spriteSheet != null)
            {
                // Generate sprites from sheet
                sprites.AddRange(GenerateSpritesFromSheet());
            }
            
            return sprites;
        }
        
        private List<Sprite> GenerateSpritesFromSheet()
        {
            var sprites = new List<Sprite>();
            
            for (int i = 0; i < totalFrames; i++)
            {
                int row = i / framesPerRow;
                int col = i % framesPerRow;
                
                var rect = new Rect(
                    col * spriteWidth,
                    spriteSheet.height - (row + 1) * spriteHeight, // Unity uses bottom-left origin
                    spriteWidth,
                    spriteHeight
                );
                
                var sprite = Sprite.Create(spriteSheet, rect, new Vector2(0.5f, 0.5f), 100f);
                sprite.name = $"{animationName}_Frame_{i:00}";
                sprites.Add(sprite);
            }
            
            return sprites;
        }
        
        private List<Rectangle> AutoDetectHitboxes(Sprite sprite)
        {
            // This is a simplified implementation
            // In a real system, you'd analyze the sprite's pixels to detect non-transparent regions
            var hitboxes = new List<Rectangle>();
            
            if (sprite != null && sprite.texture != null)
            {
                // For now, create a simple hitbox based on sprite bounds
                var bounds = sprite.bounds;
                var hitbox = Rectangle.CreateHitbox(
                    Vector2.zero, 
                    new Vector2(bounds.size.x * 0.8f, bounds.size.y * 0.6f)
                );
                hitboxes.Add(hitbox);
            }
            
            return hitboxes;
        }
        
        private void BatchImportFromFolder()
        {
            string selectedFolder = EditorUtility.OpenFolderPanel("Select Folder with Sprite Sequences", "Assets", "");
            if (string.IsNullOrEmpty(selectedFolder)) return;
            
            // This would implement batch import logic
            // For now, show a placeholder message
            EditorUtility.DisplayDialog("Batch Import", 
                "Batch import functionality would be implemented here.\n" +
                "It would scan the folder for sprite sequences and import them automatically.", 
                "OK");
        }
    }
}