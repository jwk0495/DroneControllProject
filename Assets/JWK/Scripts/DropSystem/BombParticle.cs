// BombParticle.cs

using UnityEngine;
using System.Collections;
using JWK.Scripts.FireManager; // LeveledFireController를 참조하기 위해 필요
using JWK.Scripts.CameraManager; // DroneCameraEvents를 참조하기 위해 필요

namespace JWK.Scripts.DropSystem
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class BombParticle : MonoBehaviour
    {
        [Header("폭탄 충돌 VFX")]
        [Tooltip("폭탄 충돌 시 생성할 VFX Prefab.")]
        [SerializeField] private GameObject impactVFXPrefab;

        [Header("유도 기능")]
        [Tooltip("유도 기능 활성화 여부")]
        [SerializeField] private bool enableGuidance = true;

        [Tooltip("목표 지점까지 유도되는 데 걸리는 시간입니다. 짧을수록 빠르게 유도됩니다.")]
        [SerializeField] private float guidanceDuration = 3.5f;

        // --- 내부 변수 ---
        private Rigidbody _rb;
        private Coroutine _guidanceCoroutine;
        private Vector3 _targetPosition;
        
        // ★★★ [핵심 수정] 중복 충돌을 막기 위한 플래그 ★★★
        private bool _hasImpacted = false;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }

        // ExtinguisherDropSystem에서 호출될 유도 시작 함수
        public void ActivateGuidance(Vector3 target)
        {
            if (!enableGuidance) return;

            _targetPosition = target;

            if (_guidanceCoroutine != null)
                StopCoroutine(_guidanceCoroutine);

            _guidanceCoroutine = StartCoroutine(GuidedFallCoroutine(_targetPosition));
        }

        // 목표 지점까지 부드럽게 이동하는 코루틴
        private IEnumerator GuidedFallCoroutine(Vector3 targetPosition)
        {
            _rb.isKinematic = true;

            Vector3 startPosition = transform.position;
            float elapsedTime = 0.0f;

            while (elapsedTime < guidanceDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.SmoothStep(0.0f, 1.0f, elapsedTime / guidanceDuration);
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }
            
            transform.position = targetPosition;
            _rb.isKinematic = false;
            _guidanceCoroutine = null;
        }

        // ★★★ [핵심 수정] 충돌 감지 로직 통합 ★★★
        // isTrigger가 켜진 'Fire'와 충돌했을 때
        private void OnTriggerEnter(Collider other)
        {
            // 이미 충돌 처리가 끝났다면 아무것도 하지 않음
            if (_hasImpacted) return;

            // 'Fire' 태그를 가진 오브젝트인지 확인
            if (other.CompareTag("Fire"))
            {
                // 부딪힌 오브젝트에서 LeveledFireController를 가져옴
                LeveledFireController fireController = other.GetComponent<LeveledFireController>();
                if (fireController != null)
                {
                    // 화재에 '피격' 신호를 보냄
                    fireController.TakeHit();
                }
                
                // 충돌 처리 실행
                HandleImpact(transform.position);
            }
        }

        // isTrigger가 꺼진 'Ground'나 'Building' 등과 충돌했을 때
        private void OnCollisionEnter(Collision collision)
        {
            // 이미 충돌 처리가 끝났다면 아무것도 하지 않음
            if (_hasImpacted) return;
            
            // 땅이든 건물이든, 어떤 물리적 충돌이라도 발생하면 폭발 처리
            HandleImpact(collision.contacts[0].point);
        }

        // ★★★ [핵심 수정] 모든 충돌의 최종 처리 함수 ★★★
        private void HandleImpact(Vector3 impactPosition)
        {
            // 충돌 플래그를 true로 설정하여 이후의 모든 충돌을 무시
            _hasImpacted = true;

            // 진행 중이던 유도 코루틴이 있다면 중지
            if (_guidanceCoroutine != null)
            {
                StopCoroutine(_guidanceCoroutine);
                _guidanceCoroutine = null;
            }
            
            // 카메라 시스템에 충돌 이벤트 알림
            DroneCameraEvents.BombImpact(impactPosition);
            
            // 폭발 VFX 생성
            if (impactVFXPrefab)
            {
                Instantiate(impactVFXPrefab, impactPosition, Quaternion.identity);
            }

            // 자기 자신(소화탄)을 씬에서 즉시 파괴
            Destroy(gameObject);
        }
    }
}