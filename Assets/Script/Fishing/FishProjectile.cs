using UnityEngine;
using System.Collections;

/// <summary>
/// 放物線で飛ぶ魚。着地後 (Landed = true) にプレイヤーが拾うと
/// ・Rigidbody を Kinematic に
/// ・Collider を Trigger に
/// に切り替え、魚自身がクーラーボックス Trigger に触れた瞬間
/// CoolerBox.TryStoreFish が呼ばれて収納される。
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FishProjectile : MonoBehaviour
{
    /*====== メタデータ ======*/
    [System.Serializable]
    public struct FishData
    {
        public string species;
        public float lengthCm;
        public int rarity;
        public int basePrice;  // 種類ごとの基礎価格 (例: 100)
    }
    public FishInstance data;

    /*====== 状態 ======*/
    public bool Landed { get; private set; } = false;

    public bool IsHeld { get; private set; } = false;

    public float NuisancableTime = 10f; // 30 秒後に邪魔者をスポーン

    [Header("Landing")]
    public LayerMask groundLayers = ~0;     // 着地判定用

    /*--------------------------------------------------------*/
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (Landed) return;

        if ((groundLayers.value & (1 << col.gameObject.layer)) == 0) return;

        // 上向き法線で着地判定
        foreach (var c in col.contacts)
        {
            if (c.normal.y > 0.5f)
            {
                Land();
                break;
            }
        }
    }
    public void Init(FishDefinition def)
    {
        float len = Random.Range(def.minLength, def.maxLength);
        data = new FishInstance(def, len);

        if (def.sprite)                // 見た目差し替え
            GetComponent<SpriteRenderer>().sprite = def.sprite;
    }

    private void Land()
    {
        Landed = true;

        var rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        StartCoroutine(NotifyThiefAfterDelay());
    }

    private IEnumerator NotifyThiefAfterDelay()
    {
        yield return new WaitForSeconds(NuisancableTime);
        if (this != null && Landed)      // まだ残っていれば
            NuisanceManager.Instance.RegisterFish(this);
    }

    /*--------------------------------------------------------
     * プレイヤーが拾った瞬間に呼ばれる
     *  – Trigger 衝突でクーラーボックスに触れるため
     *    Collider を有効化 & isTrigger=true
     *  – Rigidbody は Kinematic (Simulated = true)
     *-------------------------------------------------------*/
    public void PrepareForCarry()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // 位置は手元で親が動かす

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        col.enabled = true;        // 念のため
        IsHeld = true;          // ★プレイヤーが保持中
    }
}
