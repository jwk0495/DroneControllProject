// ExtinguisherDropSystem.cs

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JWK.Scripts.DropSystem
{
    public class ExtinguisherDropSystem : MonoBehaviour
    {
        [Header("회전 및 투하 대상")] [Tooltip("안쪽 로터의 Transform을 할당하세요.")] [SerializeField]
        private Transform rotaryIn;

        [Tooltip("바깥쪽 로터의 Transform을 할당하세요.")] [SerializeField]
        private Transform rotaryOut;

        [Tooltip("모든 폭탄(Bomb_1, Bomb_2 등)을 담고 있는 부모 오브젝트를 할당하세요.")] [SerializeField]
        private Transform bombsParent;

        [Tooltip("이 리스트는 시작 시 자동으로 채워지므로, Inspector에서 직접 수정할 필요 없습니다.")] [SerializeField]
        private List<GameObject> bombList;

        [Header("애니메이션 설정")] [Tooltip("로터가 회전하는 속도입니다 (도/초).")] [SerializeField]
        private float rotationSpeed = 180.0f;

        [Tooltip("각 행동 사이의 대기 시간입니다 (초).")] [SerializeField]
        private float delayBetweenActions = 1.0f;

        // --- 내부 변수 ---
        private bool _isActionInProgress = false;
        private int _bombsDroppedCount = 0;

        // --- 코루틴 캐싱 (GC 최적화) ---
        private WaitForSeconds _actionDelayWait;
        private readonly WaitForSeconds _clearanceDelay = new WaitForSeconds(0.5f);
        // [추가] 물리 프레임 대기를 위한 캐싱
        private readonly WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();

        private void Awake()
        {
            _actionDelayWait = new WaitForSeconds(delayBetweenActions);
            PopulateBombList();
        }

        private void PopulateBombList()
        {
            if (bombsParent == null)
            {
                Debug.LogError("Bombs Parent가 할당되지 않았습니다!", this.gameObject);
                return;
            }

            bombList = new List<GameObject>();
            foreach (Transform bombTransform in bombsParent)
            {
                bombList.Add(bombTransform.gameObject);
            }

            Debug.Log($"{bombList.Count}개의 폭탄이 자동으로 리스트에 추가되었습니다.", this.gameObject);
        }

        public void ResetBombs()
        {
            _bombsDroppedCount = 0;
        }

        public Vector3 GetNextBombOffsetFromDroneRoot(Transform droneRoot)
        {
            if (_bombsDroppedCount >= bombList.Count) return Vector3.zero;
            GameObject nextBomb = bombList[_bombsDroppedCount];
            if (!nextBomb) return Vector3.zero;
            return droneRoot.InverseTransformPoint(nextBomb.transform.position);
        }

        public Vector3 GetNextBombWorldPosition()
        {
            if (_bombsDroppedCount >= bombList.Count) return transform.position;
            GameObject nextBomb = bombList[_bombsDroppedCount];
            return nextBomb != null ? nextBomb.transform.position : transform.position;
        }

        public IEnumerator DropSingleBomb(Vector3 targetPosition, Transform droneTransform)
        {
            if (_isActionInProgress || _bombsDroppedCount >= bombList.Count)
            {
                yield break;
            }

            _isActionInProgress = true;
            Vector3 finalTargetPostion = targetPosition;

            // [수정] DetachBombByIndex 대신 새로운 코루틴을 호출하도록 변경
            switch (_bombsDroppedCount)
            {
                case 0:
                    yield return StartCoroutine(RotateAndDropSequence(_bombsDroppedCount, -45f, droneTransform));
                    yield return StartCoroutine(DetachAndDropBombCoroutine(_bombsDroppedCount, finalTargetPostion));
                    break;
                case 1:
                    yield return StartCoroutine(RotateAndDropSequence(_bombsDroppedCount, -45f, droneTransform));
                    yield return StartCoroutine(DetachAndDropBombCoroutine(_bombsDroppedCount, finalTargetPostion));
                    yield return _actionDelayWait;
                    yield return StartCoroutine(ReloadSequence(1));
                    break;
                case 2:
                    yield return StartCoroutine(RotateAndDropSequence(_bombsDroppedCount, -45f, droneTransform));
                    yield return StartCoroutine(DetachAndDropBombCoroutine(_bombsDroppedCount, finalTargetPostion));
                    break;
                case 3:
                    yield return StartCoroutine(RotateAndDropSequence(_bombsDroppedCount, -45f, droneTransform));
                    yield return StartCoroutine(DetachAndDropBombCoroutine(_bombsDroppedCount, finalTargetPostion));
                    yield return _actionDelayWait;
                    yield return StartCoroutine(ReloadSequence(2));
                    break;
                case 4:
                    yield return StartCoroutine(RotateAndDropSequence(_bombsDroppedCount, -45f, droneTransform));
                    yield return StartCoroutine(DetachAndDropBombCoroutine(_bombsDroppedCount, finalTargetPostion));
                    break;
                case 5:
                    yield return StartCoroutine(RotateAndDropSequence(_bombsDroppedCount, -45f, droneTransform));
                    yield return StartCoroutine(DetachAndDropBombCoroutine(_bombsDroppedCount, finalTargetPostion));
                    yield return _actionDelayWait;
                    yield return StartCoroutine(ReloadSequence(3));
                    break;
            }

            _bombsDroppedCount++;
            _isActionInProgress = false;
        }

        private IEnumerator RotateAndDropSequence(int bombIndex, float angle, Transform droneTransform)
        {
            yield return StartCoroutine(RotateRotor(rotaryOut, angle));
            GameObject bombToDrop = bombList[bombIndex];
            if (bombToDrop)
            {
                Vector3 bombPositionAfterRotation = bombToDrop.transform.position;
                Vector3 offset = bombPositionAfterRotation - droneTransform.position;
                Debug.Log($"<color=yellow>[투하 직전 오프셋 계산]</color> 드론-폭탄 간 최종 오프셋: {offset}");
            }
            yield return _clearanceDelay;
        }

        private IEnumerator ReloadSequence(int sequenceNumber)
        {
            yield return StartCoroutine(RotateRotor(rotaryIn, -60f));
            yield return StartCoroutine(RotateRotor(rotaryOut, 90f));
        }

        // --- [수정된 부분] ---
        // 기존 DetachBombByIndex 함수를 코루틴으로 변경하여 물리 문제를 해결합니다.
        private IEnumerator DetachAndDropBombCoroutine(int index, Vector3 targetPosition)
        {
            if (bombList == null || index < 0 || index >= bombList.Count)
                yield break;

            GameObject bombToDrop = bombList[index];
            if (bombToDrop)
            {
                // Rigidbody 컴포넌트를 미리 가져옵니다.
                if (bombToDrop.TryGetComponent<Rigidbody>(out var bombRb))
                {
                    // 1. 부모-자식 관계를 해제합니다.
                    bombToDrop.transform.SetParent(null);
                    
                    // 2. 물리 제어를 활성화합니다.
                    bombRb.isKinematic = false;
                    bombRb.useGravity = true;

                    // 3. (핵심) 다음 물리 프레임까지 대기합니다.
                    // 이 한 프레임 동안 물리 엔진이 상속된 속도를 적용할 수 있습니다.
                    yield return _waitForFixedUpdate;

                    // 4. 이제 어떤 속도가 적용되었든 상관없이 강제로 0으로 만듭니다.
                    bombRb.linearVelocity = Vector3.zero;
                    bombRb.angularVelocity = Vector3.zero;

                    // 5. 속도 문제가 해결되었으니 나머지 로직을 실행합니다.
                    StartCoroutine(RotateBombToGround(bombToDrop));
                    
                    if (bombToDrop.TryGetComponent<BombParticle>(out var bombParticle))
                        bombParticle.ActivateGuidance(targetPosition);
                }
            }
        }
        // --- [수정 끝] ---

        #region 코루틴 헬퍼 함수 (애니메이션)

        private IEnumerator RotateRotor(Transform rotor, float angle)
        {
            Quaternion startRot = rotor.localRotation;
            Quaternion targetRot = startRot * Quaternion.Euler(angle, 0, 0);
            float duration = Mathf.Abs(angle) / rotationSpeed;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                rotor.localRotation = Quaternion.SlerpUnclamped(startRot, targetRot, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            rotor.localRotation = targetRot;
        }

        private IEnumerator RotateBombToGround(GameObject bomb)
        {
            yield return new WaitForSeconds(0.5f);
            if (!bomb) yield break;

            float rotationDuration = 2.0f;
            float elapsedTime = 0f;
            Quaternion startRotation = bomb.transform.rotation;
            Quaternion targetRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);

            while (elapsedTime < rotationDuration)
            {
                if (!bomb) yield break;
                bomb.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsedTime / rotationDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (bomb) bomb.transform.rotation = targetRotation;
        }

        #endregion
    }
}