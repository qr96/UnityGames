using UnityEngine;
using AutoBattler.Core;

namespace AutoBattler.Data
{
    [CreateAssetMenu(menuName = "AutoBattler/Equipment", fileName = "Equip_")]
    public class EquipmentData : ScriptableObject
    {
        public string id;
        public string displayName;
        public EquipmentSlot slot;
        public Stats statBonus;
    }
}
