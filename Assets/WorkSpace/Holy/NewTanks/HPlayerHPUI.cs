using UnityEngine;
using UnityEngine.UI;

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

    private int currentIndex = 0;

    void Start()
    {
        // 最初はすべてcurrentSpriteにする
        firestImage.sprite = currentSprite;
        SecondImage.sprite = currentSprite;
        thiredImage.sprite = currentSprite;

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
        }
        else
        {
            Debug.LogWarning("HPlayerHPUI: プレイヤーの HHelth が見つかりません");
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
        ChangeImage();
    }

    public void ChangeImage()
    {
        // 変更するImageを取得
        switch (currentIndex)
        {
            case 0:
                firestImage.sprite = newSprite;
                break;

            case 1:
                SecondImage.sprite = newSprite;
                break;

            case 2:
                thiredImage.sprite = newSprite;
                break;

            default:
                return;
        }

        // 次のImageへ
        currentIndex++;
    }
}