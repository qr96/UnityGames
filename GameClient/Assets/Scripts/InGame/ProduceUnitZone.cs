using InGame;
using UnityEngine;

namespace InGame
{
    public class ProduceUnitZone : MonoBehaviour, IEventZone
    {
        public SpriteRenderer sr;
        public CapturePoint point;

        LeaderUnit nowLeader;

        void Start()
        {
            point.OnChangeOwner += OnChangeOwner;
        }

        void OnDestroy()
        {
            point.OnChangeOwner -= OnChangeOwner;
        }

        void Update()
        {
            if (nowLeader != null)
            {
                if (nowLeader.IsStop())
                    nowLeader.SetEventZone(this);
            }
        }

        void OnTriggerEnter2D(Collider2D collision)
        {
            var leader = collision.GetComponent<LeaderUnit>();
            if (leader == null)
                return;

            if (leader.TeamId == point.OwnTeamId)
            {
                sr.transform.localScale = Vector3.one * 0.9f;
                nowLeader = leader;
            }
        }

        void OnTriggerExit2D(Collider2D collision)
        {
            var leader = collision.GetComponent<LeaderUnit>();
            if (leader == null)
                return;

            if (leader.TeamId == point.OwnTeamId)
            {
                sr.transform.localScale = Vector3.one;
                nowLeader = null;
            }
        }

        public void ExecuteEvent(int index)
        {
            if (nowLeader == null)
                return;

            var teamId = nowLeader.TeamId;

            if (index == 1)
            {
                FieldManager.Instance.TryProduceUnit(teamId, 0, point.transform.position);
            }
        }

        void OnChangeOwner(CapturePoint point, int prevTeamId, int nowTeamId)
        {
            if (nowLeader == null)
                return;

            if (nowLeader.TeamId != nowTeamId)
                nowLeader.SetEventZone(null);
        }
    }
}
