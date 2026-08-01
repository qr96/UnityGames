using UnityEngine;

// 타겟팅이 활성/비활성 시 호출하는 하이라이트 계약.
public interface IHighlightable
{
    void SetHighlighted(bool on);
}

// 외곽선 하이라이트. 활성 시 렌더러들에 외곽선 재질(인버티드 헐 패스)을 추가, 비활성 시 원복.
// 외곽선 재질을 지정하지 않으면 "Hokhan/Outline" 셰이더로 런타임 생성.
[DisallowMultipleComponent]
public class OutlineHighlighter : MonoBehaviour, IHighlightable
{
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField] private float outlineWidth = 0.03f;
    [SerializeField] private Material outlineMaterialOverride;

    private Renderer[] renderers;
    private Material[][] baseMaterials; // 렌더러별 원본 배열
    private Material outlineMat;
    private bool on;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        baseMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
            baseMaterials[i] = renderers[i].sharedMaterials;

        if (outlineMaterialOverride != null)
        {
            outlineMat = outlineMaterialOverride;
        }
        else
        {
            Shader s = Shader.Find("Hokhan/Outline");
            if (s != null)
            {
                outlineMat = new Material(s);
                ApplyProperties();
            }
            else
            {
                Debug.LogWarning("[OutlineHighlighter] 'Hokhan/Outline' 셰이더를 못 찾음. 외곽선 비활성.");
            }
        }
    }

    // 색/두께를 런타임 재질에 반영. 오버라이드 재질을 쓰면 건드리지 않음.
    private void ApplyProperties()
    {
        if (outlineMat == null || outlineMaterialOverride != null) return;
        outlineMat.SetColor("_OutlineColor", outlineColor);
        outlineMat.SetFloat("_OutlineWidth", outlineWidth);
    }

    private void OnValidate()
    {
        // 플레이 중 인스펙터에서 값 바꾸면 즉시 반영
        if (Application.isPlaying) ApplyProperties();
    }

    public void SetHighlighted(bool value)
    {
        if (on == value || outlineMat == null || renderers == null) return;
        on = value;

        if (value) ApplyProperties(); // 켤 때 최신 값 사용

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            if (value)
            {
                Material[] baseArr = baseMaterials[i];
                Material[] withOutline = new Material[baseArr.Length + 1];
                for (int m = 0; m < baseArr.Length; m++) withOutline[m] = baseArr[m];
                withOutline[baseArr.Length] = outlineMat;
                renderers[i].sharedMaterials = withOutline;
            }
            else
            {
                renderers[i].sharedMaterials = baseMaterials[i];
            }
        }
    }
}