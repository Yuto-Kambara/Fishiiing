using UnityEngine;

/// <summary>
/// ショップ UI 専用の InventoryBinder
/// ・通常の InventoryBinder そのままで十分だが、
///   将来「在庫数の持続」「日替わり更新」等を入れたい場合に拡張しやすいよう独立クラス化
/// </summary>
public class ShopInventoryBinder : InventoryBinder
{
    // 今は継承のみ (拡張ポイント)
}
