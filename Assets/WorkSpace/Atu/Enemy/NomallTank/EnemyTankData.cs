using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyTankData",
    menuName = "Game/Enemy Tank Data"
)]
public class EnemyTankData : ScriptableObject
{
    [Header("基本ステータス")]
    public int attackPower = 1;
    public int maxHp = 2;

    [Header("攻撃")]
    public float attackCooldown = 0.2f;

    [Header("視界")]
    public float sightAngle = 30f;
    public float sightDistance = 5f;

    [Header("移動")]
    public float moveSpeed = 4f;
    public float bodyRotationSpeed = 120f;

    [Header("砲塔")]
    public float turretRotationSpeed = 60f;
    public float pitchTurretRotationSpeed = 60f;
}
