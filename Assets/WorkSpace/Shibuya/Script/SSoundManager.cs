using UnityEngine;
using System.Collections.Generic;

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

    [Header("SE Clips (単発)")]
    [SerializeField] private AudioClip seShoot;           // 発射
    [SerializeField] private AudioClip seImpact;          // 着弾
    [SerializeField] private AudioClip seTankDestroy;     // 戦車破壊
    [SerializeField] private AudioClip seCollision;       // 衝突
    [SerializeField] private AudioClip seDecide;          // 決定音
    [SerializeField] private AudioClip seCursor;          // カーソル音
    [SerializeField] private AudioClip seStageClear;      // ステージクリア音
    [SerializeField] private AudioClip seGameOver;        // ゲームオーバー音
    [SerializeField] private AudioClip seWallVanish;      // 壁が消える音
    [SerializeField] private AudioClip seSwitchPressed;   // スイッチが押される音
    [SerializeField] private AudioClip seSwitchAppear;    // スイッチが現れる音

    [Header("SE Clips (ループ系)")]
    [SerializeField] private AudioClip seTurretRotate;    // 砲塔旋回
    [SerializeField] private AudioClip seTankMoveEngine;  // 戦車移動（エンジン）

    // 音声再生用のコンポーネント
    private AudioSource bgmSource;
    private AudioSource seSource;

    // ループSEを管理するための辞書
    private Dictionary<AudioClip, AudioSource> loopSESources = new Dictionary<AudioClip, AudioSource>();

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

        foreach (var source in loopSESources.Values)
        {
            if (source != null)
            {
                source.volume = seVolume;
            }
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

    // ★追加: BGMを停止する関数
    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }

    // 5. 単発SEを引数にする関数
    public void PlaySE(AudioClip seClip)
    {
        if (seClip == null) return;
        seSource.PlayOneShot(seClip, seVolume);
    }

    // --- ループSE用の基本関数 ---
    private void PlayLoopSE(AudioClip seClip)
    {
        if (seClip == null) return;

        if (!loopSESources.ContainsKey(seClip))
        {
            AudioSource newSource = gameObject.AddComponent<AudioSource>();
            newSource.clip = seClip;
            newSource.loop = true;
            newSource.playOnAwake = false;
            loopSESources.Add(seClip, newSource);
        }

        AudioSource targetSource = loopSESources[seClip];
        targetSource.volume = seVolume;

        if (!targetSource.isPlaying)
        {
            targetSource.Play();
        }
    }

    private void StopLoopSE(AudioClip seClip)
    {
        if (seClip == null) return;

        if (loopSESources.ContainsKey(seClip))
        {
            AudioSource targetSource = loopSESources[seClip];
            if (targetSource.isPlaying)
            {
                targetSource.Stop();
            }
        }
    }

    // ==========================================
    // 6. 各BGM再生用関数
    // ==========================================
    public void PlayBGM_InGame() => PlayBGM(bgmInGame);
    public void PlayBGM_Menu() => PlayBGM(bgmMenu);
    public void PlayBGM_StageClear() => PlayBGM(bgmStageClear);
    public void PlayBGM_GameOver() => PlayBGM(bgmGameOver);

    // ==========================================
    // 6. 各SE再生用関数（単発）
    // ==========================================
    public void PlaySE_Shoot() => PlaySE(seShoot);
    public void PlaySE_Impact() => PlaySE(seImpact);
    public void PlaySE_TankDestroy() => PlaySE(seTankDestroy);
    public void PlaySE_Collision() => PlaySE(seCollision);
    public void PlaySE_Decide() => PlaySE(seDecide);
    public void PlaySE_Cursor() => PlaySE(seCursor);
    public void PlaySE_StageClear() => PlaySE(seStageClear);
    public void PlaySE_GameOver() => PlaySE(seGameOver);
    public void PlaySE_WallVanish() => PlaySE(seWallVanish);
    public void PlaySE_SwitchPressed() => PlaySE(seSwitchPressed);
    public void PlaySE_SwitchAppear() => PlaySE(seSwitchAppear);

    // ==========================================
    // 6. 各SE再生用関数（ループ系 Start / End）
    // ==========================================
    public void PlaySE_TurretRotate_Start() => PlayLoopSE(seTurretRotate);
    public void PlaySE_TurretRotate_End() => StopLoopSE(seTurretRotate);

    public void PlaySE_TankMoveEngine_Start() => PlayLoopSE(seTankMoveEngine);
    public void PlaySE_TankMoveEngine_End() => StopLoopSE(seTankMoveEngine);
}