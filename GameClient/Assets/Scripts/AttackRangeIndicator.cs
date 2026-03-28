using UnityEngine;

public class AttackRangeIndicator : MonoBehaviour
{
    [Header("링 설정")]
    public float radius = 2f;
    public int thickness = 4;        // 픽셀 두께
    public int textureSize = 256;
    public Color ringColor = new Color(1f, 1f, 1f, 0.5f);

    MeshRenderer _renderer;
    MaterialPropertyBlock _block;

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _block = new MaterialPropertyBlock();

        var tex = CreateRingTexture(textureSize, thickness);
        _renderer.GetPropertyBlock(_block);
        _block.SetTexture("_BaseMap", tex);
        _renderer.SetPropertyBlock(_block);
    }

    Texture2D CreateRingTexture(int size, int thick)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var center = size / 2f;
        var outer = center;
        var inner = center - thick;

        for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                bool isRing = dist <= outer && dist >= inner;
                tex.SetPixel(x, y, isRing ? ringColor : Color.clear);
            }

        tex.Apply();
        return tex;
    }

    // 공격 범위 바뀔 때 크기 업데이트
    public void SetRadius(float r)
    {
        radius = r;
        transform.localScale = new Vector3(r * 2f, r * 2f, 1f);
    }
}