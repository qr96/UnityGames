using UnityEngine;

namespace InGame
{
    public class SpumSpriter : MonoBehaviour
    {
        public SPUM_MatchingList match;

        public void SetColor(Color color)
        {
            foreach (var element in match.matchingTables)
            {
                var partType = element.PartType;
                if (partType != "Hair" && partType != "Eye" && partType != "FaceHair")
                {
                    element.renderer.color = color;
                }
            }
        }
    }
}
