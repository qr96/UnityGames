using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 패널 열고 닫기 관리.
/// HUD의 인벤토리/상점 버튼에서 호출.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("패널")]
    [SerializeField] GameObject _inventoryPanel;
    [SerializeField] GameObject _shopPanel;

    [Header("HUD 버튼")]
    [SerializeField] Button _inventoryButton;
    [SerializeField] Button _shopButton;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _inventoryButton?.onClick.AddListener(ToggleInventory);
        _shopButton?.onClick.AddListener(ToggleShop);

        // 시작 시 패널 닫기
        _inventoryPanel?.SetActive(false);
        _shopPanel?.SetActive(false);
    }

    public void ToggleInventory() => Toggle(_inventoryPanel);
    public void ToggleShop() => Toggle(_shopPanel);

    void Toggle(GameObject panel)
    {
        if (panel == null) return;
        panel.SetActive(!panel.activeSelf);
    }
}