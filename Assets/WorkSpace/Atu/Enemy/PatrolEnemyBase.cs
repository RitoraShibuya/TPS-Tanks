using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NavMeshAgent で移動し、WayPoint を巡回する敵の共通基底クラス
/// （地上戦車・ヘリで共通の巡回処理をまとめる）
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public abstract class PatrolEnemyBase : EnemyBase
{
    [Header("WayPoint（巡回）設定")]
    [SerializeField] protected List<Transform> waypoints = new List<Transform>();
    [SerializeField] protected PatrolType patrolType = PatrolType.Loop;
    [SerializeField] protected float waypointArriveDistance = 1.5f;
    [SerializeField] protected float waypointWaitTime = 2f;

    protected NavMeshAgent agent;

    // 一度プレイヤーを見つけたら追跡を続ける
    protected bool isPlayerDetected = false;

    private int currentWaypointIndex = 0;
    private bool isPatrolReverse = false;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
    }

    protected bool IsAgentReady => agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh;

    protected bool HasValidWaypoints()
    {
        if (waypoints == null || waypoints.Count == 0) return false;

        foreach (var wp in waypoints)
        {
            if (wp != null) return true;
        }

        return false;
    }

    /// <summary>
    /// WayPoint 巡回処理（毎フレーム呼ぶ）
    /// </summary>
    protected void PatrolWaypoints()
    {
        if (!IsAgentReady || !HasValidWaypoints()) return;

        if (isWaiting)
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= waypointWaitTime)
            {
                isWaiting = false;
                waitTimer = 0f;
                SetNextWaypoint();
            }

            return;
        }

        if (currentWaypointIndex >= waypoints.Count) currentWaypointIndex = 0;

        Transform currentPoint = waypoints[currentWaypointIndex];

        if (currentPoint == null)
        {
            SetNextWaypoint();
            return;
        }

        MoveToPatrolPoint(currentPoint.position);

        if (!agent.pathPending && agent.remainingDistance <= waypointArriveDistance)
        {
            isWaiting = true;
        }
    }

    /// <summary>
    /// 巡回地点へ向かう（派生クラスで向きの制御などを追加できる）
    /// </summary>
    protected virtual void MoveToPatrolPoint(Vector3 destination)
    {
        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    private void SetNextWaypoint()
    {
        if (waypoints == null || waypoints.Count <= 1) return;

        switch (patrolType)
        {
            case PatrolType.Loop:
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
                break;

            case PatrolType.PingPong:
                if (isPatrolReverse)
                {
                    currentWaypointIndex--;
                    if (currentWaypointIndex <= 0)
                    {
                        currentWaypointIndex = 0;
                        isPatrolReverse = false;
                    }
                }
                else
                {
                    currentWaypointIndex++;
                    if (currentWaypointIndex >= waypoints.Count - 1)
                    {
                        currentWaypointIndex = waypoints.Count - 1;
                        isPatrolReverse = true;
                    }
                }
                break;

            case PatrolType.Random:
                currentWaypointIndex = Random.Range(0, waypoints.Count);
                break;
        }
    }

    /// <summary>
    /// Gizmo で WayPoint を表示する際の高さ（ヘリは飛行高度ぶん上げる）
    /// </summary>
    protected virtual float WaypointGizmoHeight => 0f;

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (waypoints == null || waypoints.Count == 0) return;

        Gizmos.color = Color.cyan;
        Vector3 offset = Vector3.up * WaypointGizmoHeight;

        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i] == null) continue;

            Vector3 wpPos = waypoints[i].position + offset;
            Gizmos.DrawWireSphere(wpPos, 0.5f);

            int next = (i + 1) % waypoints.Count;
            if (waypoints[next] != null)
            {
                Gizmos.DrawLine(wpPos, waypoints[next].position + offset);
            }
        }
    }
}
