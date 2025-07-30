using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JWK.Scripts.Station
{
    public class RoofManager : MonoBehaviour
    {
        [Header("지붕 파츠 설정")]
        [Tooltip("부모에서 자식 순서로 지붕 오브젝트를 할당해주세요. (예: [roof2, roof3, roof4])")]
        public List<GameObject> roofParts;

        [Header("애니메이션 설정")]
        [Tooltip("각 지붕 파츠 하나가 움직이는 데 걸리는 시간(초)입니다.")]
        [SerializeField] private float partMoveDuration = 1.5f;
        [Tooltip("이전 파츠가 움직이기 시작한 후 다음 파츠가 움직이기 시작할 때까지의 시간 간격입니다. partMoveDuration보다 짧게 설정하면 움직임이 겹쳐 보입니다.")]
        [SerializeField] private float staggerDelay = 1.2f; 
        [Tooltip("지붕이 이동할 거리와 방향의 기준값입니다.")]
        [SerializeField] private Vector3 moveOffset = new Vector3(1.4f, 0, 0);

        private Vector3[] _initialLocalPositions;
        private Vector3[] _targetLocalPositions;
        
        private bool _isRoofOpen = false;
        private Coroutine _roofCoroutine;

        // ... Start() 메서드는 기존과 동일 ...
        void Start()
        {
            if (roofParts == null || roofParts.Count == 0)
            {
                Debug.LogError("Roof Parts 리스트가 비어있습니다! 인스펙터에서 지붕 오브젝트를 할당해주세요.", this);
                enabled = false; 
                return;
            }

            _initialLocalPositions = new Vector3[roofParts.Count];
            _targetLocalPositions = new Vector3[roofParts.Count];
            
            for(int i = 0; i < roofParts.Count; i++)
            {
                if (roofParts[i] != null)
                {
                    _initialLocalPositions[i] = roofParts[i].transform.localPosition;
                    _targetLocalPositions[i] = _initialLocalPositions[i] + moveOffset;
                }
                else
                {
                    Debug.LogError($"Roof Parts 리스트의 {i}번째 항목이 비어있습니다.", this);
                }
            }
        }


        // [수정] 테스트용 Update()는 제거하거나 주석 처리합니다.
        /*
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.O))
            {
                Open();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                Close();
            }
        }
        */

        // [수정] 메서드가 Coroutine을 반환하도록 변경합니다.
        public Coroutine Open()
        {
            if (!_isRoofOpen)
            {
                Debug.Log("지붕을 엽니다...");
                return MoveRoof(true);
            }
            return null;
        }

        // [수정] 메서드가 Coroutine을 반환하도록 변경합니다.
        public Coroutine Close()
        {
            if (_isRoofOpen)
            {
                Debug.Log("지붕을 닫습니다...");
                return MoveRoof(false);
            }
            return null;
        }

        // [수정] 메서드가 Coroutine을 반환하도록 변경합니다.
        private Coroutine MoveRoof(bool open)
        {
            if (_roofCoroutine != null)
            {
                StopCoroutine(_roofCoroutine);
            }
            _roofCoroutine = StartCoroutine(MoveSequenceCoroutine(open));
            _isRoofOpen = open;
            return _roofCoroutine;
        }
        
        // ... 나머지 코드는 기존과 동일 ...
        private IEnumerator MoveSequenceCoroutine(bool open)
        {
            // 닫기: 리스트의 끝(자식, 4번)부터 처음(부모, 2번) 순서로 움직입니다.
            if (!open)
            {
                for (int i = roofParts.Count - 1; i >= 0; i--)
                {
                    if (roofParts[i])
                    {
                        StartCoroutine(AnimatePartCoroutine(i, _targetLocalPositions[i], _initialLocalPositions[i]));
                        if (i > 0) yield return new WaitForSeconds(staggerDelay);
                    }
                }
            }
            // 열기: 리스트의 처음(부모, 2번)부터 끝(자식, 4번) 순서로 움직입니다.
            else
            {
                for (int i = 0; i < roofParts.Count; i++)
                {
                    if (roofParts[i])
                    {
                        StartCoroutine(AnimatePartCoroutine(i, _initialLocalPositions[i], _targetLocalPositions[i]));
                        if (i < roofParts.Count - 1) yield return new WaitForSeconds(staggerDelay);
                    }
                }
            }
            
            yield return new WaitForSeconds(partMoveDuration);
            
            _roofCoroutine = null;
        }

        private IEnumerator AnimatePartCoroutine(int index, Vector3 startPos, Vector3 endPos)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < partMoveDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = EaseInOutCubic(elapsedTime / partMoveDuration);
                roofParts[index].transform.localPosition = Vector3.LerpUnclamped(startPos, endPos, t);
                yield return null;
            }
            
            roofParts[index].transform.localPosition = endPos;
        }
        
        public static float EaseInOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }
    }
}
