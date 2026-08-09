using System.Collections.Generic;
using UnityEngine;

// 정적 배칭 결합. 맵 로드가 끝난 뒤 움직이지 않는 배치물들을 하나의 배칭 그룹으로 묶는다.
// 같은 메시·머티리얼이라도 오브젝트마다 드로우콜이 나가던 것을 크게 줄인다.
//
// 주의: 결합된 오브젝트는 이동·회전·스케일 변경이 반영되지 않는다.
//       채취로 사라지는 나무·바위는 Destroy되므로 문제없고(그리기에서 빠짐),
//       흔들리는 연출(HitFeedback)이 붙은 것은 제외해야 한다.
public class StaticBatcher : MonoBehaviour
{
    [SerializeField] private MapLoader loader;   // 비우면 씬에서 찾음
    [Tooltip("결합할 대상들의 부모. 비우면 MapLoader의 컨테이너(= 로더 오브젝트)")]
    [SerializeField] private Transform container;

    [Header("제외")]
    [Tooltip("HitFeedback처럼 움직이는 연출이 붙은 오브젝트는 제외")]
    [SerializeField] private bool excludeAnimated = true;

    [Header("디버그")]
    [SerializeField] private bool verboseLog = true;

    private void Awake()
    {
        if (loader == null) loader = FindObjectOfType<MapLoader>();
        if (loader != null) loader.OnLoaded += Combine;
    }

    private void OnDestroy()
    {
        if (loader != null) loader.OnLoaded -= Combine;
    }

    private void Start()
    {
        // 로더가 없으면(고정 씬) 바로 시도
        if (loader == null) Combine();
    }

    public void Combine()
    {
        Transform root = container != null ? container
                       : (loader != null ? loader.transform : null);
        if (root == null)
        {
            if (verboseLog) Debug.LogWarning("[정적배칭] 대상 부모를 찾지 못함");
            return;
        }

        var targets = new List<GameObject>();

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            GameObject go = renderers[i].gameObject;

            // 움직이는 연출이 붙은 것은 제외 (결합하면 흔들림이 안 보임)
            if (excludeAnimated && go.GetComponentInParent<HitFeedback>() != null) continue;

            targets.Add(go);
        }

        if (targets.Count == 0)
        {
            if (verboseLog) Debug.Log("[정적배칭] 묶을 대상 없음");
            return;
        }

        StaticBatchingUtility.Combine(targets.ToArray(), root.gameObject);

        if (verboseLog) Debug.Log($"[정적배칭] {targets.Count}개 결합 완료");
    }
}
