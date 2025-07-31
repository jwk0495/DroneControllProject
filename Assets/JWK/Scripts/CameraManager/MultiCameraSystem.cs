using UnityEngine;
using System.Collections;
using JWK.Scripts.CameraManager;

namespace JWK.Scripts
{
    /// <summary>
    /// 메인 카메라와 2개의 서브 카메라를 제어하는 통합 카메라 시스템입니다.
    /// 드론의 상태 변화를 감지하여 지능적으로 카메라 뷰를 전환합니다.
    /// </summary>
    public class MultiCameraSystem : MonoBehaviour
    {
        [Header("카메라 할당")]
        public Camera MainCamera;
        public Camera FireCamera;
        public Camera StationCamera;
        public Camera ImpactCamera;

        [Header("핵심 타겟")]
        public Transform DroneTarget;
        public Transform StationTarget;

        [Header("메인 카메라 워크 설정")]
        [SerializeField] private Vector3 _followOffset = new Vector3(0, 5f, -10f);
        [SerializeField] private Vector3 _lookAtOffset = new Vector3(0, 1.5f, 0);
        [SerializeField] private float _followSmoothTime = 0.5f;
        [SerializeField] private float _rotationSmoothTime = 0.3f;
        [SerializeField] private float _orbitSpeed = 10f;

        [Header("서브 카메라 워크 설정")]
        [SerializeField] private Vector3 _fireCameraOffset = new Vector3(0, 40f, -20f);
        [SerializeField] private Vector3 _stationCameraOffset = new Vector3(0, 20f, -30f);

        [Header("임팩트 카메라 설정 (메커니즘 클로즈업)")]
        [Tooltip("드론의 중심점 기준 카메라의 로컬 위치입니다.")]
        [SerializeField] private Vector3 _mechanismViewLocalOffset = new Vector3(0.5f, -0.5f, -1f);
        [Tooltip("카메라의 로컬 회전 값입니다. (오일러 각)")]
        [SerializeField] private Vector3 _mechanismViewLocalRotation = new Vector3(15, -30, 0);
        [Tooltip("메커니즘 뷰가 보여지는 시간입니다.")]
        [SerializeField] private float _mechanismViewDuration = 7.0f;

        private enum CameraMode { Follow, Orbit, Idle }
        private CameraMode _currentMode = CameraMode.Idle;
        
        private Vector3 _orbitTargetPosition;
        private Transform _fireTarget;
        private Vector3 _cameraFollowVelocity = Vector3.zero;

        private void OnEnable()
        {
            DroneCameraEvents.OnMissionStart += HandleMissionStart;
            DroneCameraEvents.OnArrivedAtDropZone += HandleArrivedAtDropZone;
            DroneCameraEvents.OnReturnToStation += HandleReturnToStation;
        }

        private void OnDisable()
        {
            DroneCameraEvents.OnMissionStart -= HandleMissionStart;
            DroneCameraEvents.OnArrivedAtDropZone -= HandleArrivedAtDropZone;
            DroneCameraEvents.OnReturnToStation -= HandleReturnToStation;
        }

        private void Start()
        {
            SetupCameras();
            _currentMode = CameraMode.Follow;
        }
        
        private void LateUpdate()
        {
            if (DroneTarget == null) return;

            if(MainCamera.gameObject.activeInHierarchy)
            {
                switch (_currentMode)
                {
                    case CameraMode.Follow:
                        UpdateFollowCamera();
                        break;
                    case CameraMode.Orbit:
                        UpdateOrbitCamera();
                        break;
                }
            }
            UpdateSubCameras();
        }

        private void SetupCameras()
        {
            if (MainCamera != null)
            {
                MainCamera.rect = new Rect(0, 0, 1, 1);
                MainCamera.depth = 0;
            }
            if (StationCamera != null)
            {
                StationCamera.rect = new Rect(0.01f, 0.01f, 0.25f, 0.2f);
                StationCamera.depth = 1;
            }
            if (FireCamera != null)
            {
                FireCamera.rect = new Rect(1 - 0.25f - 0.01f, 0.01f, 0.25f, 0.2f);
                FireCamera.depth = 1;
                FireCamera.gameObject.SetActive(false);
            }
            if (ImpactCamera != null)
            {
                ImpactCamera.gameObject.SetActive(false);
                ImpactCamera.rect = new Rect(0, 0, 1, 1);
                ImpactCamera.depth = 0;
            }
        }

