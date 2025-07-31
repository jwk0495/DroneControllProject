// C:\Unity\TeamProject\Assets\JWK\Scripts\DroneController.cs

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JWK.Scripts.DropSystem;
using JWK.Scripts.FireManager;
using JWK.Scripts.CameraManager;
using JWK.Scripts.Station;

namespace JWK.Scripts.Drone
{
    [RequireComponent(typeof(Rigidbody))]
    public class DroneController : MonoBehaviour
    {
        // ... (대부분의 변수와 함수는 그대로 유지됩니다) ...
        #region 변수 선언 (Fields and Properties)

        [Header("Firebase 연동")]
        [SerializeField] private FirebaseManager firebaseManager;

        [Header("페이로드 및 임무")]
        [SerializeField] private ExtinguisherDropSystem extinguisherDropSystem;
        public bool IsArrived { get; private set; }

        [Header("스테이션 연동")]
        [SerializeField] private RoofManager roofManager;
        [Tooltip("이륙 후 또는 착륙 완료 후 지붕이 닫히기 전 대기 시간입니다.")]
        [SerializeField] private float roofCloseDelay = 2.5f;
        [Tooltip("착륙 하강 시작 후 지붕이 열리기까지의 대기 시간입니다.")]
        [SerializeField] private float roofOpenDelayOnLanding = 4.0f;

        private Rigidbody _rb;
        private Coroutine _actionCoroutine;
        
        private Queue<GameObject> _fireTargetsQueue;

        [Header("드론 실시간 상태")]
        [SerializeField] private float batteryLevel = 100.0f;
        public Vector3 CurrentPositionAbs { get; private set; }
        public float CurrentAltitudeAbs { get; private set; }
        public float BatteryLevel => batteryLevel;

        [Header("임무 상태 및 페이로드")]
        public DroneMissionState currentMissionState = DroneMissionState.IdleAtStation;
        public PayloadType currentPayload = PayloadType.FireExtinguishingBomb;
        
        [Header("Inspector 테스트용 임무")]
        public Transform testDispatchTarget;
        
        private readonly string[] _missionStateStrings = Enum.GetNames(typeof(DroneMissionState));
        private readonly string[] _payloadTypeStrings = Enum.GetNames(typeof(PayloadType));

        [Header("드론 기본 성능")]
        [SerializeField] private float hoverForce = 70.0f;
        [SerializeField] private float moveForce = 15.0f;

        [Header("모델 비주얼 설정")]
        [Tooltip("기울일 드론의 비주얼 모델 Transform")]
        [SerializeField] private Transform droneModelTransform;
        [Tooltip("최대 기울기 각도")]
        [SerializeField] private float maxTiltAngle = 15.0f;
        [Tooltip("기울기 복원 시 스프링처럼 작용하는 힘의 강도입니다. 높을수록 더 빠르게 원래 각도로 돌아옵니다.")]
        [SerializeField] private float tiltSpringStiffness = 50f;
        [Tooltip("기울기 스윙이 멈추는 정도입니다. 0에 가까우면 바이킹처럼 많이 흔들리고, 1에 가까우면 거의 흔들리지 않고 멈춥니다.")]
        [Range(0f, 1f)]
        [SerializeField] private float tiltDamping = 0.1f;
        private Quaternion _modelNeutralRotation;
        private Vector3 _modelNeutralPosition;
        private Vector2 _visualTilt; 
        private Vector2 _tiltVelocity; 


        [Header("임무 설정")]
        [SerializeField] private Transform droneStationLocation;
        [SerializeField] private float missionCruisingAgl = 50.0f;
        [SerializeField] private float arrivalDistanceThreshold = 0.1f;
        [SerializeField] private float preActionStabilizationTime = 0.5f;
        [Tooltip("폭탄 투하 후 다음 행동까지 대기하는 시간입니다.")]
        [SerializeField] private float postDropMoveDelay = 1.5f;
        [Tooltip("프로펠러가 최대 속도에 도달한 후 실제 이륙까지 대기하는 시간입니다.")]
        [SerializeField] private float preTakeoffDelay = 1.5f;
        [SerializeField] private float retreatDistance = 10.0f;
        private float _arrivalDistanceThresholdSqr;
        private Vector3 _currentTargetPosition; 
        private Vector3 _actualFireTargetPosition; 
        private int _currentBombLoad;
        [SerializeField] private int totalBombs = 6;
        
