using UnityEngine;

public class CameraSizeFitter : MonoBehaviour
{
    public Vector2 baseRatio = new Vector2(9, 16);
    public float baseSize;

    Camera camera;

    private void OnEnable()
    {
        camera = GetComponent<Camera>();
        UpdateCameraSize();
    }

    public void UpdateCameraSize()
    {
        if (!IsValid())
            return;

        Debug.Log(camera.aspect);

        var ratio = baseRatio.x / baseRatio.y;
        var mul = ratio / camera.aspect;
        camera.orthographicSize = baseSize * mul;
    }

    bool IsValid()
    {
        if (camera == null)
        {
            Debug.LogError("Can't find a camera");
            return false;
        }
        if (!camera.orthographic)
        {
            Debug.LogError("This script is only for othographic");
            return false;
        }

        if (baseRatio.y == 0)
        {
            Debug.LogError("BaseRatio.y is 0");
            return false;
        }

        return true;
    }
}
