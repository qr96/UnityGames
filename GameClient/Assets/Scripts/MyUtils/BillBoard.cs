using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] bool lockX = true;  // X축 고정 (기울어짐 방지)
    [SerializeField] bool lockZ = true;  // Z축 고정

    Camera _cam;

    void Awake()
    {
        _cam = Camera.main;
    }

    void LateUpdate()
    {
        transform.LookAt(_cam.transform);

        var euler = transform.rotation.eulerAngles;
        if (lockX) euler.x = 0f;
        if (lockZ) euler.z = 0f;
        transform.rotation = Quaternion.Euler(euler);
    }
}
