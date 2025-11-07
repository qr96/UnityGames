using UnityEngine;
using UnityEngine.TextCore.Text;

public class CameraMover : MonoBehaviour
{
    public enum Mode
    {
        Immediately = 0,
        SmoothDamp = 1,
        Lerp = 2
    }

    public Transform target;
    public Vector3 cameraOffset;
    public float distance;
    public float smoothTime;
    public float lerp;
    public Mode mode;

    Vector3 velocity;

    private void LateUpdate()
    {
        var cameraPos = cameraOffset * distance;
        var targetPos = target.position + cameraPos;
        var calculatedPos = targetPos;

        if (mode == Mode.SmoothDamp)
            calculatedPos = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
        else if (mode == Mode.Lerp)
            calculatedPos = Vector3.Lerp(transform.position, targetPos, lerp);

        transform.position = calculatedPos;
    }

    public void MoveToTarget()
    {
        var targetPos = target.position + cameraOffset;
        transform.position = targetPos;
    }
}
