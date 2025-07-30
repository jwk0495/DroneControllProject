using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JWK.Scripts.Station
{
    /// <summary>
    /// 스테이션 지붕을 순차적으로 열고 닫는 것을 제어하는 독립적인 스크립트입니다.
    /// 키보드 O(열기), C(닫기)로 테스트할 수 있습니다.
    /// 이 스크립트는 지붕 파츠들이 부모-자식 관계로 설정되었을 때 정상적으로 작동합니다.
    /// </summary>
    public class RoofManager : MonoBehaviour
    {
        [Header("지붕 파츠 설정")]
        [Tooltip("부모에서 자식 순서로 지붕 오브젝트를 할당해주세요. (예: [roof2, roof3, roof4])")]
        public List<GameObject> roofParts;

        [Header("애니메이션 설정")]
        //====================================================================================
        // [수정된 부분] 변수명을 더 명확하게 변경하고, 겹치는 애니메이션을 위한 staggerDelay를 추가했습니다.
        [Tooltip("각 지붕 파츠 하나가 움직이는 데 걸리는 시간(초)입니다.")]
        [SerializeField] private float partMoveDuration = 1.5f;
        [Tooltip("이전 파츠가 움직이기 시작한 후 다음 파츠가 움직이기 시작할 때까지의 시간 간격입니다. partMoveDuration보다 짧게 설정하면 움직임이 겹쳐 보입니다.")]
        [SerializeField] private float staggerDelay = 1.2f; 
        //====================================================================================
        [Tooltip("지붕이 이동할 거리와 방향의 기준값입니다.")]
        [SerializeField] private Vector3 moveOffset = new Vector3(1.4f, 0, 0);

        private Vector3[] _initialLocalPositions;
        private Vector3[] _targetLocalPositions;
        
        private bool _isRoofOpen = false;
        private Coroutine _roofCoroutine;

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

        public void Open()
        {
            if (!_isRoofOpen)
            {
                Debug.Log("지붕을 엽니다...");
                MoveRoof(true);
            }
        }

        public void Close()
        {
            if (_isRoofOpen)
            {
                Debug.Log("지붕을 닫습니다...");
                MoveRoof(false);
            }
        }

        private void MoveRoof(bool open)
        {
            if (_roofCoroutine != null)
            {
                StopCoroutine(_roofCoroutine);
            }
            _roofCoroutine = StartCoroutine(MoveSequenceCoroutine(open));
            _isRoofOpen = open;
        }

        //====================================================================================
        // [수정된 부분] 겹치는 순차 애니메이션을 구현하는 로직입니다.
        private IEnumerator MoveSequenceCoroutine(bool open)
        {
            // 닫기: 리스트의 끝(자식, 4번)부터 처음(부모, 2번) 순서로 움직입니다.
            if (!open)
            {
                for (int i = roofParts.Count - 1; i >= 0; i--)
                {
                    if (roofParts[i] != null)
                    {
                        // 각 파츠의 애니메이션을 개별적으로 시작합니다.
                        StartCoroutine(AnimatePartCoroutine(i, _targetLocalPositions[i], _initialLocalPositions[i]));
                        // 다음 파츠 애니메이션을 시작하기 전에 staggerDelay 만큼 기다립니다.
                        if (i > 0) yield return new WaitForSeconds(staggerDelay);
                    }
                }
            }
            // 열기: 리스트의 처음(부모, 2번)부터 끝(자식, 4번) 순서로 움직입니다.
            else
            {
                for (int i = 0; i < roofParts.Count; i++)
                {
                    if (roofParts[i] != null)
                    {
                        StartCoroutine(AnimatePartCoroutine(i, _initialLocalPositions[i], _targetLocalPositions[i]));
                        if (i < roofParts.Count - 1) yield return new WaitForSeconds(staggerDelay);
                    }
                }
            }
            
            // 마지막 파츠의 애니메이션이 끝날 때까지 추가로 기다립니다.
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

            // 애니메이션이 끝난 후, 정확한 최종 위치에 도달하도록 위치를 한 번 더 설정합니다.
            roofParts[index].transform.localPosition = endPos;
        }
        //====================================================================================

        public static float EaseInOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }
    }
}
