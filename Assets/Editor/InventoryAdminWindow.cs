using UnityEditor;
using UnityEngine;
using System.IO;

public class InventoryAdminWindow : EditorWindow
{
    [SerializeField] private int coinAmount=1000;
    [SerializeField] private int hintAmount=10;
    [SerializeField] private int shuffleAmount=10;
    private string lastAction;

    [InitializeOnLoadMethod]
    private static void ProcessPendingBackgroundReset()
    {
        EditorApplication.delayCall+=()=>
        {
            string request=Path.GetFullPath(Path.Combine(Application.dataPath,"../Logs/ResetPurchasedBackgrounds.request"));
            if(!File.Exists(request)) return;
            var inventory=EconomyStore.AdminResetBackgrounds();
            File.Delete(request);
            File.WriteAllText(Path.ChangeExtension(request,"done"),JsonUtility.ToJson(inventory,true));
            Debug.Log("Purchased backgrounds reset for demo. Coins, boosters and level progress preserved.");
        };
    }

    [MenuItem("Tools/Inventory Admin")]
    public static void Open()
    {
        var window=GetWindow<InventoryAdminWindow>("Inventory Admin");
        window.minSize=new Vector2(440,390);
    }

    private void OnEnable() { EconomyStore.InventoryChanged+=Repaint; }
    private void OnDisable() { EconomyStore.InventoryChanged-=Repaint; }
    private void OnInspectorUpdate() { Repaint(); }

    private void OnGUI()
    {
        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("Inventory Admin",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Edit this computer's saved inventory. Changes are saved immediately and remain after stopping Play mode. This tool is only available in the Unity Editor.",MessageType.Info);
        bool busy=EditorApplication.isCompiling || EditorApplication.isUpdating ||
            (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying);
        using(new EditorGUI.DisabledScope(busy))
        {
            if(busy)
            {
                EditorGUILayout.LabelField("Waiting for the Editor...");
                return;
            }
            var inventory=EconomyStore.Inventory;
            EditorGUILayout.Space(8);
            DrawResource("Coins",inventory.coins,ref coinAmount,0);
            DrawResource("Hint",inventory.hints,ref hintAmount,1);
            DrawResource("Shuffle",inventory.shuffles,ref shuffleAmount,2);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Background demo",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Reset purchased backgrounds to buy them again.");
            EditorGUILayout.LabelField("Coins, boosters and level progress are kept.");
            if(GUILayout.Button("Reset purchased backgrounds"))
            {
                EconomyStore.AdminResetBackgrounds();
                lastAction="Purchased backgrounds reset. Default background equipped. Saved.";
            }
        }
        EditorGUILayout.Space(10);
        if(!string.IsNullOrEmpty(lastAction)) EditorGUILayout.HelpBox(lastAction,MessageType.None);
    }

    private void DrawResource(string label,int current,ref int amount,int resource)
    {
        using(new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(label+"  ·  Owned: "+current,EditorStyles.boldLabel);
            using(new EditorGUILayout.HorizontalScope())
            {
                amount=Mathf.Max(0,EditorGUILayout.IntField("Amount",amount));
                using(new EditorGUI.DisabledScope(amount==0))
                {
                    if(GUILayout.Button("Add",GUILayout.Width(65))) Adjust(label,current,amount,resource);
                    if(GUILayout.Button("Remove",GUILayout.Width(65))) Adjust(label,current,-amount,resource);
                }
            }
        }
    }

    private void Adjust(string label,int before,int delta,int resource)
    {
        var inventory=EconomyStore.AdminAdjustInventory(resource==0 ? delta : 0,
            resource==1 ? delta : 0,resource==2 ? delta : 0);
        int after=resource==0 ? inventory.coins : resource==1 ? inventory.hints : inventory.shuffles;
        lastAction=label+": "+before+" → "+after+". Saved.";
        Repaint();
    }
}
