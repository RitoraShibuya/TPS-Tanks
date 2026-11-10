using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイヤーのHPアイコン表示
/// 被弾するたびに左のアイコンから順に newSprite に切り替える
/// </summary>
public class HPlayerHPUI : MonoBehaviour
{
    [Header("Player HP UI")]
    [SerializeField] private Image firestImage;
    [SerializeField] private Image SecondImage;
    [SerializeField] private Image thiredImage;

    [SerializeField] private Sprite currentSprite;
    [SerializeField] private Sprite newSprite;

    [Header("監視するプレイヤーの体力 (未設定ならTankMovementから自動取得)")]
    [SerializeField] private HHelth playerHealth;

    private Image[] hpImages;

    void Start()
    {
        hpImages = new[] { firestImage, SecondImage, thiredImage };

        // プレイヤーのHHelthを探す
        // (HHelthは敵や的にも付いているので、TankMovementのものを使う)
        if (playerHealth == null)
        {
            TankMovement player = FindFirstObjectByType<TankMovement>();
            if (player != null)
            {
                playerHealth = player.GetComponent<HHelth>();
            }
        }

        if (playerHealth != null)
        {
            playerHealth.OnDamaged += OnPlayerDamaged;

            if (playerHealth.MaxHealth != hpImages.Length)
            {
                Debug.LogWarning($"HPlayerHPUI: プレイヤーの最大HP({playerHealth.MaxHealth})とアイコン数({hpImages.Length})が一致していません");
            }

            Refresh(playerHealth.Health);
        }
        else
        {
            Debug.LogWarning("HPlayerHPUI: プレイヤーの HHelth が見つかりません");
            Refresh(hpImages.Length);
        }
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamaged -= OnPlayerDamaged;
        }
    }

    private void OnPlayerDamaged(int remainingHealth)
    {
        Refresh(remainingHealth);
    }

    /// <summary>
    /// 残り体力に合わせてアイコンを更新する（減った分だけ左から newSprite にする）
    /// </summary>
    private void Refresh(int remainingHealth)
    {
        int maxHealth = playerHealth != null ? playerHealth.MaxHealth : hpImages.Length;
        int lostCount = maxHealth - remainingHealth;

        for (int i = 0; i < hpImages.Length; i++)
        {
            if (hpImages[i] == null) continue;
            hpImages[i].sprite = i < lostCount ? newSprite : currentSprite;
        }
    }
}
