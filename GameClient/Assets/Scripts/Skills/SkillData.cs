using UnityEngine;

namespace GameDefine
{
    [CreateAssetMenu(fileName = "NewSkill", menuName = "Battle/SkillData/NormalSkill")]
    public class SkillData : ScriptableObject
    {
        public int skillId;
        public string skillName;
        public Sprite icon;
        public SkillEffectLogic[] effectLogics; // 실행될 로직 (SO)
    }
}
