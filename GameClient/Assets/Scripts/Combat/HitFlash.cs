using UnityEngine;
using System.Collections;

public class HitFlash : MonoBehaviour
{
    [SerializeField] private Renderer[] _renderers;
    [SerializeField] private Color _flashColor = Color.white;
    [SerializeField, Range(0f, 1f)] private float _flashAmount = 0.5f;
    [SerializeField] private float _flashDuration = 0.15f;

    private MaterialPropertyBlock _mpb;
    private static readonly int FlashColorID = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");

    private Coroutine _flashRoutine;

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();

        if (_renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<Renderer>();

        // 색은 한 번만 세팅 (런타임 중 안 바뀌므로)
        foreach (var r in _renderers)
        {
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(FlashColorID, _flashColor);
            r.SetPropertyBlock(_mpb);
        }
    }

    public void Flash()
    {
        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine());
    }

    /// <summary>
    /// 진행 중인 번쩍임을 즉시 끄고 _FlashAmount를 0으로 되돌린다.
    /// 풀 재사용 시 흰색으로 굳은 상태를 막기 위해 OnSpawn에서 호출.
    /// </summary>
    public void ResetFlash()
    {
        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
            _flashRoutine = null;
        }
        SetFlashAmount(0f);
    }

    private IEnumerator FlashRoutine()
    {
        float t = 0f;
        while (t < _flashDuration)
        {
            t += Time.deltaTime;
            // 1 -> 0 으로 감쇠 (피격 순간 가장 강했다가 빠지는 형태)
            float amount = Mathf.Lerp(_flashAmount, 0f, t / _flashDuration);
            SetFlashAmount(amount);
            yield return null;
        }
        SetFlashAmount(0f);
        _flashRoutine = null;
    }

    private void SetFlashAmount(float amount)
    {
        foreach (var r in _renderers)
        {
            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat(FlashAmountID, amount);
            r.SetPropertyBlock(_mpb);
        }
    }
}