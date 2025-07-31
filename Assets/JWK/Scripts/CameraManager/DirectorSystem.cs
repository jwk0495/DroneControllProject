using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using JWK.Scripts.CameraManager;

namespace JWK.Scripts
{
    /// <summary>
    /// 모든 카메라 워크를 총괄하는 시네마틱 감독 시스템입니다.
    /// 드론의 상태에 따라 역동적인 카메라 샷을 연출합니다.
    /// LateUpdate를 사용하여 물리 기반 객체 추적 시 발생하는 떨림 현상을 방지합니다.
    /// </summary>
    public class DirectorSystem : MonoBehaviour
    {
        [Header("핵심 타겟")]
        public Transform DroneTarget;
        public Camera ImpactCamera;

        [Header("카메라 워크 설정")]
        [Tooltip("드론을 따라다닐 때의 카메라 오프셋입니다. 월드 좌표 기준입니다.")]
        [SerializeField] private Vector3 _followOffset = new Vector3(0, 5f, -10f);
        [Tooltip("카메라가 드론을 바라볼 때의 수직 오프셋입니다.")]
        [SerializeField] private Vector3 _lookAtOffset = new Vector3(0, 1.5f, 0);
        [Tooltip("카메라 위치 이동의 부드러움입니다. 낮을수록 빠르게 반응합니다.")]
        [SerializeField] private float _positionSmoothTime = 0.5f;
        [Tooltip("카메라 회전의 부드러움입니다. 낮을수록 빠르게 반응합니다.")]
        [SerializeField] private float _rotationSmoothTime = 0.3f;
        [Tooltip("오르빗 샷의 회전 속도입니다.")]
        [SerializeField] private float _orbitSpeed = 10f;

        // ====================================================================================
        // [수정] 코루틴 기반에서 LateUpdate 기반의 상태 머신으로 변경
        private enum CameraMode { Follow, Orbit, Idle }
        private CameraMode _currentMode = CameraMode.Idle;
        
        private Vector3 _orbitTargetPosition;
        private Vector3 _cameraVelocity = Vector3.zero;
        // ====================================================================================

        private void OnEnable()
        {
            DroneCameraEvents.OnMissionStart += HandleMissionStart;
            DroneCameraEvents.OnArrivedAtDropZone += HandleArrivedAtDropZone;
            DroneCameraEvents.OnBombImpact += HandleBombImpact;
            DroneCameraEvents.OnReturnToStation += HandleReturnToStation;
        }

        private void OnDisable()
        {
            DroneCameraEvents.OnMissionStart -= HandleMissionStart;
            DroneCameraEvents.OnArrivedAtDropZone -= HandleArrivedAtDropZone;
            DroneCameraEvents.OnBombImpact -= HandleBombImpact;
            DroneCameraEvents.OnReturnToStation -= HandleReturnToStation;
        }

        private void Start()
        {
            if (ImpactCamera != null)
            {
                ImpactCamera.gameObject.SetActive(false);
            }
            // 시작 시 기본 추적 카메라 모드로 설정
            _currentMode = CameraMode.Follow;
        }
        
        // ====================================================================================
        // [수정] 모든 카메라 워크를 LateUpdate에서 처리하도록 로직 변경
        private void LateUpdate()
        {
            if (DroneTarget == null) return;

            switch (_currentMode)
            {
                case CameraMode.Follow:
                    UpdateFollowCamera();
                    break;
                case CameraMode.Orbit:
                    UpdateOrbitCamera();
                    break;
                case CameraMode.Idle:
                    // 유휴 상태에서는 아무것도 하지 않음
                    break;
            }
        }
        // ====================================================================================

        // 임무 시작: 추적 모드로 변경
        private void HandleMissionStart(Transform startPoint, Transform fireTarget)
        {
            _currentMode = CameraMode.Follow;
        }

        // 목표 도착: 오르빗 모드로 변경
        private void HandleArrivedAtDropZone(Vector3 fireTargetPosition)
        {
            _orbitTargetPosition = fireTargetPosition;
            _currentMode = CameraMode.Orbit;
        }
        
        // 소화탄 충돌: 임팩트 카메라 활성화 (코루틴 유지)
        private void HandleBombImpact(Vector3 impactPosition)
        {
            if (ImpactCamera != null)
            {
                StartCoroutine(ShowImpact(impactPosition));
            }
        }

        // 기지 복귀: 추적 모드로 변경
        private void HandleReturnToStation()
        {
            _currentMode = CameraMode.Follow;
        }

        // --- LateUpdate에서 호출될 카메라 워크 함수들 ---

        /// <summary>
        /// 드론을 부드럽게 따라가는 카메라 로직
        /// </summary>
        private void UpdateFollowCamera()
        {
            Vector3 desiredPosition = DroneTarget.position + _followOffset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _cameraVelocity, _positionSmoothTime);
            
            Vector3 lookAtPoint = DroneTarget.position + _lookAtOffset;
            Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / _rotationSmoothTime);
        }

        /// <summary>
        /// 목표 지점을 중심으로 회전하는 오르빗 카메라 로직
        /// </summary>
        private void UpdateOrbitCamera()
        {
            transform.RotateAround(_orbitTargetPosition, Vector3.up, _orbitSpeed * Time.deltaTime);
            
            Vector3 lookAtPoint = DroneTarget.position + _lookAtOffset;
            Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / _rotationSmoothTime);
        }

        /// <summary>
        /// 임팩트 순간을 보여주는 서브 카메라 활성화 로직 (코루틴)
        /// </summary>
        private IEnumerator ShowImpact(Vector3 position)
        {
            if (ImpactCamera == null) yield break;
            
            // 임팩트 샷을 보여주는 동안 메인 카메라의 움직임을 멈춤
            var previousMode = _currentMode;
            _currentMode = CameraMode.Idle;

            ImpactCamera.gameObject.SetActive(true);
            ImpactCamera.transform.position = position + new Vector3(0, 3f, -5f);
            ImpactCamera.transform.LookAt(position);
            
            yield return new WaitForSeconds(4.0f);
            
            ImpactCamera.gameObject.SetActive(false);

            // 임팩트 샷이 끝나면 이전 카메라 모드로 복귀
            _currentMode = previousMode;
        }
    }
}
