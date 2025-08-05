using UnityEngine;

public class CameraSwitcherToParentView : MonoBehaviour
{
    // PLC 데이터를 관리하는 Manager1 스크립트 참조 (Inspector에서 할당 필수)
    public Manager1 manager1;

    // 제어할 메인 카메라 참조 (Inspector에서 할당 필수)
    public GameObject mainCameraGameObject; // 메인 카메라 GameObject 자체를 할당

    // 이 카메라가 "켜질 때" 따라갈 부모 Transform (Inspector에서 할당 필수)
    // 이 Transform의 현재 위치/회전으로 카메라가 전환됩니다.
    public Transform targetViewParent;

    // 이 스크립트가 붙은 카메라의 Camera 컴포넌트
    private Camera thisCamera;

    // 메인 카메라의 Camera 컴포넌트
    private Camera mainCameraComponent;

    // 이 카메라의 초기 월드 위치와 회전 (메인 카메라로 돌아갈 때 되돌릴 위치)
    private Vector3 initialThisCameraWorldPosition;
    private Quaternion initialThisCameraWorldRotation;

    void Awake()
    {
        // 스크립트 시작 시 이 게임 오브젝트의 Camera 컴포넌트를 가져옵니다.
        thisCamera = GetComponent<Camera>();
        if (thisCamera == null)
        {
            Debug.LogError("CameraSwitcherToParentView 스크립트는 Camera 컴포넌트가 있는 게임 오브젝트에 붙어야 합니다.");
            enabled = false; // 스크립트 비활성화
            return;
        }

        // 메인 카메라 GameObject가 할당되었는지 확인하고 컴포넌트 가져오기
        if (mainCameraGameObject != null)
        {
            mainCameraComponent = mainCameraGameObject.GetComponent<Camera>();
            if (mainCameraComponent == null)
            {
                Debug.LogError("할당된 Main Camera GameObject에 Camera 컴포넌트가 없습니다.");
                enabled = false;
                return;
            }
        }
        else
        {
            Debug.LogError("Main Camera GameObject가 할당되지 않았습니다. CameraSwitcherToParentView 스크립트가 작동하지 않습니다.");
            enabled = false;
            return;
        }

        // targetViewParent가 할당되었는지 확인합니다.
        if (targetViewParent == null)
        {
            Debug.LogError("Target View Parent Transform이 할당되지 않았습니다. 카메라가 따라갈 부모를 지정해주세요.");
            enabled = false;
            return;
        }

        // 이 카메라의 초기 월드 위치와 회전을 저장합니다.
        // PLC 신호가 꺼져서 메인 카메라로 돌아갈 때, 이 카메라를 원래 위치로 되돌립니다.
        initialThisCameraWorldPosition = transform.position;
        initialThisCameraWorldRotation = transform.rotation;

        // 시작할 때 이 카메라를 비활성화 상태로, 메인 카메라를 활성화 상태로 둡니다.
        thisCamera.enabled = false;
        mainCameraComponent.enabled = true;
    }

    void Update()
    {
        // Manager1이 할당되지 않았다면 오류를 기록하고 함수를 종료합니다.
        if (manager1 == null)
        {
            Debug.LogError("Manager1이 할당되지 않았습니다. CameraSwitcherToParentView 스크립트가 작동하지 않습니다.");
            return;
        }

        // Manager1에서 Y10과 Y11의 현재 상태를 가져옵니다.
        bool isY10On = manager1.currentY101State;
        bool isY11On = manager1.currentY111State;

        // PLC 신호에 따라 카메라 활성화/비활성화 및 위치 조정 로직
        if (isY10On || isY11On) // Y10 또는 Y11 중 하나라도 켜진 경우
        {
            // 이 카메라를 활성화하고 메인 카메라를 비활성화합니다.
            if (!thisCamera.enabled) // 현재 이 카메라가 꺼져 있다면 (처음 활성화되는 순간)
            {
                thisCamera.enabled = true; // 이 카메라를 웁니다.
                if (mainCameraComponent.enabled)
                {
                    mainCameraComponent.enabled = false; // 메인 카메라를 끕니다.
                }
                Debug.Log($"PLC Y10({isY10On})/Y11({isY11On}) 신호 감지: 현재 카메라 활성화, 메인 카메라 비활성화.");

                // 카메라의 위치와 회전을 targetViewParent의 현재 월드 위치/회전으로 설정합니다.
                transform.position = targetViewParent.position;
                transform.rotation = targetViewParent.rotation;
                Debug.Log($"카메라 위치를 '{targetViewParent.name}'의 뷰로 설정: {transform.position}, {transform.rotation.eulerAngles}");
            }
        }
        else // Y10과 Y11 모두 꺼진 경우
        {
            // 이 카메라를 비활성화하고 메인 카메라를 활성화합니다.
            if (thisCamera.enabled) // 현재 이 카메라가 켜져 있다면
            {
                thisCamera.enabled = false; // 이 카메라를 끕니다.
                if (!mainCameraComponent.enabled)
                {
                    mainCameraComponent.enabled = true; // 메인 카메라를 켭니다.
                }
                Debug.Log("PLC Y10/Y11 신호 없음: 현재 카메라 비활성화, 메인 카메라 활성화.");

                // 이 카메라를 원래의 초기 월드 위치로 되돌립니다.
                transform.position = initialThisCameraWorldPosition;
                transform.rotation = initialThisCameraWorldRotation;
                Debug.Log($"카메라를 초기 월드 위치로 되돌림: {initialThisCameraWorldPosition}, {initialThisCameraWorldRotation.eulerAngles}");
            }
        }
    }
}