        private Vector3 _takeoffPosition;
        private Quaternion _takeoffRotation;

        [Header("고도 제어 (PD & AGL)")]
        [SerializeField] private float kpAltitude = 2.0f;
        [SerializeField] private float kdAltitude = 2.5f;
        [Tooltip("목표 고도 변경 시 얼마나 부드럽게 도달할지 결정합니다. 낮을수록 부드러워집니다.")]
        [SerializeField] private float altitudeSmoothSpeed = 1.5f;
        [SerializeField] private float landingDescentRate = 0.4f;
        [SerializeField] private float terrainCheckDistance = 50.0f;
        [SerializeField] private LayerMask groundLayerMask;
        private float _currentGroundYAgl;
        private float _targetAltitudeAbs;
        private float _smoothedTargetAltitudeAbs; 

        [Header("자율 이동 및 회전 개선")]
        [SerializeField] private float kpRotation = 0.8f;
        [SerializeField] private float kdRotation = 0.3f;
        [SerializeField] private float turnBeforeMoveAngleThreshold = 15.0f;
        [SerializeField] private float decelerationStartDistanceXZ = 15.0f;
        [SerializeField] private float maxRotationTorque = 15.0f;
        [Tooltip("회전이 얼마나 부드럽게 될지 결정합니다. 값을 높이면 회전이 더 부드러워집니다.")]
        [SerializeField] private float rotationSmoothTime = 1.2f;
        [Tooltip("속도 변경(가/감속)이 얼마나 부드럽게 될지 결정합니다. 값을 높이면 가감속이 더 부드러워집니다.")]
        [SerializeField] private float velocitySmoothTime = 1.2f;
        
        [Tooltip("착륙 시 최종 위치를 보정하는 힘의 강도입니다.")]
        [SerializeField] private float landingCorrectionForce = 2.0f;
        [Tooltip("착륙 시 위치 보정의 저항값입니다. 높을수록 오버슈팅이 줄어듭니다.")]
        [SerializeField] private float landingCorrectionDamping = 1.5f;

        private Vector3 _smoothedLookDirection;
        private Vector3 _currentSmoothedVelocity;
        private float _decelerationStartDistanceSqr;

        private readonly WaitForSeconds _terrainCheckWait = new WaitForSeconds(0.1f);
        private WaitForSeconds _preActionWait;
        private readonly WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();

        #endregion

        // --- [핵심 수정 부분] ---
        // '도착 후 물리적 안정화 대기' 방식에서 '고도 도착 후 0.5초 대기' 방식으로 변경합니다.
        private IEnumerator PerformActionCoroutine()
        {
            Debug.Log("<color=cyan>[도착 확인]</color> 목표 상공 도착. 고도 안정화 및 0.5초 대기를 시작합니다.");

            // 현재 고도가 목표 고도에 도달할 때까지 기다립니다. (오차 범위 0.5m)
            // WaitUntil은 조건이 참이 될 때까지 매 프레임 검사하고 코루틴을 중지시킵니다.
            yield return new WaitUntil(() => Mathf.Abs(CurrentAltitudeAbs - _smoothedTargetAltitudeAbs) < 0.5f);
            
            Debug.Log($"<color=green>[고도 안정화 완료]</color> 목표 고도({_smoothedTargetAltitudeAbs:F2}m) 도달. 1초 후 투하합니다.");
            
            // 정확히 1초를 기다립니다.
            yield return new WaitForSeconds(0.5f);

            Debug.Log("<color=yellow>[투하 명령]</color> 0.5초 대기 완료. 투하 시퀀스를 시작합니다.");
            
            // 0.5초 대기 후 페이로드 투하 로직을 실행합니다.
            if (currentPayload == PayloadType.FireExtinguishingBomb)
            {
                if(extinguisherDropSystem && _currentBombLoad > 0)
                {
                    yield return StartCoroutine(extinguisherDropSystem.DropSingleBomb(_actualFireTargetPosition, this.transform));
                    _currentBombLoad--;
                }
                else
                {
                    Debug.LogWarning("ExtinguisherDropSystem이 없거나 폭탄을 모두 소진했습니다.");
                }
            }
            
            // 투하 후 후퇴 로직은 그대로 유지합니다.
            yield return new WaitForSeconds(postDropMoveDelay);

            Vector3 retreatDirection = -transform.forward;
            Vector3 retreatPosition = transform.position + retreatDirection * retreatDistance;
            
            _currentTargetPosition = retreatPosition;
            currentMissionState = DroneMissionState.RetreatingAfterAction;
        
            _actionCoroutine = null;
        }
        // --- [수정 끝] ---
        #region Unity 생명주기 함수 (Lifecycle Methods)
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.angularDamping = 1.0f;

