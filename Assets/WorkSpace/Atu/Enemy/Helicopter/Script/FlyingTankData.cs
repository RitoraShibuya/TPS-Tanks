using UnityEngine;

[CreateAssetMenu(fileName = "FlyingTankData", menuName = "ScriptableObjects/FlyingTankData")]
public class FlyingTankData : ScriptableObject
{
    [Header("基本ステータス")]
    [Tooltip("最大HP")]
    public float maxHealth = 100f;

    [Tooltip("移動速度")]
    public float moveSpeed = 5f;

    public float attackPower = 10f;

    [Tooltip("視界距離")]
    public float visionDistance = 20f;

    [Tooltip("視野角")]
    public float visionAngle = 90f;

    [Tooltip("車体の旋回速度")]
    public float bodyRotationSpeed = 120f;

    [Tooltip("砲塔の水平旋回速度")]
    public float turretRotationSpeed = 180f;

    [Tooltip("砲身の上下旋回速度")]
    public float pitchTurretRotationSpeed = 120f;

    [Tooltip("攻撃インターバル（秒）")]
    public float attackCooldown = 1.5f;

    [Tooltip("飛行高度")]
    public float flightAltitude = 5f;

    [Tooltip("移動可能かどうか")]
    public bool canMove = true;
}