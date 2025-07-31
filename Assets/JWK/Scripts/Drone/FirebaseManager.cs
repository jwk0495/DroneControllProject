// Assets/JWK/Scripts/Drone/FirebaseManager.cs

using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic; // Dictionary 사용을 위해 추가
using System; // Enum 사용을 위해 추가

namespace JWK.Scripts.Drone
{
    public class FirebaseManager : MonoBehaviour
    {
        [Header("연동할 드론 컨트롤러")]
        [SerializeField] private DroneController droneController;

        private DatabaseReference _databaseReference;
        private bool _isFirebaseInitialized = false;

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

                FirebaseApp app = FirebaseApp.DefaultInstance;
                _databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                _isFirebaseInitialized = true;
                Debug.Log("Firebase 초기화 및 데이터베이스 참조 설정 완료.");

                ListenForCommands();
            });
        }

        private void Update()
        {
            // Firebase가 초기화되었고, DroneController가 유효할 때만 데이터 전송
            if (_isFirebaseInitialized && droneController != null)
            {
                // 매 프레임 대신 일정 주기로 보내고 싶다면 Coroutine으로 변경 가능
                SendDroneData();
            }
        }

        /// <summary>
        /// 드론의 현재 상태를 Firebase에 전송합니다.
        /// </summary>
        private void SendDroneData()
        {
            DroneStatusData dataToSend = droneController.GetCurrentStatusData();
            string droneDataJson = JsonUtility.ToJson(dataToSend);
            _databaseReference.Child("drones").Child("drone_1").Child("status").SetRawJsonValueAsync(droneDataJson);
        }

        /// <summary>
        /// 임무 파견 데이터를 Firebase에 전송합니다.
        /// </summary>
        public void SendDispatchData(string missionType, Vector3 targetPosition)
        {
            if (!_isFirebaseInitialized) return;

            DispatchData dispatchData = new DispatchData(missionType, targetPosition);
            string dispatchJson = JsonUtility.ToJson(dispatchData);
            _databaseReference.Child("dispatches").Push().SetRawJsonValueAsync(dispatchJson);
        }

        /// <summary>
        /// Firebase로부터 오는 명령을 수신 대기합니다.
        /// </summary>
        private void ListenForCommands()
        {
            _databaseReference.Child("drones").Child("drone_1").Child("command").ValueChanged += HandleCommand;
        }

        /// <summary>
        /// 수신된 명령을 처리합니다.
        /// </summary>
        private void HandleCommand(object sender, ValueChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError(args.DatabaseError.Message);
                return;
            }
            if (args.Snapshot == null || !args.Snapshot.Exists)
            {
                return;
            }

            var commandData = args.Snapshot.Value as Dictionary<string, object>;
            if (commandData == null || !commandData.ContainsKey("type")) return;

            string commandType = commandData["type"].ToString();
            Debug.Log($"[Firebase] 수신된 명령: {commandType}");

            // DroneController의 공개 메서드를 호출하여 명령 실행
            switch (commandType)
            {
                case "force_return":
                    droneController.HandleForceReturnCommand();
                    break;
                case "emergency_stop":
                    droneController.HandleEmergencyStopCommand();
                    break;
                case "change_payload":
                    if (commandData.ContainsKey("payload"))
                    {
                        droneController.HandleChangePayloadCommand(commandData["payload"].ToString());
                    }
                    break;
            }

            // 처리된 명령은 데이터베이스에서 삭제하여 중복 실행 방지
            args.Snapshot.Reference.RemoveValueAsync();
        }

        private void OnDestroy()
        {
            if (_databaseReference != null)
            {
                _databaseReference.Child("drones").Child("drone_1").Child("command").ValueChanged -= HandleCommand;
            }
        }
    }

    // DroneController와 공유할 데이터 구조체들
    [System.Serializable]
    public class DispatchData
    {
        public string mission_type;
        public SerializableVector3 target_position;

        public DispatchData(string missionType, Vector3 targetPosition)
        {
            this.mission_type = missionType;
            this.target_position = new SerializableVector3(targetPosition);
        }
    }
}
