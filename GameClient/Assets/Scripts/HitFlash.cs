using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 피격 순간 적의 렌더러를 잠깐 단색으로 바꿔 번쩍이게 한다.
/// 적 프리팹에 붙이고 데미지 처리부에서 Flash() 호출이면 끝.
/// 렌더러 선택·머터리얼 생성은 알아서 처리한다(설정 0개로 동작).
/// 풀 매니저는 쓰지 않는다 — 적 자신의 머터리얼만 잠깐 교체했다 되돌린다.
/// </summary>
public class HitFlash : MonoBehaviour
{
    [Tooltip("번쩍이는 색. 비워두면 흰색. 빨강으로 바꾸고 싶으면 여기만 바꾸면 됨.")]
    public Color flashColor = Color.white;

    [Tooltip("번쩍이는 시간(초). 짧을수록 톡 친 느낌.")]
    public float flashDuration = 0.06f;

    [Tooltip("직접 지정하고 싶을 때만 사용. 비워두면 Mesh/SkinnedMesh 렌더러를 자동으로 모은다(파티클·트레일은 자동 제외).")]
    public Renderer[] renderers;

    [Tooltip("직접 만든 머터리얼을 쓰고 싶을 때만. 비워두면 단색 머터리얼을 자동 생성.")]
    public Material flashMaterial;

    private Material[][] originals;
    private Material[][] flashSets;
    private float timer;
    private bool flashing;

    void Awake()
    {
        // 렌더러 자동 수집: 번쩍여야 할 표면만 잡고 파티클/트레일/라인은 제외
        if (renderers == null || renderers.Length == 0)
        {
            var found = GetComponentsInChildren<Renderer>();
            var list = new List<Renderer>(found.Length);
            foreach (var r in found)
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                    list.Add(r);
            renderers = list.ToArray();
        }

        // 머터리얼 자동 생성: 렌더 파이프라인에 맞는 Unlit 셰이더를 찾아 단색 머터리얼 제작
        if (flashMaterial == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Unlit/Color");
            if (s == null) s = Shader.Find("Sprites/Default");
            if (s != null)
            {
                flashMaterial = new Material(s);
                flashMaterial.color = flashColor;
                if (flashMaterial.HasProperty("_BaseColor"))
                    flashMaterial.SetColor("_BaseColor", flashColor);
            }
        }

        // 배열 1회 캐싱 → 피격 때마다 할당/GC 없음
        originals = new Material[renderers.Length][];
        flashSets = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originals[i] = renderers[i].sharedMaterials;
            var set = new Material[originals[i].Length];
            for (int sIdx = 0; sIdx < set.Length; sIdx++) set[sIdx] = flashMaterial;
            flashSets[i] = set;
        }
    }

    /// <summary>피격 순간 호출. 연타 시 시간만 갱신된다.</summary>
    public void Flash()
    {
        if (flashMaterial == null) return;

        if (!flashing)
        {
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].sharedMaterials = flashSets[i];
            flashing = true;
        }
        timer = flashDuration;
    }

    void Update()
    {
        if (!flashing) return;

        timer -= Time.deltaTime;
        if (timer <= 0f) Restore();
    }

    void Restore()
    {
        for (int i = 0; i < renderers.Length; i++)
            renderers[i].sharedMaterials = originals[i];
        flashing = false;
    }

    void OnDisable()
    {
        if (flashing) Restore(); // 풀로 반환될 때 번쩍이던 상태로 남지 않게
    }
}