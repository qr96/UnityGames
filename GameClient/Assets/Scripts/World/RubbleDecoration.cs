using UnityEngine;

// 장식용 무더기(바위 무더기·눈더미). 순수 시각 — 충돌·상호작용·격자 점유 없음.
// 프리팹(빈 오브젝트)에 붙이면 시작할 때 작은 조각들을 무리지어 만든다.
// 위치를 시드로 쓰므로 같은 맵에서는 항상 같은 모양이 나온다.
public class RubbleDecoration : MonoBehaviour
{
    public enum Kind { Rock, Snow }

    [SerializeField] private Kind kind = Kind.Rock;

    [Header("무더기")]
    [SerializeField] private int minPieces = 3;
    [SerializeField] private int maxPieces = 6;
    [Tooltip("퍼지는 반경(칸 크기보다 작게)")]
    [SerializeField] private float spread = 0.35f;
    [SerializeField] private Vector2 pieceScale = new Vector2(0.12f, 0.3f);

    [Header("색")]
    [SerializeField] private Color rockColor = new Color(0.45f, 0.45f, 0.48f);
    [SerializeField] private Color snowColor = new Color(0.92f, 0.95f, 1f);

    private void Start() => Build();

    private void Build()
    {
        // 위치 기반 시드 — 맵이 같으면 모양도 같다
        Vector3 p = transform.position;
        int seed = Mathf.RoundToInt(p.x * 73856093f) ^ Mathf.RoundToInt(p.z * 19349663f);
        var rng = new System.Random(seed);

        int count = Mathf.Max(1, minPieces + rng.Next(0, Mathf.Max(1, maxPieces - minPieces + 1)));
        Color color = kind == Kind.Rock ? rockColor : snowColor;

        for (int i = 0; i < count; i++)
        {
            GameObject piece = GameObject.CreatePrimitive(
                kind == Kind.Rock ? PrimitiveType.Cube : PrimitiveType.Sphere);
            piece.name = $"piece_{i}";
            piece.transform.SetParent(transform, false);

            // 충돌 없음 — 순수 시각
            Collider col = piece.GetComponent<Collider>();
            if (col != null) Destroy(col);

            float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
            float dist = (float)rng.NextDouble() * spread;
            float scale = Mathf.Lerp(pieceScale.x, pieceScale.y, (float)rng.NextDouble());

            piece.transform.localPosition = new Vector3(
                Mathf.Cos(ang) * dist, scale * 0.35f, Mathf.Sin(ang) * dist);
            piece.transform.localRotation = Quaternion.Euler(
                (float)rng.NextDouble() * 30f,
                (float)rng.NextDouble() * 360f,
                (float)rng.NextDouble() * 30f);
            piece.transform.localScale = new Vector3(scale, scale * 0.7f, scale);

            Renderer r = piece.GetComponent<Renderer>();
            Material m = r.material;
            float shade = 0.85f + (float)rng.NextDouble() * 0.3f;
            Color c = color * shade; c.a = 1f;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color"))     m.SetColor("_Color", c);

            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
