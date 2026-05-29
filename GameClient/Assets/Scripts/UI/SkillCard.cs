using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레벨업 선택 카드. 패시브든 액티브든 ISelectableChoice면 다 표시 가능.
/// </summary>
public class SkillCard : MonoBehaviour
{
    [Header("References")]
    public Button button;
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text descText;

    private ISelectableChoice choice;
    private Action<ISelectableChoice> onClicked;

    void Awake()
    {
        if (button != null) button.onClick.AddListener(HandleClick);
    }

    public void Setup(ISelectableChoice c, Action<ISelectableChoice> onClick)
    {
        choice = c;
        onClicked = onClick;

        if (iconImage != null)
        {
            iconImage.sprite = c.Icon;
            iconImage.enabled = c.Icon != null;
        }

        if (nameText != null) nameText.text = c.DisplayName;
        if (descText != null) descText.text = c.Description;
    }

    void HandleClick() => onClicked?.Invoke(choice);
}