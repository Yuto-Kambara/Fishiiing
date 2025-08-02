using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// クーラーボックス売却ステーション（魚以外も売却可）
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CoolerVendor : MonoBehaviour
{
    [Header("Inputs & Cooldown")]
    public KeyCode sellKey = KeyCode.E;
    public float sellCooldown = 0.3f;

    [Header("References")]
    [Tooltip("クーラーパネルの InventoryBinder（UI の中身がソースオブトゥルース）")]
    public InventoryBinder coolerBinder;

    [Tooltip("旧式のデータ保持（未使用推奨）。Binder が無いときだけフォールバック")]
    public CoolerBox legacyCooler;

    [Tooltip("Binder を自動探索するときに名前に含める目印")]
    public string coolerBinderNameHint = "Cooler";

    /* 内部 */
    private bool playerInside = false;
    private float lastSellTime = -999f;

    private void Awake()
    {
        // Triggerを強制
        var col = GetComponent<Collider2D>();
        if (col && !col.isTrigger) col.isTrigger = true;

        // 参照の自動補完（Inspector未設定のときのみ）
        if (!coolerBinder)
        {
            var candidate = FindFirstObjectByType<InventoryBinder>();
            if (candidate && NameLooksCooler(candidate.name)) coolerBinder = candidate;
            else
            {
                var all = FindObjectsByType<InventoryBinder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var b in all)
                {
                    if (b && NameLooksCooler(b.name)) { coolerBinder = b; break; }
                }
                if (!coolerBinder) coolerBinder = candidate;
            }
        }

        if (!legacyCooler) legacyCooler = FindFirstObjectByType<CoolerBox>();
    }

    private bool NameLooksCooler(string n)
    {
        return !string.IsNullOrEmpty(n)
            && !string.IsNullOrEmpty(coolerBinderNameHint)
            && n.IndexOf(coolerBinderNameHint, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = true;
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    private void Update()
    {
        if (!playerInside) return;
        if (!Input.GetKeyDown(sellKey)) return;
        if (Time.time - lastSellTime < sellCooldown) return;

        lastSellTime = Time.time;
        AttemptSell();
    }

    private void AttemptSell()
    {
        int earned = 0;

        if (coolerBinder)               // ★ UI 優先：UI の中身を売却
            earned = SellFromBinder(coolerBinder);
        else if (legacyCooler)          // フォールバック（旧ロジック：魚のみのまま）
            earned = legacyCooler.SellAllFish();

        if (earned <= 0) return;

        CurrencyManager.Instance.AddMoney(earned);
        Debug.Log($"Sold items for ¥{earned}. Wallet: ¥{CurrencyManager.Instance.Money}");
    }

    /// <summary>
    /// Binder 内の売却可能アイテムを集計して売却。
    /// 魚：FishInstance.value、その他：ItemDefinition.sellPrice を使用。
    /// </summary>
    private int SellFromBinder(InventoryBinder binder)
    {
        var model = binder.Model;
        if (model == null) return 0;

        int total = 0;
        var toClear = new List<int>();

        for (int i = 0; i < model.Capacity; i++)
        {
            var it = model.Get(i);
            if (it == null) continue;

            int count = Mathf.Max(1, it.count);
            int unit = 0;

            // 魚は個体値ベース（FishInstance は struct なので null チェック不要）
            if ((it.Tags & ItemTags.Fish) != 0)
            {
                unit = Mathf.Max(0, it.fish.value);
            }
            else
            {
                // 魚以外は定義の売値
                var def = TryGetDefinition(it);
                if (def != null && def.IsSellable)
                {
                    unit = Mathf.Max(0, def.sellPrice);
                }
            }

            if (unit > 0)
            {
                total += unit * count;
                toClear.Add(i);
            }
        }

        if (total <= 0) return 0;

        // 売却したスロットを空にする
        for (int j = 0; j < toClear.Count; j++)
        {
            model.Set(toClear[j], null);
        }

        return total;
    }

    /// <summary>
    /// ItemInstance から ItemDefinition を取得（簡易リフレクション）
    /// プロジェクトで確定名がある場合は直参照に置換してください（例: return it.definition;）。
    /// </summary>
    private ItemDefinition TryGetDefinition(object it)
    {
        if (it == null) return null;

        var t = it.GetType();

        // 1) プロパティ
        foreach (var pname in new[] { "Definition", "ItemDef", "ItemDefinition", "Def", "Item" })
        {
            var p = t.GetProperty(pname, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && typeof(ScriptableObject).IsAssignableFrom(p.PropertyType))
            {
                var val = p.GetValue(it) as ItemDefinition;
                if (val != null) return val;
            }
        }

        // 2) フィールド
        foreach (var fname in new[] { "definition", "itemDef", "itemDefinition", "def", "item" })
        {
            var f = t.GetField(fname, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null && typeof(ScriptableObject).IsAssignableFrom(f.FieldType))
            {
                var val = f.GetValue(it) as ItemDefinition;
                if (val != null) return val;
            }
        }

        return null;
    }
}
