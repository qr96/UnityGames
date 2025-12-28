using InGameModel;
using System;
using UnityEngine;

namespace InGame
{
    public class CapturePoint : MonoBehaviour
    {
        public int OwnTeamId;
        public int foodProduction;

        public Action<CapturePoint, int, int> OnChangeOwner;

        public void ChangeOwner(int ownTeamId)
        {
            OnChangeOwner?.Invoke(this, OwnTeamId, ownTeamId);
            OwnTeamId = ownTeamId;
        }
    }
}
