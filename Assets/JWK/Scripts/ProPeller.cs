using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JWK.Scripts
{
    public class ProPeller : MonoBehaviour
    {
        [Header("�����緯 ����")]
        [Tooltip("�ð����(CW) ȸ�� �����緯 TransForm ����Ʈ")]
        public List<Transform> cwProPeller;
        [Tooltip("�ݽð����(CCW) ȸ�� �����緯 TransForm ����Ʈ")]
        public List<Transform> ccwProPeller;

        [Header("RPM ����")]
        [Tooltip("�����緯�� �ִ� RPM")]
        public float maxRPM = 2000.0f;
        [Tooltip("�ִ� RPM ���� �����ϴ� �� �ɸ��� �ð�")]
        public float acceleration = 2.0f;
        [Tooltip("�����ϴ� �� �ɸ��� �ð�")]
        public float decelerationTime = 1.0f;

        #region ���� ����
        private float currentRPM = 0.0f;
        private bool areSpining = false;
        

        #endregion

        void Update()
        {
        }
    }
}