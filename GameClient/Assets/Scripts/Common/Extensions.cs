using GameDefine;
using System;
using System.Collections.Generic;
using UnityEngine;

public static class Extensions
{
    public static string GetCardInfo(this List<SkillCardData> cards)
    {
        var cardLog = "";

        foreach (var card in cards)
        {
            cardLog += $"[{card.skillId}, {card.rank}]";
        }

        return cardLog;
    }
}
