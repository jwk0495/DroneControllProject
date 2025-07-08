using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

namespace JWK.Scripts
{
    public class ExtinguisherDropSystem : MonoBehaviour
    {
        [Header("ȸ�� ��� ������Ʈ �Ҵ�")] [Tooltip("���� ���͸� �Ҵ����ּ���.")]
        public Transform rotaryIn;
        [Tooltip("�ٱ��� ���͸� �Ҵ����ּ���.")]
        public Transform rotaryOut;
        [Tooltip("ȸ�� �ִϸ��̼��� �ӵ��Դϴ�.")]
        public float rotationSpeed = 90.0f; // �ʴ� 90�� ȸ��
        
        private DroneController _droneController;
        private bool isActionInProgress = false;

        public float innerRotateAngle = 30.0f;
        public float outerRotateAngle = 30.0f;

        void Start()
        {
            _droneController = GetComponent<DroneController>();

            if (!_droneController)
            {
                Debug.LogError("�θ� ������Ʈ���� DroneController�� ã�� �� �����ϴ�.");
                StartCoroutine(DropSequenceCouroutine());
            }

            else
            {
                if (!_droneController)
                    Debug.LogWarning("DroneController�� ������� �ʾҽ��ϴ�.");

                if (_droneController && !_droneController.isArrived)
                    Debug.LogWarning("����� ���� ȭ�� ����Ʈ�� �������� �ʾҽ��ϴ�.");

                if (isActionInProgress)
                    Debug.LogWarning("�̹� �ٸ� ���� �������� ���� ���Դϴ�.");
            }
        }

        /// <summary>
        /// DroneController���� ȣ���� �Լ�
        /// ����� ȭ�� ����Ʈ�� �����ϸ�, ȸ�� �� ���� �ڷ�ƾ�� ������.
        /// </summary>
        public void PlayDropExtinguishBomb()
        {
            if (!_droneController && _droneController.isArrived && !isActionInProgress)
                _droneController.isArrived = false;
        }

        /// <summary>
        /// ���� ȸ�� �� ��ź ���ϸ� ���������� �����ϴ� �ڷ�ƾ
        /// </summary>
        private IEnumerator DropSequenceCouroutine()
        {
            isActionInProgress = true;
            Debug.Log("���͸� ȸ����ŵ�ϴ�....");
            
            Quaternion initialRotIn = rotaryIn.localRotation;
            Quaternion initialRotOut = rotaryOut.localRotation;
            
            // ��ǥ ȸ���� ��� (���� �ݴ� �������� ȸ��)
            Quaternion targetRotIn = initialRotIn * Quaternion.Euler(0, innerRotateAngle, 0); // Y�� ����
            Quaternion targetRotOut = initialRotOut * Quaternion.Euler(0, -outerRotateAngle, 0); // Y�� ����, �ݴ� ����

            float elapsedTime = 0f;
            // �� ū ������ �������� ȸ�� �ð��� ����Ͽ� ���ÿ� �������� ��
            float rotationDuration = Mathf.Max(Mathf.Abs(innerRotateAngle), Mathf.Abs(outerRotateAngle)) / rotationSpeed;

            while (elapsedTime < rotationDuration)
            {
                // �ð��� ���� �ε巴�� ȸ�� (Slerp ���)
                rotaryIn.localRotation = Quaternion.Slerp(initialRotIn, targetRotIn, elapsedTime / rotationDuration);
                rotaryOut.localRotation = Quaternion.Slerp(initialRotOut, targetRotOut, elapsedTime / rotationDuration);

                elapsedTime += Time.deltaTime;
                yield return null; // ���� �����ӱ��� ���
            }

            // ���� ������ ���� ���� ȸ������ ��Ȯ�ϰ� ����
            rotaryIn.localRotation = targetRotIn;
            rotaryOut.localRotation = targetRotOut;
            Debug.Log("���� ȸ�� �Ϸ�.");
            
            // --- 2. (���⿡ ��ź ���� ���� �߰�) ---
            // ��: yield return new WaitForSeconds(0.5f); // ȸ�� �� ��� ���
            //     _droneController.InstantiateBomb(); // DroneController�� ��ź ���� �Լ��� ����� ȣ��

            // --- 3. (������) ���� ����ġ ���� ---
            yield return new WaitForSeconds(1.0f); // ���� �� ��� ���
            Debug.Log("���͸� ����ġ�� ���ͽ�ŵ�ϴ�.");
            
            elapsedTime = 0f; // �ð� �ʱ�ȭ
            while (elapsedTime < rotationDuration)
            {
                rotaryIn.localRotation = Quaternion.Slerp(targetRotIn, initialRotIn, elapsedTime / rotationDuration);
                rotaryOut.localRotation = Quaternion.Slerp(targetRotOut, initialRotOut, elapsedTime / rotationDuration);

                elapsedTime += Time.deltaTime;
                yield return null;
            }
            rotaryIn.localRotation = initialRotIn;
            rotaryOut.localRotation = initialRotOut;

            isActionInProgress = false; // �׼� �Ϸ�
            Debug.Log("��ȭź ���� ������ ��ü �Ϸ�.");
        }
    }
}