using UnityEngine;

public class XGantry : MonoBehaviour 
{
    public GameObject XGantryMoving;

    public XGantryRotaion RotaionObject;
    public Chain1 chainIntance;
    // �̵� �ӵ� (�ʴ� �̵� �Ÿ�)
    private float moveSpeed = 0.2f;
    private float moveDistanceZ = 5.0f;

    // �� Lift ���� ��ġ�� ��ǥ ��ġ ������
    private Vector3 LWStartPosition;    // XGantryMoving
    private Vector3 LWTargetPosition;  // XGantryMoving

    private bool isXGantryMovingRight = false;
    private bool isXGantryMovingLeft = false;

    void Start()
    {
        // Debug.Log(this.gameObject.name + " XGantryMoving ��ũ��Ʈ ����.");
        // XGantryMoving�� CarriageFrame�� Rigidbody�� �ִٸ� Is Kinematic�� üũ�ϴ� ���� �����ϴ�.
        // ���� Transform.position�� ������ �� ���� ������ ������ ���� �� �ֽ��ϴ�.
        if (XGantryMoving != null && XGantryMoving.GetComponent<Rigidbody>() != null)
        {
            XGantryMoving.GetComponent<Rigidbody>().isKinematic = true;
        }
      
    }

    // Update�� �� ������ ����� �̵� ������ ���
    void Update()
    {
        if (isXGantryMovingRight && !isXGantryMovingLeft)
        {
            if (XGantryMoving != null)
            {
                XGantryMoving.transform.position = Vector3.MoveTowards(XGantryMoving.transform.position, LWTargetPosition, moveSpeed * Time.deltaTime);
            }
            
        }
        // CCW �̵� ���� (XGantryMoving�� ����, CarriageFrame�� �Ʒ���)
        else if (isXGantryMovingLeft && !isXGantryMovingRight)
        {
            // XGantryMoving �̵� (MoveTowards ���)
            if (XGantryMoving != null)
            {
                XGantryMoving.transform.position = Vector3.MoveTowards(XGantryMoving.transform.position, LWTargetPosition, moveSpeed * Time.deltaTime);
            }
           
        }
    }

    // CW �̵��� �ʱ�ȭ�ϰ� �����մϴ�. (ActivateXGantryMovingUp���� ����� �� �ֽ��ϴ�)
    public void ActivateXGantryMovingRight() 
    {
        // �̹� �ٸ� �������� �����̰ų� ���� �������� �����̰� �ִٸ� ��������� ����
        if (isXGantryMovingRight || isXGantryMovingLeft) return;
        isXGantryMovingRight = true;
        LWStartPosition = XGantryMoving.transform.position;
        LWTargetPosition = LWStartPosition + new Vector3(0,0, -moveDistanceZ);
        RotaionObject.ActivateZLiftRotationCW();
        chainIntance.ActiveChainCW();
    }

    // CW �̵��� ������ �����մϴ�.
    public void DeactivateXGantryMovingRight()
    {
        isXGantryMovingRight = false;
        RotaionObject.DeactivateZLiftRotationCW();
        chainIntance.DeActiveChainCW();
    }

    public void ActivateXGantryMovingLeft() 
    {
        // �̹� �ٸ� �������� �����̰ų� ���� �������� �����̰� �ִٸ� ��������� ����
        if (isXGantryMovingRight || isXGantryMovingLeft) return;
        isXGantryMovingLeft = true;
        LWStartPosition = XGantryMoving.transform.position;
        LWTargetPosition = LWStartPosition + new Vector3(0, 0, moveDistanceZ);
        RotaionObject.ActivateZLiftRotationCCW();
        chainIntance.ActiveChainCCW();
       
    }

    // CCW �̵��� ������ �����մϴ�.
    public void DeactivateXGantryMovingLeft()
    {
        // CCW �̵� ���� ���� ����
        isXGantryMovingLeft = false;
        RotaionObject.DeactivateZLiftRotationCCW() ;
        chainIntance.DeActiveChainCCW() ;
    }
}