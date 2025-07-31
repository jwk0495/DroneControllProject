using UnityEngine;
using System.Collections;

namespace JWK.Scripts.FireManager
{
    // 파티클 시스템과 트리거 콜라이더를 필수로 요구합니다.
    [RequireComponent(typeof(ParticleSystem))]
    [RequireComponent(typeof(Collider))]
    public class LeveledFireController : MonoBehaviour
    {
        [Header("화재 레벨 설정")]
        [Tooltip("이 불을 완전히 끄는 데 필요한 소화탄의 개수입니다.")]
        [SerializeField] private int bombsToExtinguish = 5;

        [Header("VFX 설정")]
        [Tooltip("피격 시 불의 크기가 줄어드는 시간입니다.")]
        [SerializeField] private float sizeChangeDuration = 1.0f;

        private ParticleSystem fireParticleSystem;
        private int currentHits = 0;
        private float initialStartSize;
        private float initialEmissionRate;
        
        private bool isExtinguished = false;

        // ★★★ [핵심 수정] 외부에서 현재 진압 상태를 확인할 수 있는 프로퍼티 추가 ★★★
        public bool IsExtinguished => isExtinguished;

        void Awake()
        {
            fireParticleSystem = GetComponent<ParticleSystem>();

            // 콜라이더를 트리거로 설정하여 물리적 충돌 대신 감지만 하도록 합니다.
            GetComponent<Collider>().isTrigger = true;

            // 파티클 시스템의 최대 크기일 때의 값을 저장합니다.
            initialStartSize = fireParticleSystem.main.startSize.constant;
            initialEmissionRate = fireParticleSystem.emission.rateOverTime.constant;
        }

        // 화재가 활성화될 때마다 상태를 초기화합니다.
        private void OnEnable()
        {
            currentHits = 0;
            isExtinguished = false;

            // 파티클 시스템을 최대 크기로 리셋합니다.
            var mainModule = fireParticleSystem.main;
            var emissionModule = fireParticleSystem.emission;
            mainModule.startSize = initialStartSize;
            emissionModule.rateOverTime = initialEmissionRate;

            if (!fireParticleSystem.isPlaying)
            {
                fireParticleSystem.Play();
            }
            Debug.Log($"<color=green><b>[화재 활성화 & 초기화]</b></color> {this.name}의 상태가 리셋되었습니다. Current Hits: {currentHits}", this.gameObject);
        }

        // 소화탄에 의해 호출될 함수
        public void TakeHit()
        {
            if (isExtinguished) return;

            Debug.Log($"<color=orange><b>[피격!]</b></color> Frame: {Time.frameCount}, 현재 Hit Count: {currentHits}, 목표치: {bombsToExtinguish}");
            
            currentHits++;
            
            int remainingHealth = bombsToExtinguish - currentHits;
            Debug.Log($"<color=orange><b>[화재 피격!]</b></color> Hit Count: {currentHits}. 남은 내구도: {remainingHealth} / {bombsToExtinguish}");

            // 진행 중인 크기 변경 코루틴이 있다면 중지하고 새로 시작합니다.
            StopCoroutine("UpdateFireVFXSmoothly");
            StartCoroutine("UpdateFireVFXSmoothly");

            // 내구도가 0 이하면 화재 진압을 시작합니다.
            if (currentHits >= bombsToExtinguish)
            {
                isExtinguished = true;
                StartCoroutine(ExtinguishFire());
            }
        }

        // 화재의 시각적 효과를 부드럽게 업데이트하는 코루틴
        private IEnumerator UpdateFireVFXSmoothly()
        {
            // 목표 진행률 (1.0: 최대 크기 -> 0.0: 소멸)
            float targetProgress = 1f - ((float)currentHits / bombsToExtinguish);

            var mainModule = fireParticleSystem.main;
            var emissionModule = fireParticleSystem.emission;

            float currentSize = mainModule.startSize.constant;
            float currentEmission = emissionModule.rateOverTime.constant;

            float targetSize = initialStartSize * targetProgress;
            float targetEmission = initialEmissionRate * targetProgress;

            float elapsedTime = 0f;
            while (elapsedTime < sizeChangeDuration)
            {
                float t = elapsedTime / sizeChangeDuration;
                mainModule.startSize = Mathf.Lerp(currentSize, targetSize, t);
                emissionModule.rateOverTime = Mathf.Lerp(currentEmission, targetEmission, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 정확한 값으로 보정
            mainModule.startSize = targetSize;
            emissionModule.rateOverTime = targetEmission;
        }

        // 화재를 완전히 끄는 코루틴
        private IEnumerator ExtinguishFire()
        {
            // isExtinguished = true;
            Debug.Log("<color=cyan>화재 진압 완료!</color>");

            // 크기 변경 애니메이션이 끝날 때까지 기다립니다.
            yield return new WaitForSeconds(sizeChangeDuration);
            fireParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            // 파티클이 모두 사라질 시간을 준 뒤, 오브젝트를 비활성화합니다.
            yield return new WaitForSeconds(fireParticleSystem.main.startLifetime.constant);
            gameObject.SetActive(false);
        }

        /*
        // 소화탄과의 충돌을 감지하는 트리거
        private void OnTriggerEnter(Collider other)
        {
            if (isExtinguished) return;

            // "Bomb" 태그를 가진 오브젝트와 충돌했는지 확인
            if (other.CompareTag("Bomb"))
            {
                // 피격 처리
                TakeHit();

                // 충돌한 소화탄의 파괴는 BombParticle 스크립트가 스스로 처리하도록 합니다.
                // 만약 BombParticle에 파괴 로직이 없다면 아래 라인의 주석을 해제하세요.
                // Destroy(other.gameObject); 
            }
        }
        */
    }
}
