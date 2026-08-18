using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

/// <summary>
/// 1인칭 조준 카메라를 코드로 생성한다.
/// 메뉴: Tools > Camera > Build First Person Aim Camera
///
/// 생성 구성:
///   EyeTarget (Player 자식, 눈높이 고정점)
///   FPAimCamera (Built)
///     CinemachineCamera (Priority 10, Tracking Target = EyeTarget)
///     + HardLockToTarget   (위치: 타겟 위치에 고정 — 이게 없으면
///                           카메라가 타겟을 따라가지 않고 원점에 박힌다)
///     + PanTilt            (회전: 마우스)
///     + InputAxisController (마우스 → PanTilt 연결)
///
/// [실행 후 할 일]
/// 1. AimCameraRig의 Aim Camera 슬롯에 'FPAimCamera (Built)'를 꽂는다
/// 2. InputAxisController의 Pan/Tilt에 CM Default/Look이 비어 있으면 꽂는다
/// 3. Player에 FirstPersonBodyHider를 붙이고 캡슐 렌더러를 꽂는다
/// </summary>
public static class FirstPersonAimCameraBuilder
{
    const string CameraName = "FPAimCamera (Built)";
    const string EyeTargetName = "EyeTarget";

    [MenuItem("Tools/Camera/Build First Person Aim Camera")]
    public static void Build()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (!player)
        {
            EditorUtility.DisplayDialog("Player 없음",
                "'Player' 태그를 가진 오브젝트가 씬에 없다.", "확인");
            return;
        }

        var oldCam = GameObject.Find(CameraName);
        if (oldCam) Object.DestroyImmediate(oldCam);

        // ── 1. 눈높이 타겟 (Player 자식) ──
        var eye = player.transform.Find(EyeTargetName);
        if (!eye)
        {
            var eyeGo = new GameObject(EyeTargetName);
            Undo.RegisterCreatedObjectUndo(eyeGo, "Create Eye Target");
            eyeGo.transform.SetParent(player.transform, false);
            eyeGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            eye = eyeGo.transform;
        }

        // ── 2. 카메라 ──
        var go = new GameObject(CameraName);
        Undo.RegisterCreatedObjectUndo(go, "Build FP Aim Camera");

        var cam = go.AddComponent<CinemachineCamera>();
        cam.Priority = 10;
        cam.Follow = eye;

        // 위치: 타겟 위치에 고정. 이 컴포넌트가 빠지면
        // Follow가 있어도 카메라는 스폰 위치에 그대로 남는다
        go.AddComponent<CinemachineHardLockToTarget>();

        // 회전: 마우스
        var panTilt = go.AddComponent<CinemachinePanTilt>();
        panTilt.PanAxis.Range = new Vector2(-180f, 180f);
        panTilt.PanAxis.Wrap = true;
        panTilt.TiltAxis.Range = new Vector2(-80f, 80f);
        panTilt.ReferenceFrame = CinemachinePanTilt.ReferenceFrames.World;

        // 입력 연결
        var axisController = go.AddComponent<CinemachineInputAxisController>();
        axisController.SuppressInputWhileBlending = false;

        Selection.activeGameObject = go;

        Debug.Log(
            $"[FPAimCameraBuilder] '{CameraName}' 생성 완료.\n" +
            $"남은 일:\n" +
            $"1. AimCameraRig의 Aim Camera 슬롯에 이 카메라를 꽂아라\n" +
            $"2. InputAxisController의 Pan/Tilt에 CM Default/Look이 비어 있으면 꽂아라\n" +
            $"3. Player에 FirstPersonBodyHider + 캡슐 렌더러 연결");
    }
}