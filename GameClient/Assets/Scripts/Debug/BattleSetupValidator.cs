using System.Collections;
using System.Text;
using UnityEngine;

/// <summary>
/// 씬 셋업 자동 검증기 (개발 전용).
/// 빈 오브젝트에 붙이고 battle만 연결하면 플레이 시작 시 검사 결과를 콘솔에 출력.
/// 인스펙터 우클릭 → "Validate Now"로 수동 실행도 가능.
/// 검사 항목: 레이어 존재 → 마스크 포함 → 콜라이더 존재/높이 → 실측 SphereCast.
/// </summary>
public class BattleSetupValidator : MonoBehaviour
{
    [SerializeField] BattleManager battle;

    IEnumerator Start()
    {
        // StageManager.Start의 적 스폰 이후에 검사하기 위해 1프레임 대기
        yield return null;
        Validate();
    }

    [ContextMenu("Validate Now")]
    public void Validate()
    {
        if (battle == null)
        {
            Debug.LogError("[Validator] battle 미할당", this);
            return;
        }

        var sb = new StringBuilder("[Validator] 씬 셋업 검사 결과\n");
        bool anyFail = false;

        // 1. 레이어 존재 여부
        int wallLayer = LayerMask.NameToLayer("Wall");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (wallLayer < 0) { sb.AppendLine("FAIL: 프로젝트에 'Wall' 레이어 없음 (Tags and Layers에서 생성)"); anyFail = true; }
        if (enemyLayer < 0) { sb.AppendLine("FAIL: 프로젝트에 'Enemy' 레이어 없음"); anyFail = true; }

        // 2. BattleManager.collisionMask 포함 여부
        int mask = battle.CollisionMask.value;
        if (mask == 0) { sb.AppendLine("FAIL: BattleManager.collisionMask = Nothing"); anyFail = true; }
        if (wallLayer >= 0 && (mask & (1 << wallLayer)) == 0)
        { sb.AppendLine("FAIL: collisionMask에 Wall 레이어 미포함"); anyFail = true; }
        if (enemyLayer >= 0 && (mask & (1 << enemyLayer)) == 0)
        { sb.AppendLine("FAIL: collisionMask에 Enemy 레이어 미포함"); anyFail = true; }

        // 3. 씬 콜라이더 스캔: 레이어별 개수 + planeHeight 포함 여부
        float y = battle.PlaneHeight;
        int wallCount = 0, enemyCount = 0;
        foreach (var col in FindObjectsOfType<Collider>())
        {
            if (!col.enabled) continue;
            int l = col.gameObject.layer;
            bool isWall = l == wallLayer, isEnemy = l == enemyLayer;
            if (!isWall && !isEnemy) continue;

            if (isWall) wallCount++; else enemyCount++;

            if (col.bounds.min.y > y || col.bounds.max.y < y)
            {
                sb.AppendLine($"WARN: '{col.name}' 콜라이더가 planeHeight(y={y})를 미포함 " +
                              $"(bounds y: {col.bounds.min.y:F2} ~ {col.bounds.max.y:F2}) → 캐스트에 안 걸림");
                anyFail = true;
            }
        }

        sb.AppendLine($"INFO: Wall 레이어 활성 콜라이더 {wallCount}개, Enemy 레이어 {enemyCount}개");
        if (wallCount == 0 && wallLayer >= 0)
        {
            sb.AppendLine("FAIL: Wall 레이어 콜라이더 0개 → 벽 '큐브 자체'의 레이어 확인. " +
                          "부모 오브젝트만 바꾸고 자식 미적용(this object only 선택)이 흔한 원인");
            anyFail = true;
        }
        if (enemyCount == 0 && enemyLayer >= 0)
            sb.AppendLine("WARN: Enemy 레이어 콜라이더 0개 (스폰 전이거나 프리팹 레이어 미설정)");

        // 4. 실측 캐스트: 발사 위치에서 3방향으로 쏴서 실제로 뭐가 잡히는지
        CastReport(battle.LaunchPosition, Vector3.forward, sb);
        CastReport(battle.LaunchPosition, Vector3.left, sb);
        CastReport(battle.LaunchPosition, Vector3.right, sb);

        if (anyFail) Debug.LogError(sb.ToString(), this);
        else Debug.Log(sb.Append("모든 검사 통과").ToString(), this);
    }

    void CastReport(Vector3 origin, Vector3 dir, StringBuilder sb)
    {
        if (Physics.SphereCast(origin, 0.15f, dir, out RaycastHit hit, 100f,
                               battle.CollisionMask, QueryTriggerInteraction.Ignore))
            sb.AppendLine($"CAST {dir}: '{hit.collider.name}' 적중 @ {hit.distance:F2}u " +
                          $"(layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
        else
            sb.AppendLine($"CAST {dir}: MISS — 100u 내 아무것도 안 잡힘");
    }
}
