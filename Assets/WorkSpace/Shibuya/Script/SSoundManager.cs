using UnityEngine;

public class SSoundManager : MonoBehaviour
{
    // 1. シングルトンクラス
    public static SSoundManager Instance { get; private set; }

    // 2. BGM用音量
    [Range(0f, 1f)]
    public float bgmVolume = 1.0f;

    // 3. SE用音量
    [Range(0f, 1f)]
    public float seVolume = 1.0f;

    [Header("BGM Clips")]
    [SerializeField] private AudioClip bgmInGame;
    [SerializeField] private AudioClip bgmMenu;
    [SerializeField] private AudioClip bgmStageClear;
    [SerializeField] private AudioClip bgmGameOver;

    [Header("SE Clips")]
    [SerializeField] private AudioClip seShoot;           // 発射
    [SerializeField] private AudioClip seImpact;          // 着弾
    [SerializeField] private AudioClip seTankDestroy;     // 戦車破壊
    [SerializeField] private AudioClip seTurretRotate;    // 砲塔旋回
    [SerializeField] private AudioClip seTankMoveEngine;  // 戦車移動（エンジン）
    [SerializeField] private AudioClip seCollision;       // 衝突
    [SerializeField] private AudioClip seDecide;          // 決定音
    [SerializeField] private AudioClip seCursor;          // カーソル音
    [SerializeField] private AudioClip seStageClear;      // ステージクリア音
    [SerializeField] private AudioClip seGameOver;        // ゲームオーバー音
    [SerializeField] private AudioClip seWallVanish;      // 壁が消える音
    [SerializeField] private AudioClip seSwitchPressed;   // スイッチが押される音
    [SerializeField] private AudioClip seSwitchAppear;    // スイッチが現れる音

    // 音声再生用のコンポーネント
    private AudioSource bgmSource;
    private AudioSource seSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;

            seSource = gameObject.AddComponent<AudioSource>();
            seSource.playOnAwake = false;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = bgmVolume;
        }
    }

    // 4. BGMを引数にする関数
    public void PlayBGM(AudioClip bgmClip)
    {
        if (bgmClip == null) return;

        if (bgmSource.clip == bgmClip && bgmSource.isPlaying)
        {
            return;
        }

        bgmSource.clip = bgmClip;
        bgmSource.volume = bgmVolume;
        bgmSource.Play();
    }

    // 5. SEを引数にする関数
    public void PlaySE(AudioClip seClip)
    {
        if (seClip == null) return;
        seSource.PlayOneShot(seClip, seVolume);
    }

    // ==========================================
    // 6. 各BGM再生用関数
    // ==========================================
    public void PlayBGM_InGame() => PlayBGM(bgmInGame);
    public void PlayBGM_Menu() => PlayBGM(bgmMenu);
    public void PlayBGM_StageClear() => PlayBGM(bgmStageClear);
    public void PlayBGM_GameOver() => PlayBGM(bgmGameOver);

    // ==========================================
    // 6. 各SE再生用関数
    // ==========================================
    public void PlaySE_Shoot() => PlaySE(seShoot);
    public void PlaySE_Impact() => PlaySE(seImpact);
    public void PlaySE_TankDestroy() => PlaySE(seTankDestroy);
    public void PlaySE_TurretRotate() => PlaySE(seTurretRotate);
    public void PlaySE_TankMoveEngine() => PlaySE(seTankMoveEngine);
    public void PlaySE_Collision() => PlaySE(seCollision);
    public void PlaySE_Decide() => PlaySE(seDecide);
    public void PlaySE_Cursor() => PlaySE(seCursor);
    public void PlaySE_StageClear() => PlaySE(seStageClear);
    public void PlaySE_GameOver() => PlaySE(seGameOver);
    public void PlaySE_WallVanish() => PlaySE(seWallVanish);
    public void PlaySE_SwitchPressed() => PlaySE(seSwitchPressed);
    public void PlaySE_SwitchAppear() => PlaySE(seSwitchAppear);
}