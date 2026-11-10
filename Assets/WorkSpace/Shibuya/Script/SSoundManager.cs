
using UnityEngine;
using System.Collections.Generic;

public class SSoundManager : MonoBehaviour
{
    // ==========================================
    // 1. シングルトン
    // ==========================================
    public static SSoundManager Instance { get; private set; }

    // ==========================================
    // 2. 全体音量
    // ==========================================
    [Header("Master Volume")]

    [Range(0f, 2f)]
    public float bgmVolume = 0.5f;

    [Range(0f, 3f)]
    public float seVolume = 1.5f;

    // ==========================================
    // 3. BGM設定
    // ==========================================
    [Header("BGM Clips")]

    [SerializeField] private AudioClip bgmInGame;
    [SerializeField] private AudioClip bgmMenu;
    [SerializeField] private AudioClip bgmStageClear;
    [SerializeField] private AudioClip bgmGameOver;

    [Header("BGM Individual Volume")]

    [Range(0f, 2f)]
    [SerializeField] private float bgmInGameVolume = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float bgmMenuVolume = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float bgmStageClearVolume = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float bgmGameOverVolume = 1f;

    // ==========================================
    // 4. 単発SE設定
    // ==========================================
    [Header("SE Clips (単発)")]

    [SerializeField] private AudioClip seShoot;
    [SerializeField] private AudioClip seImpact;
    [SerializeField] private AudioClip seTankDestroy;
    [SerializeField] private AudioClip seCollision;
    [SerializeField] private AudioClip seDecide;
    [SerializeField] private AudioClip seCursor;
    [SerializeField] private AudioClip seStageClear;
    [SerializeField] private AudioClip seGameOver;
    [SerializeField] private AudioClip seWallVanish;
    [SerializeField] private AudioClip seSwitchPressed;
    [SerializeField] private AudioClip seSwitchAppear;

    [Header("SE Individual Volume (単発)")]

    [Range(0f, 2f)]
    [SerializeField] private float seShootVolume = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float seImpactVolume = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float seTankDestroyVolume = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float seCollisionVolume = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float seDecideVolume = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float seCursorVolume = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float seStageClearVolume = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float seGameOverVolume = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float seWallVanishVolume = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float seSwitchPressedVolume = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float seSwitchAppearVolume = 1f;

    // ==========================================
    // 5. ループSE設定
    // ==========================================
    [Header("SE Clips (ループ系)")]

    [SerializeField] private AudioClip seTurretRotate;
    [SerializeField] private AudioClip seTankMoveEngine;

    [Header("SE Individual Volume (ループ系)")]

    [Range(0f, 2f)]
    [SerializeField] private float seTurretRotateVolume = 1f;

    [Range(0f, 2f)]
    [SerializeField] private float seTankMoveEngineVolume = 1f;

    // ==========================================
    // 6. AudioSource
    // ==========================================
    private AudioSource bgmSource;
    private AudioSource seSource;

    private Dictionary<AudioClip, AudioSource> loopSESources
        = new Dictionary<AudioClip, AudioSource>();

    private Dictionary<AudioClip, int> loopSERequestCounts
        = new Dictionary<AudioClip, int>();

    // ==========================================
    // 7. 初期化
    // ==========================================
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

