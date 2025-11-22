using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [Header("UI References")]
    [Tooltip("로딩 화면 전체를 감싸는 Canvas Group (페이드 효과용)")]
    [SerializeField] private CanvasGroup loadingCanvasGroup;

    [Header("Settings")]
    [Tooltip("최소 로딩 시간 (깜빡임 방지)")]
    [SerializeField] private float minLoadingTime = 1.0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(this.gameObject);

            // 초기에는 로딩 화면 숨기기
            if (loadingCanvasGroup != null)
            {
                loadingCanvasGroup.alpha = 0f;
                loadingCanvasGroup.blocksRaycasts = false;
            }
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// 외부에서 씬 이동을 요청하는 함수
    /// </summary>
    /// <param name="sceneName">이동할 씬 이름</param>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneProcess(sceneName));
    }

    private IEnumerator LoadSceneProcess(string sceneName)
    {
        // ---------------------------------------------------------
        // 1. 로딩 화면 등장 (Fade In)
        // ---------------------------------------------------------
        loadingCanvasGroup.blocksRaycasts = true; // 터치 방지
        yield return StartCoroutine(Fade(1f));

        // ---------------------------------------------------------
        // 2. 메모리 대청소 (Golden Time)
        // ---------------------------------------------------------
        // A. 풀 매니저 정리 (C++ 네이티브 객체 파괴, C# 참조 끊기)
        // OnSceneLoaded에서도 호출되지만, 메모리 피크를 막기 위해 미리 비우는 것이 안전함
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.OnSceneLoaded();
        }

        // B. 사용하지 않는 에셋(텍스처, 오디오 등) 메모리 해제
        yield return Resources.UnloadUnusedAssets();

        // C. C# 가비지 컬렉터 강제 수행 (껍데기 객체 수거)
        System.GC.Collect();

        // GC 안정화를 위해 아주 잠깐 대기
        yield return new WaitForSeconds(0.1f);

        // ---------------------------------------------------------
        // 3. 비동기 씬 로드 시작
        // ---------------------------------------------------------
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

        // 씬 로드가 90% 되면 멈추고(activation false), 남은 처리를 기다리게 함
        op.allowSceneActivation = false;

        var timer = 0f;

        while (!op.isDone)
        {
            yield return null;
            timer += Time.deltaTime;

            // 씬 로딩 진행률 (0.0 ~ 0.9)
            // op.progress는 최대 0.9까지만 오름
            float progress = op.progress;

            if (progress < 0.9f)
            {
                if (progress >= 0.9f) 
                    timer = 0f; // 90% 도달 시 타이머 리셋
            }
            else
            {
                if (timer >= minLoadingTime)
                {
                    op.allowSceneActivation = true;
                    yield break;
                }
            }
        }
    }

    // 씬 전환 후(Start 등)에 호출하여 로딩창을 끄는 함수
    // SceneManager.sceneLoaded 이벤트에 연결해도 됨
    public void LoadComplete()
    {
        StartCoroutine(Fade(0f));
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = loadingCanvasGroup.alpha;
        float time = 0f;
        float duration = 0.5f;

        while (time < duration)
        {
            time += Time.deltaTime;
            loadingCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }
        loadingCanvasGroup.alpha = targetAlpha;

        // 페이드 아웃(0)이 끝났으면 클릭 허용 해제
        if (targetAlpha == 0f)
        {
            loadingCanvasGroup.blocksRaycasts = false;
        }
    }

    // 씬이 로드될 때 자동으로 페이드 아웃 시키려면 아래 코드 추가
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 새 씬이 열리면 로딩창 끄기
        LoadComplete();
    }
}
