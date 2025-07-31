using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using JWK.Scripts.CameraManager;

namespace JWK.Scripts
{
    /// <summary>
    /// 모든 카메라 워크를 총괄하는 시네마틱 감독 시스템입니다.
    /// 드론의 상태에 따라 역동적인 카메라 샷을 연출합니다.
    /// </summary>
    public class DirectorSystem : MonoBehaviour
    {
        [Header("핵심 타겟")]
        public Transform DroneTarget;
        public Camera ImpactCamera;

        // ====================================================================================
        // [수정] 카메라 워크 설정 변수들을 더 세분화하여 정교한 제어가 가능하도록 변경합니다.
        [Header("카메라 워크 설정")]
        [Tooltip("드론을 따라다닐 때의 카메라 오프셋입니다. 월드 좌표 기준입니다.")]
        [SerializeField] private Vector3 _followOffset = new Vector3(0, 5f, -10f);
        [Tooltip("카메라가 드론을 바라볼 때의 수직 오프셋입니다. 드론의 무게 중심 등을 설정할 수 있습니다.")]
        [SerializeField] private Vector3 _lookAtOffset = new Vector3(0, 1.5f, 0);
        [Tooltip("카메라 위치 이동의 부드러움입니다. 낮을수록 빠르게 반응합니다.")]
        [SerializeField] private float _positionSmoothTime = 0.5f;
        [Tooltip("카메라 회전의 부드러움입니다. 낮을수록 빠르게 반응합니다.")]
        [SerializeField] private float _rotationSmoothTime = 0.3f;
        // ====================================================================================

        private Coroutine _currentCameraWork;
        private Vector3 _cameraVelocity = Vector3.zero; // SmoothDamp를 위한 속도 변수

        private void OnEnable()
        {
            // 드론의 상태 변경 이벤트를 구독합니다.
            DroneCameraEvents.OnMissionStart += HandleMissionStart;
            DroneCameraEvents.OnArrivedAtDropZone += HandleArrivedAtDropZone;
            DroneCameraEvents.OnBombImpact += HandleBombImpact;
            DroneCameraEvents.OnReturnToStation += HandleReturnToStation;
        }

        private void OnDisable()
        {
            // 이벤트 구독을 해제합니다.
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
            // 시작 시 기본 추적 카메라를 실행합니다.
            HandleReturnToStation();
        }

        // 임무 시작: 기본 추적 샷을 바로 시작합니다.
        private void HandleMissionStart(Transform startPoint, Transform fireTarget)
        {
            SwitchCameraWork(FollowDrone());
        }

        // 목표 도착: 투하 지점 오르빗 카메라 워크 시작
        private void HandleArrivedAtDropZone(Vector3 fireTargetPosition)
        {
            SwitchCameraWork(OrbitDropZone(fireTargetPosition));
        }
        
        // 소화탄 충돌: 임팩트 카메라 활성화
        private void HandleBombImpact(Vector3 impactPosition)
        {
            if (ImpactCamera != null)
            {
                StartCoroutine(ShowImpact(impactPosition));
            }
        }

        // 기지 복귀: 기본 추적 카메라 워크 시작
        private void HandleReturnToStation()
        {
            SwitchCameraWork(FollowDrone());
        }

        // 현재 진행 중인 카메라 워크를 중단하고 새로운 워크로 전환합니다.
        private void SwitchCameraWork(IEnumerator newCameraWork)
        {
            if (_currentCameraWork != null)
            {
                StopCoroutine(_currentCameraWork);
            }
            _currentCameraWork = StartCoroutine(newCameraWork);
        }

        // --- 카메라 워크 코루틴들 ---

        // 1. 웨이포인트 통과 샷 (현재는 사용되지 않음)
        private IEnumerator WaypointTransition(Transform start, Transform end)
        {
            Vector3 startPos = start.position;
            Vector3 endPos = end.position;
            float distance = Vector3.Distance(startPos, endPos);

            List<Vector3> waypoints = new List<Vector3>();
            Vector3 direction = (endPos - startPos).normalized;
            Vector3 side = Vector3.Cross(direction, Vector3.up).normalized * (distance / 8f);

            waypoints.Add(transform.position);
            waypoints.Add(startPos + direction * (distance * 0.2f) + side);
            if (distance > 50f)
            {
                waypoints.Add(startPos + direction * (distance * 0.5f) - side * 1.2f);
            }
            waypoints.Add(endPos - direction * 20f + new Vector3(0, 10f, 0));

            foreach (var point in waypoints)
            {
                float journey = 0f;
                float duration = Vector3.Distance(transform.position, point) / 30f;
                duration = Mathf.Max(duration, 1.5f);

                Vector3 startPoint = transform.position;
                Quaternion startRotation = transform.rotation;

                while (journey < duration)
                {
                    journey += Time.deltaTime;
                    float percent = Mathf.SmoothStep(0, 1, journey / duration);
                    transform.position = Vector3.Lerp(startPoint, point, percent);
                    Quaternion targetRotation = Quaternion.LookRotation(DroneTarget.position - transform.position);
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, percent);
                    yield return null;
                }
            }
        }

        // 2. 투하 지점 오르빗 샷
        private IEnumerator OrbitDropZone(Vector3 fireTargetPosition)
        {
            float orbitSpeed = 10f;
            while (true)
            {
                transform.RotateAround(fireTargetPosition, Vector3.up, orbitSpeed * Time.deltaTime);
                Vector3 lookAtPoint = DroneTarget.position + _lookAtOffset;
                Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - transform.position);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / _rotationSmoothTime);
                yield return null;
            }
        }

        // ====================================================================================
        // [수정] 드론을 화면 중앙에 고정하여 따라가는 로직으로 개선합니다.
        private IEnumerator FollowDrone()
        {
            while (true)
            {
                if (!DroneTarget) yield break;

                // 1. 목표 위치 계산: 드론의 위치에 월드 좌표 기준 오프셋을 더합니다.
                // 이렇게 하면 드론이 회전해도 카메라가 함께 돌지 않아 안정적입니다.
                Vector3 desiredPosition = DroneTarget.position + _followOffset;
                
                // 2. 카메라 위치를 부드럽게 이동 (SmoothDamp 사용으로 더 안정적인 추적 가능)
                transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _cameraVelocity, _positionSmoothTime);
                
                // 3. 목표 바라보기 계산: 드론의 위치에 시선 오프셋을 더한 지점을 바라봅니다.
                Vector3 lookAtPoint = DroneTarget.position + _lookAtOffset;
                Quaternion targetRotation = Quaternion.LookRotation(lookAtPoint - transform.position);

                // 4. 카메라 회전을 부드럽게 변경
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime / _rotationSmoothTime);
                
                yield return null;
            }
        }
        // ====================================================================================

        // 4. 임팩트 샷 (서브 카메라)
        private IEnumerator ShowImpact(Vector3 position)
        {
            if (!ImpactCamera) yield break;
            
            ImpactCamera.gameObject.SetActive(true);
            ImpactCamera.transform.position = position + new Vector3(0, 3f, -5f);
            ImpactCamera.transform.LookAt(position);
            
            yield return new WaitForSeconds(4.0f);
            
            ImpactCamera.gameObject.SetActive(false);
        }
    }
}
