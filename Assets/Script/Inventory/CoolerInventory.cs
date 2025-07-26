using UnityEngine;

/// <summary>
/// クーラーボックス用のインベントリUIバインダ＋魚自動収納
/// ・BoxCollider2D (IsTrigger = true) 必須
/// ・FishProjectile が触れたら ItemInstance に変換して格納
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CoolerInventory : MonoBehaviour
{
    [Header("UI Binder")]
    public InventoryBinder binder;

    [Header("Fish Item Definition")]
    public ItemDefinition fishItemDef; // 魚アイテムの見た目/タグ(Fish)を持つ定義

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var fish = other.GetComponent<FishProjectile>();
        if (!fish) return;

        // 個体情報をアイテム化
        var item = ItemInstance.FromFish(fish.data, fishItemDef);
        // Fish タグが立っているか確認
        if ((item.Tags & ItemTags.Fish) == 0)
        {
            Debug.LogWarning("fishItemDef.tags に Fish を付けてください。");
        }

        // 収納（クーラーは Fish も Lure も要らないルアーもOKにしたければ allowedMask = ItemTags.All）
        bool ok = binder.TryAddFirst(item, ItemTags.All);
        if (ok) Destroy(fish.gameObject);
        else Debug.Log("Cooler is full.");
    }
}
