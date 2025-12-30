using UnityEngine;

namespace InGame 
{
    public class GameUtil
    {
        public static Color GetTeamUnitColor(int teamId)
        {
            if (teamId == 2)
                return new Color(1f, 180f / 255f, 180f / 255f);
            else if (teamId == 3)
                return new Color(1f, 1f, 180f / 255f);

            return Color.white;
        }

        public static Color GetTeamFlagColor(int teamId)
        {
            if (teamId == 1)
                return Color.blue;
            else if (teamId == 2)
                return Color.red;
            else if (teamId == 3)
                return Color.yellow;

            return Color.white;
        }
    }
}
