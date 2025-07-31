// Assets/JWK/Scripts/Drone/FirebaseManager.cs

using UnityEngine;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using System;

namespace JWK.Scripts.Drone
{
    public class FirebaseManager : MonoBehaviour
    {
        [Header("연동할 드론 컨트롤러")]
        [SerializeField] private DroneController droneController;

        [Header("Firebase 설정")]
        [Tooltip("데이터를 전송할 주기 (초)")]
        [SerializeField] private float dataSendInterval = 0.2f;

        private FirebaseFirestore _firestore;
        private bool _isFirebaseInitialized = false;
        private ListenerRegistration _commandListener;

        private void Start()
        {
            if (droneController == null)
            {
                Debug.LogError("DroneController가 FirebaseManager에 할당되지 않았습니다!");
                return;
            }

            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Exception != null)
                {
                    Debug.LogError($"Firebase 의존성 확인 중 오류 발생: {task.Exception}");
                    return;
                }

                _firestore = FirebaseFirestore.DefaultInstance;
                _isFirebaseInitialized = true;
                Debug.Log("Firebase 초기화 및 Firestore 인스턴스 설정 완료.");

                ListenForCommands();
                StartCoroutine(SendDroneDataRoutine());
            });
        }

        private void OnDestroy()
        {
            _commandListener?.Stop();
        }

        private IEnumerator SendDroneDataRoutine()
        {
            var wait = new WaitForSeconds(dataSendInterval);
            while (true)
            {
                yield return wait;
                if (_isFirebaseInitialized && droneController != null)
                {
                    SendDroneStatus();
                }
            }
        }

        private void SendDroneStatus()
        {
            DroneStatusData statusData = droneController.GetCurrentStatusData();

            // GeoPoint 대신 Map(Dictionary) 형태로 좌표 저장
            var positionMap = new Dictionary<string, float>
            {
                { "x", statusData.position.x },
                { "y", statusData.position.y },
                { "z", statusData.position.z }
            };

            var statusDict = new Dictionary<string, object>
            {
                { "position", positionMap },
                { "altitude", statusData.altitude },
                { "battery", statusData.battery },
                { "mission_state", statusData.mission_state },
                { "payload_type", statusData.payload_type },
                { "bomb_load", statusData.bomb_load },
                { "timestamp", Timestamp.GetCurrentTimestamp() }
            };

            _firestore.Collection("drones").Document("drone_1").SetAsync(statusDict);
        }

        public void SendDispatchData(string missionType, Vector3 targetPosition)
        {
            if (!_isFirebaseInitialized) return;

            // GeoPoint 대신 Map(Dictionary) 형태로 좌표 저장
            var targetPositionMap = new Dictionary<string, float>
            {
                { "x", targetPosition.x },
                { "y", targetPosition.y },
                { "z", targetPosition.z }
            };

            var dispatchData = new Dictionary<string, object>
            {
                { "mission_type", missionType },
                { "target_position", targetPositionMap },
                { "dispatched_at", Timestamp.GetCurrentTimestamp() }
            };
            
            _firestore.Collection("dispatches").AddAsync(dispatchData);
        }

        private void ListenForCommands()
        {
            DocumentReference docRef = _firestore.Collection("drones").Document("drone_1");
            _commandListener = docRef.Listen(snapshot =>
            {
                if (!snapshot.Exists)
                {
                    Debug.LogWarning("[Firestore] drone_1 문서가 존재하지 않습니다.");
                    return;
                }

                if (snapshot.TryGetValue("command", out object commandValue))
                {
                    string commandType = commandValue.ToString();
                    Debug.Log($"[Firestore] 수신된 명령: {commandType}");

                    switch (commandType)
                    {
                        case "force_return":
                            droneController.HandleForceReturnCommand();
                            break;
                        case "emergency_stop":
                            droneController.HandleEmergencyStopCommand();
                            break;
                        case "start_random_fire_mission": // 랜덤 화재 임무 시작 명령 추가
                            droneController.DispatchMissionToRandomFire();
                            break;
                        case "change_payload":
                            if (snapshot.TryGetValue("command_payload", out object payloadValue))
                            {
                                droneController.HandleChangePayloadCommand(payloadValue.ToString());
                            }
                            break;
                    }

                    var updates = new Dictionary<string, object>
                    {
                        { "command", FieldValue.Delete },
                        { "command_payload", FieldValue.Delete }
                    };
                    snapshot.Reference.UpdateAsync(updates);
                }
            });
        }
    }
}
