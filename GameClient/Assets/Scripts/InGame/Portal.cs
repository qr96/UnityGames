using UnityEngine;

namespace InGame
{
    public class Portal : MonoBehaviour
    {
        public string nextSceneName = "Stage_02";

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                // 씬 이동 요청 (로딩창 -> 청소 -> 이동 -> 로딩창 끄기 자동 수행)
                SceneLoader.Instance.LoadScene(nextSceneName);
            }
        }
    }
}