        // ====================================================================================
        // [수정] 이벤트 데이터 타입을 Vector3로 다시 변경하여 시스템 전체의 데이터 흐름을 통일합니다.
        private void HandleArrivedAtDropZone(Vector3 fireTargetPosition)
        {
            _orbitTargetPosition = fireTargetPosition;
            StartCoroutine(ShowMechanismThenFollow());
        }
        // ====================================================================================

        private IEnumerator ShowMechanismThenFollow()
        {
            _currentMode = CameraMode.Idle;
            yield return StartCoroutine(ShowMechanismView());
            
            if (DroneTarget != null && MainCamera != null)
            {
                Vector3 immediateTargetPosition = DroneTarget.position + _followOffset;
                Vector3 lookAtPoint = DroneTarget.position + _lookAtOffset;
                Quaternion immediateTargetRotation = Quaternion.LookRotation(lookAtPoint - immediateTargetPosition);

                MainCamera.transform.position = immediateTargetPosition;
                MainCamera.transform.rotation = immediateTargetRotation;

                _cameraFollowVelocity = Vector3.zero;
            }

            _currentMode = CameraMode.Follow;
        }

        private IEnumerator ShowMechanismView()
        {
            if (ImpactCamera == null || DroneTarget == null || MainCamera == null) yield break;
            
            MainCamera.gameObject.SetActive(false);
            ImpactCamera.gameObject.SetActive(true);

            try
            {
                ImpactCamera.transform.SetParent(DroneTarget);
                ImpactCamera.transform.localPosition = _mechanismViewLocalOffset;
                ImpactCamera.transform.localRotation = Quaternion.Euler(_mechanismViewLocalRotation);
                
                yield return new WaitForSeconds(_mechanismViewDuration);
            }
            finally
            {
                if (ImpactCamera != null)
                {
                    ImpactCamera.transform.SetParent(null);
                    ImpactCamera.gameObject.SetActive(false);
                }
                if (MainCamera != null)
                {
                    MainCamera.gameObject.SetActive(true);
                }
                // Debug.Log("카메라 상태가 안전하게 복구되었습니다.");
            }
        }
        
        public void PositionMechanismCameraForPreview()
        {
            if (ImpactCamera == null || DroneTarget == null) return;
            ImpactCamera.gameObject.SetActive(true);
            ImpactCamera.transform.SetParent(DroneTarget);
            ImpactCamera.transform.localPosition = _mechanismViewLocalOffset;
            ImpactCamera.transform.localRotation = Quaternion.Euler(_mechanismViewLocalRotation);
            // Debug.Log("메커니즘 카메라 미리보기가 배치되었습니다.");
        }

        private void HandleMissionStart(Transform startPoint, Transform fireTarget)
        {
            _fireTarget = fireTarget;
            if (FireCamera != null) FireCamera.gameObject.SetActive(true);
            _currentMode = CameraMode.Follow;
        }
        private void HandleReturnToStation()
        {
            if (FireCamera != null) FireCamera.gameObject.SetActive(false);
            _fireTarget = null;
            _currentMode = CameraMode.Follow;
        }
        private void UpdateFollowCamera()
        {
            Vector3 targetPosition = DroneTarget.position + _followOffset;
            MainCamera.transform.position = Vector3.SmoothDamp(MainCamera.transform.position, targetPosition, ref _cameraFollowVelocity, _followSmoothTime);
            Vector3 lookAtPoint = DroneTarget.position + _lookAtOffset;
            Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - MainCamera.transform.position);
            MainCamera.transform.rotation = Quaternion.Slerp(MainCamera.transform.rotation, targetRotation, Time.deltaTime / _rotationSmoothTime);
        }
        private void UpdateOrbitCamera()
        {
            MainCamera.transform.RotateAround(_orbitTargetPosition, Vector3.up, _orbitSpeed * Time.deltaTime);
            Vector3 lookAtPoint = DroneTarget.position + _lookAtOffset;
            Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - MainCamera.transform.position);
            MainCamera.transform.rotation = Quaternion.Slerp(MainCamera.transform.rotation, targetRotation, Time.deltaTime / _rotationSmoothTime);
        }
        
        private void UpdateSubCameras()
        {
            if (StationCamera != null && StationCamera.gameObject.activeInHierarchy && StationTarget != null)
            {
                StationCamera.transform.position = StationTarget.position + _stationCameraOffset;
                StationCamera.transform.LookAt(StationTarget);
            }
            if (FireCamera != null && FireCamera.gameObject.activeInHierarchy && _fireTarget != null)
            {
                FireCamera.transform.position = _fireTarget.position + _fireCameraOffset;
                FireCamera.transform.LookAt(_fireTarget);
            }
        }
    }
}
