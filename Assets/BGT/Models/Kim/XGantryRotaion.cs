using UnityEngine;

public class XGantryRotaion : MonoBehaviour
{
    private float rotationSpeed = 50f;

    public GameObject Pulley1;
    public GameObject Pulley2;
    public GameObject Shaft;
    
    private bool isZLiftRotationCW = false;
    private bool isZLiftRotationCCW = false;
    // Update is called once per frame
    void Update()
    {
        if(isZLiftRotationCW && !isZLiftRotationCCW)
        {
            // �� ���� ������Ʈ�� Y���� �߽����� 'rotationSpeed' ��ŭ ȸ����ŵ�ϴ�.
            // Time.deltaTime�� ���Ͽ� ������ �ӵ��� �������� ȸ���� �����մϴ�.
            // Space.Self�� ������Ʈ �ڽ��� ���� Y���� �������� ȸ����ŵ�ϴ�.
            // Space.World�� ���� ��ǥ���� Y���� �������� ȸ����ŵ�ϴ�.
            Pulley1.transform.Rotate(-rotationSpeed * Time.deltaTime, 0, 0, Space.Self);
            Pulley2.transform.Rotate(-rotationSpeed * Time.deltaTime, 0, 0, Space.Self);
            Shaft.transform.Rotate(-rotationSpeed * Time.deltaTime, 0, 0, Space.Self);
        }

        else if(!isZLiftRotationCW && isZLiftRotationCCW)
        {
            // �� ���� ������Ʈ�� Y���� �߽����� 'rotationSpeed' ��ŭ ȸ����ŵ�ϴ�.
            // Time.deltaTime�� ���Ͽ� ������ �ӵ��� �������� ȸ���� �����մϴ�.
            // Space.Self�� ������Ʈ �ڽ��� ���� Y���� �������� ȸ����ŵ�ϴ�.
            // Space.World�� ���� ��ǥ���� Y���� �������� ȸ����ŵ�ϴ�.
            Pulley1.transform.Rotate(rotationSpeed * Time.deltaTime, 0, 0, Space.Self);
            Pulley2.transform.Rotate(rotationSpeed * Time.deltaTime, 0, 0, Space.Self);
            Shaft.transform.Rotate(rotationSpeed * Time.deltaTime, 0, 0, Space.Self);
        }

        
    }

    public void ActivateZLiftRotationCW()
    {
        isZLiftRotationCW = true;
    }

    public void DeactivateZLiftRotationCW()
    {
        isZLiftRotationCW = false;
    }
    public void ActivateZLiftRotationCCW()
    {
        isZLiftRotationCCW = true;
    }

    public void DeactivateZLiftRotationCCW()
    {
        isZLiftRotationCCW = false;
    }
}