            _arrivalDistanceThresholdSqr = arrivalDistanceThreshold * arrivalDistanceThreshold;
            _decelerationStartDistanceSqr = decelerationStartDistanceXZ * decelerationStartDistanceXZ;
            
            _preActionWait = new WaitForSeconds(preActionStabilizationTime);
            
            _fireTargetsQueue = new Queue<GameObject>();
        }

        private void Start()
        {
            StartCoroutine(TerrainCheckRoutine());
            
            PerformInitialGroundCheckAndSetAltitude();
            currentMissionState = DroneMissionState.IdleAtStation;
            _currentBombLoad = totalBombs;

            _smoothedLookDirection = transform.forward;
            _currentSmoothedVelocity = Vector3.zero;

            if (droneModelTransform)
            {
                _modelNeutralRotation = droneModelTransform.localRotation;
                _modelNeutralPosition = droneModelTransform.localPosition;
            }
            
            _visualTilt = Vector2.zero;
            _tiltVelocity = Vector2.zero;
        }

        private void Update()
        {
            UpdateDroneInternalStatus();
            RunStateMachine();
        }

        private void FixedUpdate()
        {
            SmoothAltitudeTarget();
            ApplyForcesBasedOnState();
        }


        private void LateUpdate()
        {
            if (droneModelTransform)
            {
                ApplyVisualTilt();
            }
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
        #endregion

        #region 드론 임무 및 상태 관리 (Drone Mission & State Logic)
        private void RunStateMachine()
        {
            switch (currentMissionState)
            {
                case DroneMissionState.TakingOff:              Handle_TakingOff();         break;
                case DroneMissionState.MovingToTarget:         Handle_MovingToTarget();    break;
                case DroneMissionState.ReturningToStation:
                case DroneMissionState.EmergencyReturn:        Handle_MovingToStation();   break;
                case DroneMissionState.Landing:                Handle_Landing();           break;
                case DroneMissionState.RetreatingAfterAction:  Handle_MovingToStation();   break;
            }
        }
        #endregion
        #region 상태별 핸들러 (State Handlers)
        private void Handle_TakingOff()
        {
            if (Mathf.Abs(CurrentAltitudeAbs - _smoothedTargetAltitudeAbs) < 0.5f)
                currentMissionState = DroneMissionState.MovingToTarget;
        }
        
        private void Handle_MovingToTarget()
        {
            Vector3 dronePosXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPosXZ = new Vector3(_currentTargetPosition.x, 0, _currentTargetPosition.z);
            float distanceSqr = (dronePosXZ - targetPosXZ).sqrMagnitude;

            if (distanceSqr < _arrivalDistanceThresholdSqr)
            {
                IsArrived = true;
                currentMissionState = DroneMissionState.PerformingAction;

                DroneCameraEvents.ArrivedAtDropZone(_actualFireTargetPosition);

                if (_actionCoroutine != null) StopCoroutine(_actionCoroutine);
                _actionCoroutine = StartCoroutine(PerformActionCoroutine());
            }
        }

        private void Handle_MovingToStation()
        {
            Vector3 currentPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 targetPosXZ = new Vector3(_currentTargetPosition.x, 0, _currentTargetPosition.z);
            float distanceSqr = (currentPosXZ - targetPosXZ).sqrMagnitude;

            if (distanceSqr < _arrivalDistanceThresholdSqr)
            {
                if (currentMissionState == DroneMissionState.RetreatingAfterAction)
                    DecideNextAction();
            }
        }
        
        private void Handle_Landing()
        {
            if (Mathf.Abs(CurrentAltitudeAbs - _targetAltitudeAbs) < 0.15f)
            {
                if (_rb.linearVelocity.sqrMagnitude < 0.01f && _rb.angularVelocity.sqrMagnitude < 0.01f)
                {
                    _rb.isKinematic = true; 
                    transform.position = _takeoffPosition;
                    transform.rotation = _takeoffRotation;
                    _rb.isKinematic = false;

                    currentMissionState = DroneMissionState.IdleAtStation;
                    _currentBombLoad = totalBombs;
                    PerformInitialGroundCheckAndSetAltitude();
                    
                    DroneEvents.LandingSequenceCompleted();
                }
            }
        }
        #endregion
        // ... (이하 나머지 코드는 모두 동일합니다) ...
    
        #region 임무 수행 로직 (Action Logic)
        
        public void StartSingleTargetMission(Vector3 targetPosition)
        {
            if (currentMissionState != DroneMissionState.IdleAtStation) return;
            
            _fireTargetsQueue.Clear();
            _currentBombLoad = totalBombs;
            if(extinguisherDropSystem) extinguisherDropSystem.ResetBombs();

            var tempTarget = new GameObject("SingleMissionTarget");
            tempTarget.transform.position = targetPosition;
            _fireTargetsQueue.Enqueue(tempTarget);
            Destroy(tempTarget, 300f); 

            StartCoroutine(FullMissionSequence());
        }
        
        public void StartFireSuppressionMission(List<GameObject> fireTargets)
        {
            if (currentMissionState != DroneMissionState.IdleAtStation) return;
            if (fireTargets == null || fireTargets.Count == 0)
            {
                Debug.LogWarning("진압할 화재 목표가 없습니다.");
                return;
            }

            _fireTargetsQueue.Clear();
            foreach (var target in fireTargets)
            {
                _fireTargetsQueue.Enqueue(target);
            }

            _currentBombLoad = totalBombs;
            if(extinguisherDropSystem) extinguisherDropSystem.ResetBombs();

            StartCoroutine(FullMissionSequence());
        }

        private IEnumerator FullMissionSequence()
        {
            // --- 1. 이륙 준비 ---
            Debug.Log("출동 명령 수신! 이륙 시퀀스를 시작합니다.");
            if (roofManager) yield return roofManager.Open();
            else Debug.LogWarning("RoofManager가 연결되지 않았습니다.");

            if (!SetNextMissionTarget())
            {
                Debug.LogError("임무를 시작할 유효한 타겟이 없습니다.");
                if (roofManager) yield return roofManager.Close();
                yield break; 
            }

            // --- 2. 이륙 ---
            yield return StartCoroutine(TakeOffSequenceCoroutine());
            
            yield return new WaitUntil(() => currentMissionState == DroneMissionState.MovingToTarget);
            
            Debug.Log("이륙 완료. 잠시 후 지붕을 닫습니다.");
            yield return new WaitForSeconds(roofCloseDelay);
            if (roofManager) yield return roofManager.Close(); 

            // --- 3. 임무 수행 및 복귀 ---
            yield return new WaitUntil(() => currentMissionState == DroneMissionState.ReturningToStation || currentMissionState == DroneMissionState.EmergencyReturn);
            Debug.Log("임무 지역에서 복귀합니다. 스테이션으로 이동합니다.");

            yield return new WaitUntil(() => {
                if (!droneStationLocation) return true; 
                Vector3 currentPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 stationPosXZ = new Vector3(droneStationLocation.position.x, 0, droneStationLocation.position.z);
                return (currentPosXZ - stationPosXZ).sqrMagnitude < _arrivalDistanceThresholdSqr;
            });
            Debug.Log("스테이션 상공 도착. 최종 착륙 위치로 이동합니다.");

            // --- 4. 착륙 준비 (위치 및 방향 정렬) ---
            Vector3 finalApproachPoint = new Vector3(_takeoffPosition.x, transform.position.y, _takeoffPosition.z);
            _currentTargetPosition = finalApproachPoint;
            currentMissionState = DroneMissionState.ReturningToStation; 

            yield return new WaitUntil(() => {
                Vector3 currentPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
                Vector3 approachPointXZ = new Vector3(finalApproachPoint.x, 0, finalApproachPoint.z);
                return (currentPosXZ - approachPointXZ).sqrMagnitude < _arrivalDistanceThresholdSqr;
            });
            Debug.Log("착륙 위치 상공 도착. 방향 정렬 시작.");

            currentMissionState = DroneMissionState.HoldingPosition; 
            _smoothedLookDirection = _takeoffRotation * Vector3.forward;

            // 회전이 완료될 때까지 대기
            yield return new WaitUntil(() => Quaternion.Angle(transform.rotation, _takeoffRotation) < 1.0f);
            Debug.Log("방향 정렬 완료. 착륙 하강을 시작합니다.");

            // --- 5. 착륙 ---
            currentMissionState = DroneMissionState.Landing;
            _targetAltitudeAbs = _takeoffPosition.y;

            yield return new WaitForSeconds(roofOpenDelayOnLanding);

            if (roofManager)
            {
                Debug.Log("지붕 개방을 시작합니다.");
                yield return roofManager.Open();
            }

            yield return new WaitUntil(() => currentMissionState == DroneMissionState.IdleAtStation);
            
            Debug.Log("착륙 완료. 잠시 후 지붕을 닫습니다.");
            yield return new WaitForSeconds(roofCloseDelay);
            if (roofManager) yield return roofManager.Close();
            
            Debug.Log("임무 완전 종료. 스테이션에 안전하게 격납되었습니다.");
        }

        private void DecideNextAction()
        {
            if (currentMissionState != DroneMissionState.RetreatingAfterAction)
            {
                return;
            }

            if (SetNextMissionTarget() && _currentBombLoad > 0)
            {
                currentMissionState = DroneMissionState.MovingToTarget;
            }
            else
            {
                if (droneStationLocation)
                {
                    _currentTargetPosition = droneStationLocation.position;
                    _targetAltitudeAbs = droneStationLocation.position.y + 20f;
                    currentMissionState = DroneMissionState.ReturningToStation;
                    DroneCameraEvents.ReturnToStation();
                }
                else
                {
                    Debug.LogError("Cannot return to station, droneStationLocation is not set! Holding position.");
                    currentMissionState = DroneMissionState.HoldingPosition;
                }
            }
        }

        private bool SetNextMissionTarget()
        {
            while (_fireTargetsQueue.Count > 0)
            {
                GameObject nextTarget = _fireTargetsQueue.Dequeue();
                if (nextTarget)
                {
                    SetMissionTarget(nextTarget.transform.position);
                    
                    DroneCameraEvents.MissionStart(transform, nextTarget.transform);
                    return true; 
                }
                else
                {
                    Debug.LogWarning("[정보] 이미 파괴된 목표(유령 참조)를 큐에서 제거하고 다음을 탐색합니다.");
                }
            }
            
            return false; 
        }
        
        private void SetMissionTarget(Vector3 actualFirePosition)
        {
            _actualFireTargetPosition = actualFirePosition;

            if (extinguisherDropSystem && _currentBombLoad > 0 && droneStationLocation != null)
            {
                Vector3 directionToTarget = (actualFirePosition - droneStationLocation.position).normalized;
                directionToTarget.y = 0;
                
                if (directionToTarget.sqrMagnitude > 0.001f)
                {
                    Quaternion predictedRotation = Quaternion.LookRotation(directionToTarget);
                    Vector3 bombLocalOffset = extinguisherDropSystem.GetNextBombOffsetFromDroneRoot(this.transform);
                    Vector3 bombWorldOffset = predictedRotation * bombLocalOffset;
                    _currentTargetPosition = actualFirePosition - bombWorldOffset;
                }
                else
                {
                    _currentTargetPosition = actualFirePosition;
                }
            }
            else
            {
                if (droneStationLocation == null)
                {
                    Debug.LogWarning("DroneStationLocation이 할당되지 않았습니다. 폭탄 투하 위치 보정 없이 임무를 수행합니다.");
                }
                _currentTargetPosition = actualFirePosition;
            }
        }
        
        private IEnumerator TakeOffSequenceCoroutine()
        {
            _takeoffPosition = transform.position;
            _takeoffRotation = transform.rotation;

            DroneEvents.TakeOffSequenceStarted();

            yield return new WaitForSeconds(preTakeoffDelay);

            currentPayload = PayloadType.FireExtinguishingBomb;
            Vector3 takeoffRefPos = droneStationLocation ? droneStationLocation.position : transform.position;
        
            if (Physics.Raycast(takeoffRefPos + Vector3.up, Vector3.down, out RaycastHit hit, terrainCheckDistance, groundLayerMask))
                _targetAltitudeAbs = hit.point.y + missionCruisingAgl;
            
            else
                _targetAltitudeAbs = takeoffRefPos.y + missionCruisingAgl;
        
            currentMissionState = DroneMissionState.TakingOff;
        }
        #endregion

        #region 드론 물리 및 상태 업데이트 (Physics & Status Updates)
        
        private void PerformInitialGroundCheckAndSetAltitude()
        {
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, terrainCheckDistance, groundLayerMask))
            {
                _currentGroundYAgl = hit.point.y;
            }
            else
            {
                _currentGroundYAgl = transform.position.y;
            }
            _targetAltitudeAbs = transform.position.y;
            _smoothedTargetAltitudeAbs = _targetAltitudeAbs;
        }

        private IEnumerator TerrainCheckRoutine()
        {
            while (true)
            {
                if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, terrainCheckDistance, groundLayerMask))
                {
                    _currentGroundYAgl = hit.point.y;
                }
                
                if (currentMissionState == DroneMissionState.MovingToTarget || 
                    currentMissionState == DroneMissionState.RetreatingAfterAction ||
                    currentMissionState == DroneMissionState.PerformingAction ||
                    currentMissionState == DroneMissionState.HoldingPosition)
                {
                    _targetAltitudeAbs = _currentGroundYAgl + missionCruisingAgl;
                }
                yield return _terrainCheckWait;
            }
        }
        
        private void UpdateDroneInternalStatus()
        {
            CurrentPositionAbs = transform.position;
            CurrentAltitudeAbs = CurrentPositionAbs.y;
        
            if (currentMissionState != DroneMissionState.IdleAtStation)
            {
                batteryLevel = Mathf.Max(0, batteryLevel - Time.deltaTime * 0.05f);
            }
        }

        private void SmoothAltitudeTarget()
        {
            _smoothedTargetAltitudeAbs = Mathf.Lerp(_smoothedTargetAltitudeAbs, _targetAltitudeAbs, Time.fixedDeltaTime * altitudeSmoothSpeed);
        }

        private void ApplyForcesBasedOnState()
        {
            _rb.AddForce(Physics.gravity, ForceMode.Acceleration);

            switch (currentMissionState)
            {
                case DroneMissionState.TakingOff:
                case DroneMissionState.MovingToTarget:
                case DroneMissionState.ReturningToStation:
                case DroneMissionState.EmergencyReturn:
                case DroneMissionState.RetreatingAfterAction:
                case DroneMissionState.PerformingAction:
                case DroneMissionState.HoldingPosition:
                    ApplyVerticalForce(2.0f);
                    ApplyHorizontalAndRotationalForces();
                    break;
                case DroneMissionState.Landing:
                    ApplyLandingForce();
                    break;
                case DroneMissionState.IdleAtStation:
                    _rb.AddForce(-_rb.linearVelocity, ForceMode.VelocityChange);
                    _rb.AddTorque(-_rb.angularVelocity, ForceMode.VelocityChange);
                    _currentSmoothedVelocity = Vector3.zero;
                    _smoothedLookDirection = transform.forward;
                    break;
            }
        }

        private void ApplyHorizontalDamping()
        {
            Vector3 horizontalVel = _rb.linearVelocity;
            horizontalVel.y = 0;
            _rb.AddForce(-horizontalVel, ForceMode.VelocityChange);
            _rb.AddTorque(-_rb.angularVelocity, ForceMode.VelocityChange);
        }

        private void ApplyVerticalForce(float maxForceMultiplier)
        {
            float altError = _smoothedTargetAltitudeAbs - CurrentAltitudeAbs;
            float pForceAlt = altError * kpAltitude;
            float dForceAlt = -_rb.linearVelocity.y * kdAltitude;
            float totalVertForce = Physics.gravity.magnitude + pForceAlt + dForceAlt;
            _rb.AddForce(Vector3.up * Mathf.Clamp(totalVertForce, 0.0f, hoverForce * maxForceMultiplier), ForceMode.Acceleration);
        }

        private void ApplyLandingForce()
        {
            if (CurrentAltitudeAbs > _targetAltitudeAbs + 0.05f)
            {
                float descentRate = landingDescentRate;
                if (CurrentAltitudeAbs < _targetAltitudeAbs + 1.0f)
                {
                    descentRate *= 0.5f;
                }
                
                float upwardThrust = Mathf.Max(0, Physics.gravity.magnitude - descentRate);
                _rb.AddForce(Vector3.up * upwardThrust, ForceMode.Acceleration);
            }

            Vector3 targetPosXZ = new Vector3(_takeoffPosition.x, 0, _takeoffPosition.z);
            Vector3 currentPosXZ = new Vector3(transform.position.x, 0, transform.position.z);
            Vector3 positionError = targetPosXZ - currentPosXZ;
            
            Vector3 correctiveForce = positionError * landingCorrectionForce;
            
            Vector3 horizontalVelocity = _rb.linearVelocity;
            horizontalVelocity.y = 0;
            
            correctiveForce -= horizontalVelocity * landingCorrectionDamping;
            
            _rb.AddForce(new Vector3(correctiveForce.x, 0, correctiveForce.z), ForceMode.Acceleration);

            Quaternion targetRotation = _takeoffRotation;
            float targetAngleY = targetRotation.eulerAngles.y;
            float angleErrorY = Mathf.DeltaAngle(_rb.rotation.eulerAngles.y, targetAngleY);
            float pTorque = angleErrorY * Mathf.Deg2Rad * kpRotation;
            float dTorque = -_rb.angularVelocity.y * kdRotation;
            _rb.AddTorque(Vector3.up * Mathf.Clamp(pTorque + dTorque, -maxRotationTorque, maxRotationTorque), ForceMode.Acceleration);
        }

        private void ApplyHorizontalAndRotationalForces()
        {
            if (currentMissionState == DroneMissionState.IdleAtStation ||
                currentMissionState == DroneMissionState.Landing ||
                currentMissionState == DroneMissionState.TakingOff)
            {
                ApplyHorizontalDamping();
                return;
            }
            
            if (currentMissionState == DroneMissionState.PerformingAction)
            {
                ApplyHorizontalDamping();
                _currentSmoothedVelocity = Vector3.zero;
                return;
            }

            if (currentMissionState != DroneMissionState.HoldingPosition)
            {
                Vector3 currentPosXZ = transform.position;
                currentPosXZ.y = 0;
                Vector3 targetPosXZ = _currentTargetPosition;
                targetPosXZ.y = 0;
                Vector3 directionToTarget = (targetPosXZ - currentPosXZ);
                float distanceToTarget = directionToTarget.magnitude;

                Vector3 targetLookDirection = (distanceToTarget > 0.01f) ? directionToTarget.normalized : transform.forward;
                _smoothedLookDirection = Vector3.Slerp(_smoothedLookDirection, targetLookDirection, Time.fixedDeltaTime / rotationSmoothTime);
                
                float desiredSpeed = moveForce;
                if (distanceToTarget < decelerationStartDistanceXZ)
                {
                    desiredSpeed = Mathf.SmoothStep(0f, moveForce, distanceToTarget / decelerationStartDistanceXZ);
                }

                Vector3 desiredVelocityXZ = _smoothedLookDirection * desiredSpeed;
                
                float angleToTarget = Quaternion.Angle(transform.rotation, Quaternion.LookRotation(_smoothedLookDirection));
                
                if (angleToTarget > turnBeforeMoveAngleThreshold)
                {
                    desiredVelocityXZ *= 0.2f;
                }

                _currentSmoothedVelocity = Vector3.Lerp(_currentSmoothedVelocity, desiredVelocityXZ, Time.fixedDeltaTime / velocitySmoothTime);

                Vector3 currentVelocityXZ = _rb.linearVelocity;
                currentVelocityXZ.y = 0;
                Vector3 forceNeededXZ = (_currentSmoothedVelocity - currentVelocityXZ) * 3.0f;
                _rb.AddForce(forceNeededXZ, ForceMode.Acceleration);
            }
            else
            {
                Vector3 horizontalVel = _rb.linearVelocity;
                horizontalVel.y = 0;
                _rb.AddForce(-horizontalVel, ForceMode.VelocityChange);
                _currentSmoothedVelocity = Vector3.zero;
            }
            
            Quaternion targetRotation = Quaternion.LookRotation(_smoothedLookDirection);
            float targetAngleY = targetRotation.eulerAngles.y;
            float angleErrorY = Mathf.DeltaAngle(_rb.rotation.eulerAngles.y, targetAngleY);
            float pTorque = angleErrorY * Mathf.Deg2Rad * kpRotation;
            float dTorque = -_rb.angularVelocity.y * kdRotation;
            _rb.AddTorque(Vector3.up * Mathf.Clamp(pTorque + dTorque, -maxRotationTorque, maxRotationTorque), ForceMode.Acceleration);
        }
        
        private void ApplyVisualTilt()
        {
            if (_rb == null || droneModelTransform == null) return;

            Vector3 localVelocity = transform.InverseTransformDirection(_rb.linearVelocity);
            float targetPitch = -Mathf.Clamp(localVelocity.z / moveForce, -1f, 1f) * maxTiltAngle;
            float roll = Mathf.Clamp(localVelocity.x / moveForce, -1f, 1f) * maxTiltAngle;
            Vector2 targetTilt = new Vector2(targetPitch, roll);

            Vector2 springForce = (targetTilt - _visualTilt) * tiltSpringStiffness;

            float dampingCoefficient = tiltDamping * 2 * Mathf.Sqrt(tiltSpringStiffness);
            
            Vector2 dampingForce = -_tiltVelocity * dampingCoefficient;

            Vector2 acceleration = springForce + dampingForce;

            _tiltVelocity += acceleration * Time.deltaTime;
            
            _visualTilt += _tiltVelocity * Time.deltaTime;

            Quaternion finalRotation = _modelNeutralRotation * Quaternion.Euler(_visualTilt.x, 0, _visualTilt.y);
            droneModelTransform.localRotation = finalRotation;
            
            droneModelTransform.localPosition = _modelNeutralPosition;
        }

        public void DispatchMissionToTestTarget()
        {
            if (currentMissionState != DroneMissionState.IdleAtStation) 
            {
                Debug.LogWarning("[Mission] 드론이 현재 임무 수행 중입니다."); 
                return; 
            }
            if (!testDispatchTarget) 
            {
                Debug.LogError("[Mission] 테스트 임무 타겟이 설정되지 않았습니다!"); 
                return; 
            }
            
            StartSingleTargetMission(testDispatchTarget.position);
            SendDispatchDataToServer("수동 타겟 임무 (테스트)", testDispatchTarget.position);
        }

        public void DispatchMissionToRandomFire()
        {
            if (currentMissionState != DroneMissionState.IdleAtStation) 
            {
                Debug.LogWarning("[Mission] 드론이 현재 임무 수행 중입니다."); 
                return;
            }
            if (!WildfireManager.Instance)
            {
                Debug.LogError("[Mission] WildfireManager 인턴스를 찾을 수 없습니다!");
                return;
            }
            if (!WildfireManager.Instance.isFireActive)
            {
                Debug.Log("[Mission] 화재가 없으므로 새로 생성합니다.");
                WildfireManager.Instance.GenerateFires();
            }
            
            List<GameObject> fireTargets = WildfireManager.Instance.GetActiveFires();
            StartFireSuppressionMission(fireTargets);
            
            if (fireTargets.Count > 0)
            {
                SendDispatchDataToServer("랜덤 화재 진압 (테스트)", fireTargets[0].transform.position);
            }
        }

        private void SendDispatchDataToServer(string missionType, Vector3 targetPosition)
        {
            if (firebaseManager != null)
            {
                firebaseManager.SendDispatchData(missionType, targetPosition);
            }
            else
            {
                Debug.LogWarning("FirebaseManager가 할당되지 않아 임무 파견 데이터를 전송할 수 없습니다.");
            }
        }

        #endregion

        #region Firebase 연동 공개 메서드 (Public Methods for Firebase)

        public DroneStatusData GetCurrentStatusData()
        {
            return new DroneStatusData(
                CurrentPositionAbs,
                CurrentAltitudeAbs,
                BatteryLevel,
                _missionStateStrings[(int)currentMissionState],
                _payloadTypeStrings[(int)currentPayload],
                _currentBombLoad
            );
        }

        public void HandleForceReturnCommand()
        {
            if (droneStationLocation)
            {
                _currentTargetPosition = droneStationLocation.position;
                currentMissionState = DroneMissionState.EmergencyReturn;
                if (_actionCoroutine != null) StopCoroutine(_actionCoroutine);
                DroneCameraEvents.ReturnToStation();
                Debug.Log("[Firebase] 강제 복귀 명령 수신");
            }
        }

        public void HandleEmergencyStopCommand()
        {
            currentMissionState = DroneMissionState.HoldingPosition;
            if (_actionCoroutine != null) StopCoroutine(_actionCoroutine);
            Debug.Log("[Firebase] 긴급 정지 명령 수신");
        }

        public void HandleChangePayloadCommand(string payload)
        {
            if (currentMissionState == DroneMissionState.IdleAtStation)
            {
                if (Enum.TryParse(payload, out PayloadType newPayload))
                {
                    currentPayload = newPayload;
                    Debug.Log($"[Firebase][Mission] Payload changed to: {currentPayload}");
                }
            }
        }
        #endregion
    }
}