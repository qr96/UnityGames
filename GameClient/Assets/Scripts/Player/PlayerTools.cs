using UnityEngine;

// 플레이어 보유 도구 플래그. 시험판은 도끼 유무만.
// 제작대(CraftingStation)가 제작 성공 시 GiveAxe() 호출. 테스트 땐 인스펙터에서 hasAxe 토글.
public class PlayerTools : MonoBehaviour
{
    [SerializeField] private bool hasAxe = false;

    public bool HasAxe => hasAxe;

    public void GiveAxe() => hasAxe = true;
}
