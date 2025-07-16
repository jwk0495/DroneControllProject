using UnityEngine;

public class PinMove8 : MonoBehaviour
{
    public GameObject sourceObject; // Z�� �̵��� ������ �� ������Ʈ

    private Vector3 previousSourcePosition; // ���� �������� sourceObject ��ġ (��ü Vector3 ����)

    private bool isForward = false;
    void Start()
    {
        previousSourcePosition = sourceObject.transform.position;
    }

    void Update()
    {
        if (sourceObject == null)
        {
            return;
        }
        if(isForward)
        {
            // sourceObject�� ���� Z�� ��ġ�� ������
            float currentSourceZ = sourceObject.transform.position.x;

      
            float deltay = currentSourceZ - previousSourcePosition.x;


            Vector3 currentMyPosition = transform.position;

            // �� ������Ʈ�� X�� ��ġ�� sourceObject�� Z�� �̵���(deltaZ)��ŭ ����
            currentMyPosition.x += deltay;
            transform.position = currentMyPosition;

            // ���� �������� ���� sourceObject�� ���� ��ü ��ġ�� ���� ��ġ�� ������Ʈ
            previousSourcePosition = sourceObject.transform.position;
        }
    }
    public void ActiveForward()
    {
        isForward = true;
    }
    public void DeactiveForward()
    {
        isForward = false;
    }
}