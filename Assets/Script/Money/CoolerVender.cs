using UnityEngine;

/// <summary>
/// クーラーボックス売却ステーション（高精度版 + UIクリア）
/// ・FindFirstObjectByType/FindObjectsByType を使用（旧APIやLINQ不使用）
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
            // まず1件だけ取得
            var candidate = FindFirstObjectByType<InventoryBinder>();
            if (candidate && NameLooksCooler(candidate.name)) coolerBinder = candidate;
            else
            {
                var all = FindObjectsByType<InventoryBinder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var b in all)
                {
                    if (b && NameLooksCooler(b.name)) { coolerBinder = b; break; }
                }
                if (!coolerBinder) coolerBinder = candidate; // それでも無ければ最初の候補
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

        if (coolerBinder)               // ★ UI 優先：UI の魚だけを売却
            earned = SellFishFromBinder(coolerBinder);
        else if (legacyCooler)          // フォールバック（旧ロジック）
            earned = legacyCooler.SellAllFish();

        if (earned <= 0) return;        // 魚が無ければ何もしない

        CurrencyManager.Instance.AddMoney(earned);
        Debug.Log($"Sold fish for ¥{earned}. Wallet: ¥{CurrencyManager.Instance.Money}");
    }

    /// <summary>
    /// Binder 内の「魚タグ」を持つ ItemInstance だけを集計して売却。
    /// 売却後は魚アイテムだけをスロットから除去。
    /// </summary>
    private int SellFishFromBinder(InventoryBinder binder)
    {
        var model = binder.Model;
        if (model == null) return 0;

        int total = 0;
        bool anyFish = false;

        // 1) 集計
        for (int i = 0; i < model.Capacity; i++)
        {
            var it = model.Get(i);
            if (it == null) continue;
            if ((it.Tags & ItemTags.Fish) == 0) continue;

            int count = Mathf.Max(1, it.count);
            int pricePerOne = it.fish.value; // FishInstance.value（個体値から確定済み）
            total += pricePerOne * count;
            anyFish = true;
        }

        if (!anyFish) return 0; // ★ 魚が無ければ売却しない

        // 2) 魚だけ削除（ルアー等は残す）
        for (int i = 0; i < model.Capacity; i++)
        {
            var it = model.Get(i);
            if (it == null) continue;
            if ((it.Tags & ItemTags.Fish) == 0) continue;
            model.Set(i, null);
        }

        return total;
    }
}