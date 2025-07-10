using UnityEngine;

namespace JWK.Scripts
{
    public class Bomb_Particle : MonoBehaviour
    {
        [Header("��ź �浹 VFX")] [Tooltip("��ź�� ȭ�� ����Ʈ�� �浹 �� ����� VFX Prefab�� �Ҵ��ϼ���")]
        public GameObject impactVFXPrefab;

        [Tooltip("VFX�� �� �� �ڿ� �ı�����")] public float vfxDestroyDelay = 5.0f;
        
        [Tooltip("�������� �ν��� LayerMask")]
        public LayerMask groundlayerMask;

        private void OnCollisionEnter(Collision collision)
        {
            if ( ((1 << collision.gameObject.layer) & groundlayerMask) != 0)
            {
                Destroy(gameObject);
                
                if(impactVFXPrefab)
                {
                    GameObject vfx = Instantiate(impactVFXPrefab, collision.contacts[0].point, Quaternion.identity);
                    // Destroy(vfx, vfxDestroyDelay);
                }
            }
            
        }
    }
}