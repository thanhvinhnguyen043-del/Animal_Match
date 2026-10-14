using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class LevelDatabaseWindow : EditorWindow
{
    private LevelDatabase database;
    private int selected;
    private int brushID;
    private TileState brushState;
    private bool erase;
    private Vector2 scroll;
    private Vector2 boardScroll;

    [MenuItem("Tools/Level Database")]
    public static void Open()
    {
        GetWindow<LevelDatabaseWindow>("Level Database");
    }

    private void OnEnable()
    {
        database = Resources.Load<LevelDatabase>("LevelDatabase");
        Undo.undoRedoPerformed += Repaint;
    }

    private void OnDisable() { Undo.undoRedoPerformed -= Repaint; }

    private void Edit() { Undo.RecordObject(database, "Edit level database"); }
    private void Save() { EditorUtility.SetDirty(database); }

    private void OnGUI()
    {
        database = (LevelDatabase)EditorGUILayout.ObjectField("Database", database, typeof(LevelDatabase), false);
        if (database == null)
        {
            if (GUILayout.Button("Tao database"))
            {
                string path = EditorUtility.SaveFilePanelInProject("Tao database", "LevelDatabase", "asset", "Chon noi luu");
                if (!string.IsNullOrEmpty(path))
                {
                    database = CreateInstance<LevelDatabase>();
                    AssetDatabase.CreateAsset(database, path);
                    AssetDatabase.SaveAssets();
                }
            }
            return;
        }
        scroll = EditorGUILayout.BeginScrollView(scroll);
        var serialized = new SerializedObject(database);
        serialized.Update();
        EditorGUILayout.PropertyField(serialized.FindProperty("animalSprites"), new GUIContent("Danh sach hinh thu"), true);
        serialized.ApplyModifiedProperties();
        if (GUILayout.Button("Lay hinh thu tu GridManager trong scene"))
        {
            var manager = FindObjectOfType<GridManager>();
            if (manager != null) { Edit(); database.animalSprites = (Sprite[])manager.animalSprites.Clone(); Save(); }
        }
        if (database.levels == null) database.levels = new List<LevelDefinition>();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Them level"))
        {
            Edit(); database.levels.Add(new LevelDefinition { name = "Level " + (database.levels.Count + 1) });
            selected = database.levels.Count - 1; Save();
        }
        if (database.levels.Count > 0 && GUILayout.Button("Nhan ban"))
        {
            Edit(); database.levels.Add(JsonUtility.FromJson<LevelDefinition>(JsonUtility.ToJson(database.levels[Mathf.Clamp(selected, 0, database.levels.Count - 1)])));
            selected = database.levels.Count - 1; Save();
        }
        if (database.levels.Count > 0 && GUILayout.Button("Xoa level"))
        {
            Edit(); database.levels.RemoveAt(Mathf.Clamp(selected, 0, database.levels.Count - 1)); Save();
        }
        EditorGUILayout.EndHorizontal();
        if (database.levels.Count == 0) { EditorGUILayout.EndScrollView(); return; }
        selected = Mathf.Clamp(selected, 0, database.levels.Count - 1);
        string[] names = database.levels.ConvertAll(l => l.name).ToArray();
        selected = EditorGUILayout.Popup("Level", selected, names);
        LevelDefinition level = database.levels[selected];
        Edit();
        EditorGUI.BeginChangeCheck();
        level.name = EditorGUILayout.TextField("Ten", level.name);
        level.mode = (LevelLayoutMode)EditorGUILayout.EnumPopup("Che do", level.mode);
        level.columns = EditorGUILayout.IntSlider("Cot", level.columns, 2, 20);
        level.rows = EditorGUILayout.IntSlider("Hang", level.rows, 2, 20);
        level.tileSize = EditorGUILayout.FloatField("Co o", level.tileSize);
        level.timeLimit = EditorGUILayout.FloatField("Thoi gian (giay)", level.timeLimit);
        level.timeBonus = EditorGUILayout.FloatField("Thuong combo (giay)", level.timeBonus);
        level.hintReward = Mathf.Max(0,EditorGUILayout.IntField("Thuong goi y khi thang",level.hintReward));
        level.shuffleReward = Mathf.Max(0,EditorGUILayout.IntField("Thuong dao ban khi thang",level.shuffleReward));
        level.gravity = (GravityDirection)EditorGUILayout.EnumPopup("Trong luc", level.gravity);
        level.animalKinds = EditorGUILayout.IntField("So loai thu tu dong", level.animalKinds);
        level.seed = EditorGUILayout.IntField("Seed", level.seed);
        level.randomEachPlay = EditorGUILayout.Toggle("Ngau nhien moi lan choi", level.randomEachPlay);
        level.iceChance = EditorGUILayout.Slider("Ti le bang", level.iceChance, 0, 1);
        level.barrierChance = EditorGUILayout.Slider("Ti le chan", level.barrierChance, 0, 1);
        if (EditorGUI.EndChangeCheck()) Save();
        EditorGUILayout.HelpBox("Automatic sinh khi choi. Manual giu dung bo cuc da luu. ID thu theo thu tu danh sach, bat dau tu 0. Ti le bang/chan ap dung theo cap.", MessageType.Info);
        if (GUILayout.Button("Sinh tu dong va chuyen sang xep tay"))
        {
            LevelLayoutMode previous = level.mode;
            level.mode = LevelLayoutMode.Automatic;
            if (level.Validate(database.animalSprites.Length, out string generationError))
            {
                level.cells = level.Generate(database.animalSprites.Length, level.seed);
                level.mode = LevelLayoutMode.Manual; Save();
            }
            else { level.mode = previous; ShowNotification(new GUIContent(generationError)); }
        }
        if (level.mode == LevelLayoutMode.Manual)
        {
            if (GUILayout.Button("Tao/doi kich thuoc ban (giu o theo thu tu danh sach)"))
            {

                while (level.cells.Count < level.columns * level.rows) level.cells.Add(new LevelCell { empty = true });
                if (level.cells.Count > level.columns * level.rows) level.cells.RemoveRange(level.columns * level.rows, level.cells.Count - level.columns * level.rows);
                Save();
            }
            brushID = EditorGUILayout.IntSlider("But: ID thu", brushID, 0, Mathf.Max(0, database.animalSprites.Length - 1));
            int state = brushState == TileState.Ice ? 1 : brushState == TileState.Barrier ? 2 : 0;
            state = EditorGUILayout.Popup("But: trang thai", state, new[] { "Normal", "Ice", "Barrier" });
            brushState = state == 1 ? TileState.Ice : state == 2 ? TileState.Barrier : TileState.Normal;
            erase = EditorGUILayout.Toggle("But xoa (o trong)", erase);
            if (level.cells.Count == level.columns * level.rows)
            {
                boardScroll = EditorGUILayout.BeginScrollView(boardScroll, GUILayout.Height(Mathf.Min(420, level.rows * 60 + 25)));
                for (int y = 0; y < level.rows; y++)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int x = 0; x < level.columns; x++)
                    {
                        LevelCell cell = level.cells[x + y * level.columns];
                        Color old = GUI.backgroundColor;
                        GUI.backgroundColor = cell.empty ? Color.gray : cell.state == TileState.Ice ? Color.cyan : cell.state == TileState.Barrier ? Color.yellow : Color.white;
                        Texture preview = !cell.empty && cell.animalID >= 0 && cell.animalID < database.animalSprites.Length ? AssetPreview.GetAssetPreview(database.animalSprites[cell.animalID]) : null;
                        var content = new GUIContent(cell.empty ? "-" : cell.animalID + "\n" + cell.state, preview, "(" + x + ", " + y + ")");
                        if (GUILayout.Button(content, GUILayout.Width(58), GUILayout.Height(55)))
                        { cell.empty = erase; cell.animalID = brushID; cell.state = brushState; Save(); }
                        GUI.backgroundColor = old;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
        }
        bool valid = level.Validate(database.animalSprites.Length, out string error);
        EditorGUILayout.HelpBox(valid ? "Du lieu hop le. Can choi thu de kiem tra duong noi va do kho; khong dam bao moi bo cuc deu giai duoc." : error, valid ? MessageType.Info : MessageType.Error);
        if (GUILayout.Button("Luu database")) { Save(); AssetDatabase.SaveAssets(); }
        using (new EditorGUI.DisabledScope(!valid || Application.isPlaying))
        {
            if (GUILayout.Button("Gan database va level nay vao GridManager trong scene"))
            {
                var manager = FindObjectOfType<GridManager>();
                if (manager != null)
                {
                    Undo.RecordObject(manager, "Assign level database");
                    manager.levelDatabase = database; manager.levelIndex = selected;
                    EditorUtility.SetDirty(manager); EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
                }
            }
        }
        EditorGUILayout.EndScrollView();
    }
}
