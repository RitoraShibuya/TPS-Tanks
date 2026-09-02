using UnityEngine;

public class HelicopterEngine : MonoBehaviour
{
    [Header("回転のスピード設定")]
    public float maxSpeed = 1500f;       // 最終的な最高速度
    public float acceleration = 300f;    // 1秒あたりの加速量（大きいほど早く最高速に達する）

    private float currentSpeed = 0f;     // 現在の回転速度
    private bool isEngineOn = false;     // エンジンが動いているかどうか

    [SerializeField] Transform MaineRotor; //メインローター
    [SerializeField] Transform TailRotor;　//テールローター


    private void Start()
    {
        //エンジンはオンにしておく
        isEngineOn = true;
    }


    void Update()
    {

        // エンジンONなら最高速へ、OFFなら0へ向かって現在の速度を変化させる
        if (isEngineOn)
        {
            // Mathf.MoveTowards(現在の値, 目標の値, 変化量)
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            // エンジンを切った後も、惰性で徐々に回転が止まるようにする
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * Time.deltaTime);
        }

        // 実際の回転処理（Y軸を軸に回転）
        // モデルの向きが違う場合は Vector3.forward 等に変更してください
        MaineRotor.Rotate(Vector3.up * currentSpeed * Time.deltaTime);
        TailRotor.Rotate(Vector3.forward * currentSpeed * Time.deltaTime);
    }
}