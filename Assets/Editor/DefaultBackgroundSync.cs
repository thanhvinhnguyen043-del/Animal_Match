using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class DefaultBackgroundSync
{
    static DefaultBackgroundSync() { Undo.postprocessModifications+=OnChanged; }
    static UndoPropertyModification[] OnChanged(UndoPropertyModification[] changes)
    {
        if(Application.isPlaying) return changes;
        foreach(var change in changes)
        {
            if(change.currentValue.propertyPath!="m_Sprite") continue;
            var image=change.currentValue.target as Image;
            if(!image || !image.sprite || !image.gameObject.scene.IsValid()) continue;
            foreach(var shop in Object.FindObjectsOfType<ShopUI>(true))
            {
                if(shop.gameObject.scene!=image.gameObject.scene || shop.backgroundTargets==null || System.Array.IndexOf(shop.backgroundTargets,image)<0) continue;
                var sprite=change.currentValue.objectReference as Sprite;
                if(!sprite) sprite=image.sprite;
                EditorApplication.delayCall+=()=>ApplyDefault(sprite);
                break;
            }
        }
        return changes;
    }
    public static void ApplyDefault(Sprite sprite)
    {
        if(!sprite || Application.isPlaying) return;
        var config=Resources.Load<EconomyConfig>("EconomyConfig");
        if(!config || config.backgrounds==null) return;
        foreach(var item in config.backgrounds)
            if(item!=null && item.id=="garden")
            {
                if(item.sprite!=sprite)
                {
                    Undo.RecordObject(config,"Change default game background");
                    item.sprite=sprite;EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
                }
                EconomyStore.TryEquipBackground("garden");
                break;
            }
    }
}
