using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스킬 카드 하나의 표시와 클릭을 담당. 프리팹으로 만들어두면 부모 UI가 동적 생성.
/// </summary>
public class SkillCard : MonoBehaviour
{
    [Header("References")]
    public Button button;
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text descText;

    private SkillInstance instance;
    private Action<SkillInstance> onClicked;

    void Awake()
    {
        if (button != null) button.onClick.AddListener(HandleClick);
    }

    public void Setup(SkillInstance inst, Action<SkillInstance> onClick)
    {
        instance = inst;
        onClicked = onClick;

        var def = inst.definition;

        if (iconImage != null)
        {
            iconImage.sprite = def.icon;
            // 아이콘이 없으면 슬롯 자체를 숨김 (디자인 선택)
            iconImage.enabled = def.icon != null;
        }

        if (nameText != null)
        {
            nameText.text = inst.stack > 0
                ? $"{def.skillName} (Lv.{inst.stack + 1})"
                : def.skillName;
        }

        if (descText != null) descText.text = def.description;
    }

    void HandleClick()
    {
        onClicked?.Invoke(instance);
    }
}
