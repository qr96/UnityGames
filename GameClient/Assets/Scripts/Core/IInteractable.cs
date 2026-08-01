using UnityEngine;

// 근처에서 키 입력으로 상호작용하는 대상 공통 계약.
// 줍기(StickPickup), 벌목/채집 노드, 제작대, 막사 배정이 모두 이 인터페이스로 통일됨.
public interface IInteractable
{
    // 표시용 짧은 라벨 (예: "줍기", "패기", "배정")
    string Prompt { get; }

    // 지금 상호작용 가능한지 (쿨타임 노드, 자원 부족 등 판정)
    bool CanInteract(GameObject interactor);

    // 상호작용 실행. 주체(플레이어) 전달.
    void Interact(GameObject interactor);
}
