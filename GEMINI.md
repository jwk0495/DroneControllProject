# `DroneController` WebSocket to Firebase Integration Plan

## 1. 개요
이 문서는 `DroneController.cs`의 통신 계층을 기존의 WebSocket 기반 시스템(Flask 서버와 통신)에서 Google의 Firebase 실시간 데이터베이스(Realtime Database)로 마이그레이션하는 계획을 상세히 설명합니다.

## 2. 실현 가능성
이 마이그레이션은 매우 실현 가능성이 높습니다. Firebase 실시간 데이터베이스는 드론 관제와 같은 실시간 데이터 동기화 요구사항에 매우 적합하며, 기존 WebSocket 구현보다 더 안정적이고 확장성 있는 솔루션을 제공합니다.

-   **상태 동기화:** 드론의 현재 상태(위치, 배터리 등)를 특정 데이터베이스 경로에 지속적으로 덮어쓰는 방식으로 간단하게 구현할 수 있습니다.
-   **명령 처리:** 드론은 특정 데이터베이스 경로의 데이터 변경을 감지(구독)하여 외부(웹 대시보드, 서버 등)로부터의 명령을 실시간으로 수신할 수 있습니다.

## 3. 마이그레이션 절차

### 1단계: Firebase 프로젝트 설정
1.  **Firebase 프로젝트 생성:** [Firebase 콘솔](https://console.firebase.google.com/)에 접속하여 새 프로젝트를 생성합니다.
2.  **Unity 앱 추가:** 프로젝트 설정에서 '앱 추가'를 선택하고 Unity 아이콘을 클릭합니다. 화면의 안내에 따라 `google-services.json` 설정 파일을 다운로드합니다.
3.  **실시간 데이터베이스 설정:**
    *   Firebase 콘솔의 '빌드' 메뉴에서 **실시간 데이터베이스(Realtime Database)**를 선택하고 데이터베이스를 생성합니다.
    *   초기 개발 및 테스트를 위해 **'규칙'** 탭에서 보안 규칙을 다음과 같이 수정합니다.
        ```json
        {
          "rules": {
            ".read": "true",
            ".write": "true"
          }
        }
        ```
    *   **경고:** 이 규칙은 누구나 데이터베이스를 읽고 쓸 수 있게 하므로, 프로덕션 환경에서는 반드시 인증 규칙을 강화해야 합니다.

### 2단계: Unity 프로젝트에 Firebase SDK 추가
1.  **SDK 다운로드:** [Firebase Unity SDK 공식 사이트](https://firebase.google.com/docs/unity/setup)에서 `.NET Framework` 버전의 SDK(`firebase_unity_sdk_...zip`)를 다운로드합니다.
2.  **패키지 임포트:** 다운로드한 파일의 압축을 해제한 후, Unity 에디터의 `Assets -> Import Package -> Custom Package...` 메뉴를 통해 다음 두 가지 필수 패키지를 프로젝트로 가져옵니다.
    *   `FirebaseDatabase.unitypackage`
    *   `FirebaseCore.unitypackage`
3.  **설정 파일 추가:** 1단계에서 다운로드한 `google-services.json` 파일을 Unity 프로젝트의 `Assets` 폴더 최상단으로 드래그 앤 드롭합니다.

### 3단계: `DroneController.cs` 코드 리팩토링
기존의 모든 WebSocket 관련 코드를 Firebase SDK 호출로 교체하는 작업입니다.

1.  **WebSocket 관련 코드 제거:**
    *   `using WebSocketSharp;` 구문을 제거합니다.
    *   `_ws`, `ServerUrl`, `_webSocketCts`, `_socketMessageBuilder` 등 WebSocket 통신에 사용된 모든 변수를 삭제합니다.
    *   `ConnectWebSocket`, `ReconnectWebSocket`, `OnWebSocketMessage` 등 WebSocket 관련 메서드를 모두 삭제합니다.

2.  **Firebase 변수 추가 및 초기화:**
    ```csharp
    // 새로운 using 구문 추가
    using Firebase;
    using Firebase.Database;
    using Firebase.Extensions; // ContinueWithOnMainThread 사용을 위함

    public class DroneController : MonoBehaviour
    {
        // Firebase 데이터베이스 참조를 저장할 변수 추가
        private DatabaseReference _databaseReference;

        void Start()
        {
            // Firebase 초기화 로직 추가
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
                if (task.Exception != null) {
                    Debug.LogError($"Firebase 의존성 확인 중 오류 발생: {task.Exception}");
                    return;
                }

                FirebaseApp app = FirebaseApp.DefaultInstance;
                _databaseReference = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase 초기화 및 데이터베이스 참조 설정 완료.");

                // Firebase가 준비된 후 데이터 전송 및 명령 수신 시작
                StartCoroutine(SendDroneDataRoutine());
                ListenForCommands();
            });
            // ... 기존 Start() 함수의 나머지 부분 ...
        }
    }
    ```

3.  **데이터 전송 로직 수정 (`SendDroneDataRoutine`):**
    *   `_ws.Send(...)` 호출 부분을 `_databaseReference.Child(...).SetRawJsonValueAsync(...)`로 교체하여 Firebase에 데이터를 씁니다.
    ```csharp
    private IEnumerator SendDroneDataRoutine()
    {
        while (true)
        {
            yield return _sendDataWait;
            if (_databaseReference != null)
            {
                // ... (기존처럼 _dataToSend 객체에 상태 값 채우기) ...
                string droneDataJson = JsonUtility.ToJson(_dataToSend);
                
                // "drones/drone_1/status" 경로에 드론의 상태 데이터를 JSON 형태로 덮어쓰기
                _databaseReference.Child("drones").Child("drone_1").Child("status").SetRawJsonValueAsync(droneDataJson);
            }
        }
    }
    ```

4.  **명령 수신 로직 구현:**
    *   데이터베이스의 특정 경로에 변경이 생길 때마다 호출될 리스너(Listener)를 등록하는 새로운 메서드를 작성합니다.
    ```csharp
    private void ListenForCommands()
    {
        // "drones/drone_1/command" 경로의 데이터 변경을 감지하는 리스너 추가
        _databaseReference.Child("drones").Child("drone_1").Child("command").ValueChanged += HandleCommand;
    }

    private void HandleCommand(object sender, ValueChangedEventArgs args)
    {
        if (args.DatabaseError != null) { Debug.LogError(args.DatabaseError.Message); return; }
        if (args.Snapshot == null || !args.Snapshot.Exists) { return; }

        var commandData = args.Snapshot.Value as Dictionary<string, object>;
        if (commandData == null || !commandData.ContainsKey("type")) return;

        string commandType = commandData["type"].ToString();
        Debug.Log($"[Firebase] 수신된 명령: {commandType}");

        // 명령 타입에 따라 기존 핸들러 함수 호출
        switch (commandType)
        {
            case "force_return":
                HandleForceReturnCommand();
                break;
            case "emergency_stop":
                HandleEmergencyStopCommand();
                break;
            // ... 다른 명령 케이스들 ...
        }

        // 처리된 명령은 데이터베이스에서 삭제하여 중복 실행을 방지
        args.Snapshot.Reference.RemoveValueAsync();
    }

    void OnDestroy()
    {
        // 씬이 파괴될 때 리스너를 정리하여 메모리 누수 방지
        if (_databaseReference != null)
        {
            _databaseReference.Child("drones").Child("drone_1").Child("command").ValueChanged -= HandleCommand;
        }
        // ... 기존 OnDestroy() 코드 ...
    }
    ```

5.  **임무 파견 데이터 전송 로직 수정 (`SendDispatchDataToServer`):**
    *   WebSocket 전송 대신, `dispatches`라는 새로운 리스트에 `Push()`를 사용하여 고유 ID와 함께 임무 데이터를 추가합니다.
    ```csharp
    private void SendDispatchDataToServer(string missionType, Vector3 targetPosition)
    {
        if (_databaseReference == null) return;
        DispatchData dispatchData = new DispatchData(missionType, targetPosition);
        string dispatchJson = JsonUtility.ToJson(dispatchData);
        
        // "dispatches" 리스트에 새 임무 데이터 추가
        _databaseReference.Child("dispatches").Push().SetRawJsonValueAsync(dispatchJson);
    }
    ```

### 4단계: 테스트 및 검증
1.  **Unity에서 실행:** Unity 에디터에서 씬을 실행하고, 콘솔 창에 "Firebase 초기화 완료" 로그가 출력되는지 확인합니다.
2.  **Firebase 콘솔에서 데이터 확인:**
    *   웹 브라우저에서 Firebase 프로젝트의 실시간 데이터베이스를 엽니다.
    *   `drones/drone_1/status` 경로에 드론의 상태 정보가 실시간으로 업데이트되는 것을 확인합니다.
3.  **Firebase 콘솔에서 명령 전송:**
    *   `drones/drone_1` 경로 옆의 `+` 버튼을 눌러 `command` 자식 노드를 추가합니다.
    *   `command` 노드에 자식으로 `type` (값: `emergency_stop`)을 추가합니다.
    *   데이터를 추가하는 즉시 Unity 에디터의 드론이 정지하는지, 그리고 데이터베이스에서 해당 `command` 노드가 자동으로 삭제되는지 확인합니다.
