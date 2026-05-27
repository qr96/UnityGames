using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬 구성 가이드:
/// - Canvas 아래 패널 하나 (root에 할당)
/// - 자식으로 카드 3개: 각 카드는 Button + Image icon + TMP_Text nameText + TMP_Text descText
/// </summary>
public class SkillSelectionUI : MonoBehaviour
{
    [Serializable]
    public class CardUI
    {
        public Button button;
        public Image iconImage;
        public TMP_Text nameText;
        public TMP_Text descText;
    }

    public GameObject root;
    public List<CardUI> cardSlots = new List<CardUI>();

    private Action<SkillInstance> currentCallback;

    void Awake()
    {
        if (root != null) root.SetActive(false);
    }

    public void Show(List<SkillInstance> choices, Action<SkillInstance> onChosen)
    {
        currentCallback = onChosen;

        if (root != null) root.SetActive(true);

        for (int i = 0; i < cardSlots.Count; i++)
        {
            var slot = cardSlots[i];
            if (i < choices.Count)
            {
                var inst = choices[i];
                var def = inst.definition;

                slot.button.gameObject.SetActive(true);
                if (slot.iconImage != null) slot.iconImage.sprite = def.icon;
                if (slot.nameText != null) slot.nameText.text = def.skillName +
                    (inst.stack > 0 ? $" (Lv.{inst.stack + 1})" : "");
                if (slot.descText != null) slot.descText.text = def.description;

                slot.button.onClick.RemoveAllListeners();
                SkillInstance captured = inst;
                slot.button.onClick.AddListener(() => Choose(captured));
            }
            else
            {
                slot.button.gameObject.SetActive(false);
            }
        }

        Time.timeScale = 0f;
    }

    void Choose(SkillInstance inst)
    {
        Time.timeScale = 1f;
        if (root != null) root.SetActive(false);
        currentCallback?.Invoke(inst);
        currentCallback = null;
    }
}
