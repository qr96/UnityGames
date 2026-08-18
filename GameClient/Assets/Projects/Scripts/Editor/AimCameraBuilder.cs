using UnityEngine;
using UnityEditor;
using Unity.Cinemachine;

/// <summary>
/// 조준 카메라를 코드로 생성한다. 메뉴: Tools > Camera > Build Aim Camera
///
/// 인스펙터에서 수동으로 조립하다 파이프라인 등록이 꼬이는 문제를 피하기 위해
/// 필요한 컴포넌트를 정확한 순서와 설정으로 한 번에 만든다.
///
/// 생성 구성:
///   CinemachineCamera (Priority 10)
///   + ThirdPersonFollow  (위치: 어깨너머)
///   + PanTilt            (회전: 마우스)
///   + InputAxisController (마우스 → PanTilt 연결, 블렌드 중에도 입력 허용)
///
/// 실행 후 할 일: AimCameraRig의 Aim Camera 슬롯에 새 카메라를 꽂을 것.
/// </summary>
public static class AimCameraBuilder
{
    const string CameraName = "AimCamera (Built)";

    [MenuItem("Tools/Camera/Build Aim Camera")]
    public static void Build()
    {
        // 기존 빌드 결과물이 있으면 제거
        var old = GameObject.Find(CameraName);
        if (old) Object.DestroyImmediate(old);

        // 타겟 확인
        var target = Object.FindFirstObjectByType<CameraFollowTarget>();
        if (!target)
        {
            EditorUtility.DisplayDialog("타겟 없음",
                "씬에 CameraFollowTarget이 없다.\n" +
                "씬 루트의 빈 오브젝트에 CameraFollowTarget을 붙이고 다시 실행해라.", "확인");
            return;
        }

        var go = new GameObject(CameraName);
        Undo.RegisterCreatedObjectUndo(go, "Build Aim Camera");

        // 1. 본체
        var cam = go.AddComponent<CinemachineCamera>();
        cam.Priority = 10;
        cam.Follow = target.transform;

        // 2. 위치: 어깨너머
        var follow = go.AddComponent<CinemachineThirdPersonFollow>();
        follow.ShoulderOffset = new Vector3(0.5f, 0.25f, 0f);
        follow.VerticalArmLength = 0.4f;
        follow.CameraDistance = 3.0f;
        follow.Damping = new Vector3(0.1f, 0.25f, 0.3f);

        // 3. 회전: 마우스
        var panTilt = go.AddComponent<CinemachinePanTilt>();
        panTilt.PanAxis.Range = new Vector2(-180f, 180f);
        panTilt.PanAxis.Wrap = true;
        panTilt.TiltAxis.Range = new Vector2(-70f, 70f);
        panTilt.ReferenceFrame = CinemachinePanTilt.ReferenceFrames.World;

        // 4. 입력 연결. 컴포넌트 추가 시점에 PanTilt가 이미 있으므로 정상 인식된다
        var axisController = go.AddComponent<CinemachineInputAxisController>();
        axisController.SuppressInputWhileBlending = false;   // 블렌드 중에도 조준 가능

        Selection.activeGameObject = go;

        Debug.Log(
            $"[AimCameraBuilder] '{CameraName}' 생성 완료.\n" +
            $"남은 일: AimCameraRig의 Aim Camera 슬롯에 이 카메라를 꽂아라.\n" +
            $"기존 Third Person Aim Camera는 삭제해도 된다.");
    }
}