using UnityEngine;

public class SystemLoader : MonoBehaviour
{
    public static SystemLoader Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);

            // 자식들 초기화 (필요하다면 순서대로 호출 가능)
            // GetComponentInChildren<PoolManager>().Init();
            // GetComponentInChildren<SoundManager>().Init();
        }
        else
        {
            // 이미 시스템이 존재하면 통째로 파괴
            Destroy(this.gameObject);
        }
    }
}
