using UnityEngine;

[CreateAssetMenu(fileName = "FlyingTankData", menuName = "ScriptableObjects/FlyingTankData", order = 1)]
public class FlyingTankData : ScriptableObject
{
    [Header("基本ステータス")]
    [Tooltip("攻撃力")]
    public int attackPower = 1;

    [Tooltip("HP")]
    public int maxHp = 2;

    [Tooltip("砲撃クールダウン時間 (秒)")]
    public float attackCooldown = 0.2f;

    [Tooltip("移動 可/不可")]
    public bool canMove = true;

    [Tooltip("飛行高度 (m)")]
    public float flightAltitude = 6.0f;

    [Header("視界パラメータ")]
    [Tooltip("視界 角度 (度)")]
    public float visionAngle = 20.0f;

    [Tooltip("視界 距離 (m)")]
    public float visionDistance = 7.0f;

    [Header("移動・回転スピード")]
    [Tooltip("移動スピード (m/s)")]
    public float moveSpeed = 5.0f;

    [Tooltip("Body回転速度 (度/s)")]
    public float bodyRotationSpeed = 180.0f;

    [Tooltip("Turret（砲塔旋回）回転速度 (度/s)")]
    public float turretRotationSpeed = 60.0f;

    [Tooltip("上下Turret（砲身俯仰）回転速度 (度/s)")]
    public float pitchTurretRotationSpeed = 60.0f;
}