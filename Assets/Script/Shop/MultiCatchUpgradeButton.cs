using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 「一回で釣れる魚の数」を +1 ずつ強化するボタン
/// ・初期価格 priceStep ／ クリックごとに (level+1)×priceStep
/// ・購入成功で RodStats.multiCatchCount++, 価格更新
/// </summary>
[RequireComponent(typeof(Button))]
public class MultiCatchUpgradeButton : MonoBehaviour
{
    public RodStats rod;
    public InventoryBinder playerBinder;   // 空でもOK（所持枠は使わない）
    public int priceStep = 500;

    Text label;
    Button btn;
    int level = 0;

    void Awake()
    {
        if (!rod) rod = FindFirstObjectByType<RodStats>();
        btn = GetComponent<Button>();
        label = GetComponentInChildren<Text>();
        btn.onClick.AddListener(TryUpgrade);
        RefreshLabel();
    }

    void TryUpgrade()
    {
        int price = (level + 1) * priceStep;
        var wallet = CurrencyManager.Instance;
        if (!wallet) return;
        if (!wallet.TrySpend(price))
        {
            Debug.Log("Not enough money.");
            return;
        }

        rod.UpgradeMultiCatch();
        level++;
        Debug.Log($"[SHOP] MultiCatch Lv{level} 購入  ¥{price:N0}  残高 ¥{wallet.Money:N0}");
        RefreshLabel();
    }

    void RefreshLabel()
    {
        int nextPrice = (level + 1) * priceStep;
        if (label) label.text = $"一回で釣れる魚の数 +1\n価格 ¥{nextPrice:N0}";
    }
}
