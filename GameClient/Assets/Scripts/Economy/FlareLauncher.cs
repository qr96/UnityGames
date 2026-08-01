using System;
using System.Collections;
using UnityEngine;

// 신호탄. E로 골드 소비(FlareConfig 가격, 매회 상승) → 발사 연출 → 도착 시 주민 프리팹 스폰.
// 주민의 실제 행동/배정은 뒤 마디. 여기선 스폰 + OnResidentArrived 이벤트까지만.
public class FlareLauncher : InteractableBase
{
    [SerializeField] private FlareConfig config;
    [SerializeField] private Inventory inventory; // 비우면 씬에서 찾음

    [Header("발사 연출 (임시)")]
    [SerializeField] private GameObject flareProjectilePrefab; // 위로 솟는 신호탄(선택)
    [SerializeField] private Transform launchPoint;            // 발사 시작점(비우면 자기 위치)
    [SerializeField] private float riseHeight = 12f;
    [SerializeField] private float riseTime = 0.6f;
    [SerializeField] private float arrivalDelay = 1.5f;        // 발사 → 도착까지

    [Header("도착")]
    [SerializeField] private GameObject residentPrefab;        // 무직 주민 프리팹(임시 배치)
    [SerializeField] private Transform arrivalPoint;           // 도착 위치(비우면 자기 위치)

    public event Action<GameObject> OnResidentArrived;

    private int purchaseCount;
    private bool busy;

    public int CurrentPrice => config != null ? config.PriceForPurchaseIndex(purchaseCount) : 0;

    private void Start()
    {
        if (inventory == null) inventory = FindObjectOfType<Inventory>();
    }

    public override string Prompt
    {
        get
        {
            if (busy) return "신호탄 발사 중…";
            if (config == null || inventory == null) return "신호탄";
            return $"신호탄 [골드 {inventory.Gold}/{CurrentPrice}]";
        }
    }

    // 발사 중이 아니면 타겟은 되게(가격 표시). 골드 부족은 Interact에서 판정.
    public override bool CanInteract(GameObject interactor)
        => !busy && config != null && inventory != null;

    public override void Interact(GameObject interactor)
    {
        if (busy || config == null || inventory == null) return;

        int price = CurrentPrice;
        if (!inventory.TrySpendGold(price))
        {
            Debug.Log("[신호탄] 골드 부족");
            return;
        }

        purchaseCount++; // 다음 가격 상승
        StartCoroutine(LaunchRoutine());
    }

    private IEnumerator LaunchRoutine()
    {
        busy = true;

        GameObject flare = null;
        Vector3 start = launchPoint != null ? launchPoint.position : transform.position;
        if (flareProjectilePrefab != null)
            flare = Instantiate(flareProjectilePrefab, start, Quaternion.identity);

        float t = 0f;
        while (t < arrivalDelay)
        {
            t += Time.deltaTime;
            if (flare != null)
            {
                float k = Mathf.Clamp01(t / Mathf.Max(0.01f, riseTime));
                flare.transform.position = start + Vector3.up * (riseHeight * k);
            }
            yield return null;
        }

        if (flare != null) Destroy(flare);

        Vector3 arrivePos = arrivalPoint != null ? arrivalPoint.position : transform.position;
        GameObject resident = null;
        if (residentPrefab != null)
            resident = Instantiate(residentPrefab, arrivePos, Quaternion.identity);
        else
            Debug.Log("[신호탄] 주민 도착 (Resident Prefab 미지정)");

        OnResidentArrived?.Invoke(resident);
        busy = false;
    }
}
