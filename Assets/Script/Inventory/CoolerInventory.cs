using UnityEngine;

/// <summary>
/// �N�[���[�{�b�N�X�p�̃C���x���g��UI�o�C���_�{���������[
/// �EBoxCollider2D (IsTrigger = true) �K�{
/// �EFishProjectile ���G�ꂽ�� ItemInstance �ɕϊ����Ċi�[
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CoolerInventory : MonoBehaviour
{
    [Header("UI Binder")]
    public InventoryBinder binder;

    [Header("Fish Item Definition")]
    public ItemDefinition fishItemDef; // ���A�C�e���̌�����/�^�O(Fish)������`

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var fish = other.GetComponent<FishProjectile>();
        if (!fish) return;

        // �̏����A�C�e����
        var item = ItemInstance.FromFish(fish.data, fishItemDef);
        // Fish �^�O�������Ă��邩�m�F
        if ((item.Tags & ItemTags.Fish) == 0)
        {
            Debug.LogWarning("fishItemDef.tags �� Fish ��t���Ă��������B");
        }

        // ���[�i�N�[���[�� Fish �� Lure ���v��Ȃ����A�[��OK�ɂ�������� allowedMask = ItemTags.All�j
        bool ok = binder.TryAddFirst(item, ItemTags.All);
        if (ok) Destroy(fish.gameObject);
        else Debug.Log("Cooler is full.");
    }
}
