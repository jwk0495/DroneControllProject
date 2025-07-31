# `DroneController` WebSocket to Firebase Firestore Integration Plan

## 1. 개요
이 문서는 `DroneController.cs`의 통신 계층을 기존의 WebSocket 기반 시스템에서 Google의 **Cloud Firestore**로 마이그레이션하는 계획을 상세히 설명합니다.

## 2. 실현 가능성
이 마이그레이션은 매우 실현 가능성이 높습니다. Cloud Firestore는 실시간 데이터 동기화, 구조화된 데이터 저장, 강력한 쿼리 기능을 제공하여 드론 관제 시스템에 매우 적합합니다. 기존 WebSocket 구현보다 더 안정적이고 확장성 있는 솔루션을 제공합니다.

-   **상태 동기화:** 드론의 현재 상태(위치, 배터리 등)를 특정 문서에 지속적으로 덮어쓰는 방식으로 간단하게 구현할 수 있습니다.
-   **명령 처리:** 드론은 특정 문서를 실시간으로 수신 대기(Listen)하여 외부(웹 대시보드, 서버 등)로부터의 명령을 즉시 처리할 수 있습니다.

## 3. 마이그레이션 절차

### 1단계: Firebase 프로젝트 설정
1.  **Firebase 프로젝트 생성:** [Firebase 콘솔](https://console.firebase.google.com/)에 접속하여 새 프로젝트를 생성합니다.
2.  **Unity 앱 추가:** 프로젝트 설정에서 '앱 추가'를 선택하고 Unity 아이콘을 클릭합니다. 화면의 안내에 따라 `google-services.json` 설정 파일을 다운로드합니다.
3.  **Cloud Firestore 설정:**
    *   Firebase 콘솔의 '빌드' 메뉴에서 **Cloud Firestore**를 선택하고 데이터베이스를 생성합니다.
    *   초기 개발 및 테스트를 위해 **'규칙'** 탭에서 보안 규칙을 다음과 같이 수정합니다.
        ```
        rules_version = '2';
        service cloud.firestore {
          match /databases/{database}/documents {
            match /{document=**} {
              allow read, write: if true;
            }
          }
        }
        ```
    *   **경고:** 이 규칙은 누구나 데이터베이스를 읽고 쓸 수 있게 하므로, 프로덕션 환경에서는 반드시 인증 규칙을 강화해야 합니다.

### 2단계: Unity 프로젝트에 Firebase SDK 추가
1.  **SDK 다운로드:** [Firebase Unity SDK 공식 사이트](https://firebase.google.com/docs/unity/setup)에서 `.NET Framework` 버전의 SDK(`firebase_unity_sdk_...zip`)를 다운로드합니다.
2.  **패키지 임포트:** 다운로드한 파일의 압축을 해제한 후, Unity 에디터의 `Assets -> Import Package -> Custom Package...` 메뉴를 통해 다음 패키지를 프로젝트로 가져옵니다.
    *   `FirebaseFirestore.unitypackage` (Core SDK가 포함되어 있습니다)
3.  **설정 파일 추가:** 1단계에서 다운로드한 `google-services.json` 파일을 Unity 프로젝트의 `Assets` 폴더 최상단으로 드래그 앤 드롭합니다.

### 3단계: 코드 리팩토링
기존의 모든 WebSocket 관련 코드를 Firebase Firestore SDK 호출로 교체하고, 관련 스크립트를 모듈화하는 작업입니다.

1.  **`DroneController.cs` 리팩토링:**
    *   **WebSocket 관련 코드 완전 제거:** `using WebSocketSharp;`, `using SimpleJSON;` 구문 및 관련 변수(`_ws`, `ServerUrl` 등), 메서드 (`ConnectWebSocket`, `OnWebSocketMessage` 등)를 모두 삭제합니다.
    *   **FirebaseManager 참조 추가:** `FirebaseManager`와 통신하기 위한 `[SerializeField] private FirebaseManager firebaseManager;` 변수를 추가합니다.
    *   **명령 처리 메서드 `public` 전환:** `HandleForceReturnCommand`, `HandleEmergencyStopCommand`, `HandleChangePayloadCommand` 메서드를 `public`으로 변경하여 `FirebaseManager`가 호출할 수 있도록 합니다.
    *   **상태 조회 메서드 추가:** 드론의 현재 상태를 `FirebaseManager`에 전달하기 위한 `public DroneStatusData GetCurrentStatusData()` 메서드를 구현합니다.
    *   **임무 파견 로직 수정:** `SendDispatchDataToServer` 메서드가 `firebaseManager.SendDispatchData()`를 호출하도록 수정합니다.

2.  **`FirebaseManager.cs` 구현 (Cloud Firestore 사용):**
    *   **Firestore 변수 추가 및 초기화:**
        ```csharp
        // using Firebase.Database; // -> 제거
        using Firebase.Firestore; // -> 추가
        
        public class FirebaseManager : MonoBehaviour
        {
            private FirebaseFirestore _firestore;
            private ListenerRegistration _commandListener;

            void Start()
            {
                FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
                    // ... (오류 처리) ...
                    _firestore = FirebaseFirestore.DefaultInstance;
                    // ... (리스너 등록 및 데이터 전송 코루틴 시작) ...
                });
            }
        }
        ```
    *   **데이터 전송 로직 (`SendDroneStatus`):**
        *   `DroneController`로부터 `DroneStatusData`를 받아와 `Dictionary<string, object>` 형태로 변환합니다.
        *   `_firestore.Collection("drones").Document("drone_1").SetAsync(statusDict)`를 사용하여 "drones" 컬렉션의 "drone_1" 문서에 데이터를 덮어씁니다.
    *   **명령 수신 로직 (`ListenForCommands`):**
        *   `_firestore.Collection("drones").Document("drone_1").Listen(snapshot => { ... })`을 사용하여 문서 변경을 실시간으로 감지합니다.
        *   `snapshot`에서 `command` 필드를 확인하고, 값에 따라 `DroneController`의 해당 `public` 메서드를 호출합니다.
        *   명령 처리 후, `snapshot.Reference.UpdateAsync`와 `FieldValue.Delete`를 사용하여 처리된 `command` 관련 필드를 문서에서 삭제하여 중복 실행을 방지합니다.
    *   **임무 파견 데이터 전송 (`SendDispatchData`):**
        *   `_firestore.Collection("dispatches").AddAsync(dispatchData)`를 사용하여 "dispatches" 컬렉션에 새 임무 문서를 추가합니다. (고유 ID 자동 생성)
    *   **리소스 정리 (`OnDestroy`):**
        *   `_commandListener?.Stop()`을 호출하여 씬이 파괴될 때 리스너를 안전하게 제거합니다.

3.  **`DataModels.cs` 파일 생성:**
    *   **코드 모듈화:** `DroneController`와 `FirebaseManager`가 공통으로 사용하는 데이터 구조체(`DroneStatusData`, `DispatchData`, `SerializableVector3`)와 열거형(`DroneMissionState`, `PayloadType`)을 `Assets/JWK/Scripts/DataModels.cs`라는 별도의 파일로 분리하여 관리의 용이성과 코드 재사용성을 높입니다.

### 4단계: 테스트 및 검증
1.  **Unity에서 설정:**
    *   `DroneController`와 `FirebaseManager`가 서로의 컴포넌트를 인스펙터에서 참조하도록 설정합니다.
2.  **Unity에서 실행:** Unity 에디터에서 씬을 실행하고, 콘솔 창에 "Firebase 초기화 완료" 로그가 출력되는지 확인합니다.
3.  **Firebase 콘솔에서 데이터 확인:**
    *   웹 브라우저에서 Firebase 프로젝트의 Cloud Firestore를 엽니다.
    *   `drones/drone_1` 문서에 드론의 상태 정보(`position`, `battery` 등)가 실시간으로 업데이트되는 것을 확인합니다.
4.  **Firebase 콘솔에서 명령 전송:**
    *   `drones/drone_1` 문서에 필드를 추가합니다.
    *   `command` (타입: string, 값: `emergency_stop`) 필드를 추가합니다.
    *   데이터를 추가하는 즉시 Unity 에디터의 드론이 정지하는지, 그리고 데이터베이스에서 해당 `command` 필드가 자동으로 삭제되는지 확인합니다.
    *   `command` (값: `change_payload`)와 `command_payload` (값: `RescueKit`) 필드를 추가하여 페이로드 변경을 테스트합니다.