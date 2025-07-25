using UnityEngine;

/// <summary>
/// クーラーボックス売却ステーション（高精度版）
/// ・Collider2D (IsTrigger = true) 必須
/// ・Player が中にいる間だけ Update() で入力チェック
/// ・E キーでクーラーボックス内の魚を一括売却
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CoolerVendor : MonoBehaviour
{
    [Header("Inputs & Cooldown")]
    public KeyCode sellKey = KeyCode.E;
    public float sellCooldown = 0.3f;        // 0.3 秒連打ガード

    [Header("Reference")]
    public CoolerBox cooler;                 // 指定しなければ自動探索

    /*================ Internal ================*/
    private bool playerInside = false;
    private float lastSellTime = -999f;

    public delegate void Sold(int amount);
    public event Sold OnSold;                // UI で購読すると便利

    /*------------------------------------------*/
    private void Awake()
    {
        if (!GetComponent<Collider2D>().isTrigger)
            GetComponent<Collider2D>().isTrigger = true;

        if (!cooler) cooler = FindFirstObjectByType<CoolerBox>();
    }

    /*------------------------------------------*/
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    /*------------------------------------------*/
    private void Update()
    {
        if (!playerInside) return;
        if (!Input.GetKeyDown(sellKey)) return;
        if (Time.time - lastSellTime < sellCooldown) return;

        lastSellTime = Time.time;
        AttemptSell();
    }

    /*------------------------------------------*/
    private void AttemptSell()
    {
        if (!cooler) return;

        int earned = cooler.SellAllFish();
        if (earned <= 0) return;             // 何も入っていない

        CurrencyManager.Instance.AddMoney(earned);
        OnSold?.Invoke(earned);              // UI 更新などに利用

        Debug.Log($"Sold fish for ¥{earned}. Wallet: ¥{CurrencyManager.Instance.Money}");
    }
}
