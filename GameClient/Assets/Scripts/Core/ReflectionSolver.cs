using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 반사 경로 계산기 (3D, 순수 정적 함수).
/// 게임플레이는 XZ 평면(높이 y 고정) 위에서만 진행된다.
/// Physics.SphereCast로 충돌을 찾고, 반사 방향의 y성분을 0으로 강제하여
/// 투사체가 평면을 절대 이탈하지 않게 잠근다.
/// 조준선(Simulate)과 실제 투사체(Step)가 같은 함수를 쓰므로 궤적이 항상 일치.
/// </summary>
public static class ReflectionSolver
{
    public struct Hit
    {
        public Vector3 point;       // 충돌 시점의 투사체 중심 위치
        public Vector3 normal;      // 충돌면 법선 (평면 투영됨)
        public Vector3 inDir;       // 입사 방향
        public Vector3 outDir;      // 반사 방향
        public Collider collider;   // 부딪힌 대상 (벽 or 적)
    }

    const float SKIN = 0.005f; // 같은 면 즉시 재충돌 방지 오프셋

    /// <summary>방향/법선을 XZ 평면에 투영하고 정규화. 평면 이탈 방지의 핵심.</summary>
    static Vector3 FlattenDir(Vector3 v)
    {
        v.y = 0f;
        float mag = v.magnitude;
        // 법선이 거의 수직(바닥/천장)인 비정상 콜라이더를 만나면 안전한 기본값
        return mag < 1e-4f ? Vector3.back : v / mag;
    }

    /// <summary>
    /// origin에서 dir 방향으로 반지름 radius짜리 구를 발사했을 때의 경로 시뮬레이션.
    /// </summary>
    /// <param name="points">경로 꼭짓점 목록 (조준선 LineRenderer에 그대로 사용).</param>
    /// <param name="hits">충돌 정보 목록 (데미지/룬 판정용).</param>
    /// <param name="maxBounces">최대 반사 횟수 (조준선 1, 실제 투사체는 크게).</param>
    /// <param name="stopBelowZ">이 z좌표보다 뒤로(-z) 내려오며 후퇴 중이면 종료 = 발사 라인 복귀.</param>
    /// <returns>발사 라인에 복귀하며 끝났으면 true.</returns>
    public static bool Simulate(
        Vector3 origin, Vector3 dir, float radius,
        LayerMask mask, int maxBounces, float maxDistance,
        float stopBelowZ,
        List<Vector3> points, List<Hit> hits)
    {
        points.Clear();
        hits.Clear();

        Vector3 pos = origin;
        Vector3 d = FlattenDir(dir);
        float remaining = maxDistance;
        points.Add(pos);

        for (int bounce = 0; bounce <= maxBounces && remaining > 0f; bounce++)
        {
            bool blocked = Physics.SphereCast(pos, radius, d, out RaycastHit hit, remaining, mask,
                                              QueryTriggerInteraction.Ignore);

            // 이번 구간에서 발사 라인(z)을 후방 통과하는지 먼저 검사
            float segLen = blocked ? hit.distance : remaining;
            if (d.z < 0f && pos.z > stopBelowZ)
            {
                float travelToLine = (pos.z - stopBelowZ) / -d.z;
                if (travelToLine <= segLen)
                {
                    points.Add(pos + d * travelToLine);
                    return true; // 발사 라인 복귀로 종료
                }
            }

            if (!blocked)
            {
                points.Add(pos + d * remaining);
                return false; // 거리 한도 소진
            }

            Vector3 hitCenter = pos + d * hit.distance;
            points.Add(hitCenter);

            Vector3 normal = FlattenDir(hit.normal);
            Vector3 outDir = FlattenDir(Vector3.Reflect(d, normal));
            hits.Add(new Hit
            {
                point = hitCenter,
                normal = normal,
                inDir = d,
                outDir = outDir,
                collider = hit.collider,
            });

            remaining -= hit.distance;
            pos = hitCenter + normal * SKIN;
            d = outDir;
        }

        return false; // 최대 반사 횟수 도달
    }

    /// <summary>
    /// 다음 충돌 지점"까지만" 전진하는 스텝 함수.
    /// 충돌 시 pos를 충돌 지점(+SKIN)으로 옮기고 true 반환. 반사/관통/연쇄 등
    /// "충돌 후 방향" 결정은 호출자(Projectile의 효과 파이프라인) 몫이므로
    /// 예전 Step처럼 다중 반사를 내부에서 소화하지 않는다.
    /// </summary>
    /// <param name="consumed">이번 호출로 소모한 이동 거리.</param>
    /// <returns>충돌이 있었으면 true (hit 유효).</returns>
    public static bool StepOnce(
        ref Vector3 pos, Vector3 dir, float radius, float maxDist,
        LayerMask mask, out Hit hit, out float consumed)
    {
        Vector3 d = FlattenDir(dir);

        if (!Physics.SphereCast(pos, radius, d, out RaycastHit rc, maxDist, mask,
                                QueryTriggerInteraction.Ignore))
        {
            pos += d * maxDist;
            consumed = maxDist;
            hit = default;
            return false;
        }

        Vector3 hitCenter = pos + d * rc.distance;
        Vector3 normal = FlattenDir(rc.normal);
        hit = new Hit
        {
            point = hitCenter,
            normal = normal,
            inDir = d,
            outDir = FlattenDir(Vector3.Reflect(d, normal)),
            collider = rc.collider,
        };

        pos = hitCenter + normal * SKIN;
        consumed = rc.distance;
        return true;
    }

    /// <summary>
    /// 관통 시 콜라이더 "반대편 출구" 위치 계산.
    /// 콜라이더 바깥 먼 지점에서 역방향 레이를 쏴 출구면을 찾고,
    /// 반지름 + SKIN만큼 진행 방향으로 밀어낸 지점을 반환한다.
    /// 스침각에서 미세 겹침이 남을 수 있지만, SphereCast는 시작 시점에 겹쳐 있는
    /// 콜라이더를 보고하지 않으므로(유니티 명세) 재타격은 발생하지 않는다.
    /// </summary>
    public static Vector3 ComputePierceExit(Collider col, Vector3 entryCenter, Vector3 dir, float radius)
    {
        Vector3 d = FlattenDir(dir);
        float probe = col.bounds.size.magnitude + radius * 2f + 0.1f;
        Ray back = new Ray(entryCenter + d * probe, -d);

        Vector3 exit;
        if (col.Raycast(back, out RaycastHit rc, probe))
            exit = rc.point + d * (radius + SKIN);
        else
            exit = entryCenter + d * (radius * 2f + SKIN); // 폴백 (이론상 미도달)

        exit.y = entryCenter.y; // 게임플레이 평면 고정
        return exit;
    }
}