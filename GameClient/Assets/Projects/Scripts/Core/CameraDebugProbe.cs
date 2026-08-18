using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// 카메라 진단용 임시 스크립트. 문제 잡히면 삭제할 것.
///
/// 아무 오브젝트에나 붙이면 1초마다 콘솔에 카메라 체인의 실제 상태를 찍는다.
/// 인스펙터로 하나씩 확인하는 대신 전부 한 번에 본다.
///
/// [세팅] 빈 오브젝트 또는 GameSystem에 붙이고 Play.
///        조준 상태로 들어간 뒤 콘솔에 찍힌 블록을 통째로 복사해서 줄 것.
/// </summary>
public class CameraDebugProbe : MonoBehaviour
{
    float timer;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = 1f;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("========== CAMERA PROBE ==========");

        // 1. Brain
        var brain = Camera.main ? Camera.main.GetComponent<CinemachineBrain>() : null;
        if (!brain)
        {
            sb.AppendLine("[FATAL] Main Camera 또는 CinemachineBrain이 없다");
            Debug.Log(sb.ToString());
            return;
        }

        sb.AppendLine($"Brain 활성 카메라: {brain.ActiveVirtualCamera?.Name ?? "(없음)"}");
        sb.AppendLine($"블렌드 중: {brain.IsBlending}");
        sb.AppendLine($"MainCamera 위치: {Camera.main.transform.position:F2}  회전: {Camera.main.transform.eulerAngles:F1}");

        // 2. 씬의 모든 Cinemachine 카메라
        var cams = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
        sb.AppendLine($"--- CinemachineCamera {cams.Length}개 ---");

        foreach (var cam in cams)
        {
            sb.AppendLine($"[{cam.name}]");
            sb.AppendLine($"  활성: {cam.gameObject.activeInHierarchy} / Priority: {cam.Priority.Value} / Live: {CinemachineCore.IsLive(cam)}");
            sb.AppendLine($"  Tracking Target: {(cam.Follow ? cam.Follow.name : "(비어있음!)")}");
            sb.AppendLine($"  카메라 위치: {cam.transform.position:F2}  회전: {cam.transform.eulerAngles:F1}");

            var panTilt = cam.GetComponent<CinemachinePanTilt>();
            if (panTilt)
                sb.AppendLine($"  PanTilt: Pan={panTilt.PanAxis.Value:F1}  Tilt={panTilt.TiltAxis.Value:F1}");

            var tpf = cam.GetComponent<CinemachineThirdPersonFollow>();
            if (tpf)
                sb.AppendLine($"  ThirdPersonFollow: Distance={tpf.CameraDistance:F1}");

            var orbital = cam.GetComponent<CinemachineOrbitalFollow>();
            if (orbital)
                sb.AppendLine($"  OrbitalFollow 있음 (FreeLook 계열)");
        }

        Debug.Log(sb.ToString());
    }
}
