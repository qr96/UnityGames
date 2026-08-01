// 공용 자원 종류. 골드는 화폐라 Inventory에서 별도 필드로 관리.
public enum ResourceKind
{
    Stick,     // 나뭇가지 (줍기) — 도끼 제작 재료
    Firewood,  // 장작 (벌목)   — 화로 연료 / 판매
    Food,      // 식량·눈 속 열매 (채집) — 허기 회복 / 판매
}
