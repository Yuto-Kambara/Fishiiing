using UnityEngine;

/// <summary>
/// ゲーム内通貨を一元管理するシングルトン
/// ▶ AddMoney(int) … 加算
/// ▶ TrySpend(int) … 消費（残高不足なら false）
/// ▶ OnMoneyChanged … 残高変化イベント (UI 連携用)
/// </summary>
public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    private int money = 0;
    public int Money => money;                 // 読み取り用

    public delegate void MoneyChanged(int newValue);
    public event MoneyChanged OnMoneyChanged;  // UI 側で購読

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);         // シーン跨ぎでも保持
    }

    public void AddMoney(int amount)
    {
        money += Mathf.Max(0, amount);
        OnMoneyChanged?.Invoke(money);
    }

    /// <returns>成功したら true</returns>
    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (money < amount) return false;

        money -= amount;
        OnMoneyChanged?.Invoke(money);
        return true;
    }
}
