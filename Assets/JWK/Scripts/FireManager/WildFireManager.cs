using UnityEngine;
using System.Collections.Generic;

namespace JWK.Scripts.FireManager
{
    public class WildfireManager : MonoBehaviour
    {
        public static WildfireManager Instance { get; private set; }
        public bool isFireActive => _activeFire != null && _activeFire.activeInHierarchy;
        // [수정] 이제 단일 화재 오브젝트의 Transform을 반환하여 드론이 조준할 수 있게 합니다.
        public Transform GetActiveFireTarget() => _activeFire != null ? _activeFire.transform : null;

        [Header("화재 설정")]
        [SerializeField] private Terrain targetTerrain;
        // [수정] 프리팹 변수 이름을 명확하게 변경했습니다. 인스펙터에서 새로 할당해야 합니다.
        [Tooltip("LeveledFireController 스크립트가 부착된 화재 프리팹을 할당하세요.")]
        [SerializeField] private GameObject leveledFirePrefab;

        [Header("화재 발생 영역 설정")]
        [SerializeField] private Vector3 spawnAreaCenter = new Vector3(500, 0, 500);
        [SerializeField] private Vector2 spawnAreaSize = new Vector2(5, 5);

        [Header("화재 발생 제어")]
        [SerializeField] private bool generateFireNow = false;

        // --- 내부 변수 ---
        private GameObject _activeFire; // 여러 개 대신 단일 화재 오브젝트

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        private void Start()
        {
            // 오브젝트 풀링 대신 단일 인스턴스를 미리 생성해 둡니다.
            if (leveledFirePrefab != null)
            {
                _activeFire = Instantiate(leveledFirePrefab, transform);
                _activeFire.SetActive(false); // 처음에는 비활성화
            }
            else
            {
                Debug.LogError("Leveled Fire Prefab이 할당되지 않았습니다!");
            }
        }

        private void Update()
        {
            if (generateFireNow && !isFireActive)
            {
                GenerateFire();
                generateFireNow = false;
            }
        }

        public void GenerateFire()
        {
            if (isFireActive)
            {
                Debug.LogWarning("화재가 이미 발생했습니다.");
                return;
            }
            if (!targetTerrain)
            {
                Debug.LogError("Terrain이 할당되지 않았습니다.");
                return;
            }
            if (_activeFire == null)
            {
                Debug.LogError("화재 오브젝트가 초기화되지 않았습니다.");
                return;
            }

            // 화재 발생 위치 계산
            Vector3 areaStartCorner = spawnAreaCenter - new Vector3(spawnAreaSize.x / 2, 0, spawnAreaSize.y / 2);
            float randomX = Random.Range(0, spawnAreaSize.x);
            float randomZ = Random.Range(0, spawnAreaSize.y);
            Vector3 spawnPos = areaStartCorner + new Vector3(randomX, 0, randomZ);

            float terrainHeight = targetTerrain.SampleHeight(spawnPos);
            Vector3 finalSpawnPosition = new Vector3(spawnPos.x, terrainHeight, spawnPos.z);

            _activeFire.transform.position = finalSpawnPosition;
            _activeFire.SetActive(true); // 화재를 활성화하여 OnEnable 함수가 호출되게 함
            Debug.Log($"<color=red>레벨 시스템 화재 발생!</color> 위치: {finalSpawnPosition}");
        }
    }
}