    // ==========================================
    // 8. 音量更新
    // ==========================================
    private void Update()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = GetCurrentBGMVolume();
        }

        foreach (var pair in loopSESources)
        {
            if (pair.Value != null)
            {
                pair.Value.volume = seVolume * GetLoopSEVolume(pair.Key);
            }
        }
    }

    private float GetCurrentBGMVolume()
    {
        if (bgmSource == null) return 0f;

        if (bgmSource.clip == bgmInGame)
            return bgmVolume * bgmInGameVolume;

        if (bgmSource.clip == bgmMenu)
            return bgmVolume * bgmMenuVolume;

        if (bgmSource.clip == bgmStageClear)
            return bgmVolume * bgmStageClearVolume;

        if (bgmSource.clip == bgmGameOver)
            return bgmVolume * bgmGameOverVolume;

        return bgmVolume;
    }

    private float GetLoopSEVolume(AudioClip clip)
    {
        if (clip == seTurretRotate)
            return seTurretRotateVolume;

        if (clip == seTankMoveEngine)
            return seTankMoveEngineVolume;

        return 1f;
    }

    // ==========================================
    // 9. BGM再生
    // ==========================================
    public void PlayBGM(AudioClip bgmClip)
    {
        if (bgmClip == null) return;

        if (bgmSource.clip == bgmClip && bgmSource.isPlaying)
            return;

        bgmSource.clip = bgmClip;
        bgmSource.volume = GetCurrentBGMVolume();
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }

    // ==========================================
    // 10. 単発SE再生
    // ==========================================
    private void PlaySE(AudioClip seClip, float individualVolume = 1f)
    {
        if (seClip == null) return;

        seSource.PlayOneShot(
            seClip,
            seVolume * individualVolume
        );
    }

    // ==========================================
    // 11. ループSE再生
    // ==========================================
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

        loopSERequestCounts.TryGetValue(seClip, out int count);
        loopSERequestCounts[seClip] = count + 1;

        AudioSource targetSource = loopSESources[seClip];

        targetSource.volume = seVolume * GetLoopSEVolume(seClip);

        if (!targetSource.isPlaying)
        {
            targetSource.Play();
        }
    }

    private void StopLoopSE(AudioClip seClip)
    {
        if (seClip == null) return;

        loopSERequestCounts.TryGetValue(seClip, out int count);

        count = Mathf.Max(0, count - 1);
        loopSERequestCounts[seClip] = count;

        if (count > 0) return;

        if (loopSESources.TryGetValue(seClip, out AudioSource targetSource)
            && targetSource.isPlaying)
        {
            targetSource.Stop();
        }
    }

    // ==========================================
    // 12. BGM再生関数
    // ==========================================
    public void PlayBGM_InGame() => PlayBGM(bgmInGame);
    public void PlayBGM_Menu() => PlayBGM(bgmMenu);
    public void PlayBGM_StageClear() => PlayBGM(bgmStageClear);
    public void PlayBGM_GameOver() => PlayBGM(bgmGameOver);

    // ==========================================
    // 13. 単発SE再生関数
    // ==========================================
    public void PlaySE_Shoot() => PlaySE(seShoot, seShootVolume);
    public void PlaySE_Impact() => PlaySE(seImpact, seImpactVolume);
    public void PlaySE_TankDestroy() => PlaySE(seTankDestroy, seTankDestroyVolume);
    public void PlaySE_Collision() => PlaySE(seCollision, seCollisionVolume);
    public void PlaySE_Decide() => PlaySE(seDecide, seDecideVolume);
    public void PlaySE_Cursor() => PlaySE(seCursor, seCursorVolume);
    public void PlaySE_StageClear() => PlaySE(seStageClear, seStageClearVolume);
    public void PlaySE_GameOver() => PlaySE(seGameOver, seGameOverVolume);
    public void PlaySE_WallVanish() => PlaySE(seWallVanish, seWallVanishVolume);
    public void PlaySE_SwitchPressed() => PlaySE(seSwitchPressed, seSwitchPressedVolume);
    public void PlaySE_SwitchAppear() => PlaySE(seSwitchAppear, seSwitchAppearVolume);

    // ==========================================
    // 14. ループSE再生関数
    // ==========================================
    public void PlaySE_TurretRotate_Start() => PlayLoopSE(seTurretRotate);
    public void PlaySE_TurretRotate_End() => StopLoopSE(seTurretRotate);

    public void PlaySE_TankMoveEngine_Start() => PlayLoopSE(seTankMoveEngine);
    public void PlaySE_TankMoveEngine_End() => StopLoopSE(seTankMoveEngine);
}
