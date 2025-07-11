using UnityEngine;

public class ZLiftTigger : MonoBehaviour 
{
    public GameObject LiftWeight;
    public GameObject CarriageFrame;

    public ZLiftRotation ROT;
    public ChainMove CHM;
    public ChainMove CHM1;

    // �̵� �ӵ� (�ʴ� �̵� �Ÿ�)
    private float moveSpeed = 0.2f;
    private float moveDistanceY = 5.0f;

    // �� Lift ���� ��ġ�� ��ǥ ��ġ ������
    private Vector3 LWStartPosition;    // LiftWeight
    private Vector3 LWTargetPosition;  // LiftWeight

    private Vector3 CFStartPosition;    // CarriageFrame
    private Vector3 CFTargetPosition;  // CarriageFrame

    private bool isZLiftCW = false;
    private bool isZLiftCCW = false;

    void Start()
    {
        // Debug.Log(this.gameObject.name + " ZLift ��ũ��Ʈ ����.");
        // LiftWeight�� CarriageFrame�� Rigidbody�� �ִٸ� Is Kinematic�� üũ�ϴ� ���� �����ϴ�.
        // ���� Transform.position�� ������ �� ���� ������ ������ ���� �� �ֽ��ϴ�.
        if (LiftWeight != null && LiftWeight.GetComponent<Rigidbody>() != null)
        {
            LiftWeight.GetComponent<Rigidbody>().isKinematic = true;
        }
        if (CarriageFrame != null && CarriageFrame.GetComponent<Rigidbody>() != null)
        {
            CarriageFrame.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    // Update�� �� ������ ����� �̵� ������ ���
    void Update()
    {
        if (isZLiftCW && !isZLiftCCW)
        {
            if (LiftWeight != null)
            {
                LiftWeight.transform.localPosition = Vector3.MoveTowards(LiftWeight.transform.localPosition, LWTargetPosition, moveSpeed * Time.deltaTime);
            }
            if (CarriageFrame != null)
            {
                CarriageFrame.transform.localPosition = Vector3.MoveTowards(CarriageFrame.transform.localPosition, CFTargetPosition, moveSpeed * Time.deltaTime);
            }
        }
        // CCW �̵� ���� (LiftWeight�� ����, CarriageFrame�� �Ʒ���)
        else if (isZLiftCCW && !isZLiftCW)
        {
            // LiftWeight �̵� (MoveTowards ���)
            if (LiftWeight != null)
            {
                LiftWeight.transform.localPosition = Vector3.MoveTowards(LiftWeight.transform.localPosition, LWTargetPosition, moveSpeed * Time.deltaTime);
            }
            // CarriageFrame �̵� (MoveTowards ���)
            if (CarriageFrame != null)
            {
                CarriageFrame.transform.localPosition = Vector3.MoveTowards(CarriageFrame.transform.localPosition, CFTargetPosition, moveSpeed * Time.deltaTime);
            }
        }
    }

    // CW �̵��� �ʱ�ȭ�ϰ� �����մϴ�. (ActivateZLiftUp���� ����� �� �ֽ��ϴ�)
    public void ActivateZLiftUp() 
    {
        // �̹� �ٸ� �������� �����̰ų� ���� �������� �����̰� �ִٸ� ��������� ����
        if (isZLiftCW || isZLiftCCW) return;

        isZLiftCW = true;

        LWStartPosition = LiftWeight.transform.localPosition;
        LWTargetPosition = LWStartPosition + new Vector3(0, 0,-moveDistanceY);

        CFStartPosition = CarriageFrame.transform.localPosition;
        CFTargetPosition = CFStartPosition + new Vector3(0, 0, moveDistanceY);

        ROT.ActivateZLiftRotationCW();
        CHM.ActiveChainCW();
        CHM1.ActiveChainCW();
        Debug.Log("ZLiftUp Ȱ��ȭ. CW �̵� ����.");
    }

    // CW �̵��� ������ �����մϴ�.
    public void DeactivateZLiftUp()
    {
        // CW �̵� ���� ���� ����
        isZLiftCW = false;
        ROT.DeactivateZLiftRotationCW();
        CHM.DeActiveChainCW();
        CHM1.DeActiveChainCW();
    }

    public void ActivateZLiftDown() 
    {
        // �̹� �ٸ� �������� �����̰ų� ���� �������� �����̰� �ִٸ� ��������� ����
        if (isZLiftCW || isZLiftCCW) return;

        isZLiftCCW = true;
        LWStartPosition = LiftWeight.transform.localPosition;
        LWTargetPosition = LWStartPosition + new Vector3(0, 0, moveDistanceY);
        CFStartPosition = CarriageFrame.transform.localPosition;
        CFTargetPosition = CFStartPosition + new Vector3(0, 0, -moveDistanceY);

        ROT.ActivateZLiftRotationCCW();
        CHM.ActiveChainCCW();
        CHM1.ActiveChainCCW();
        Debug.Log("ZLiftDown Ȱ��ȭ. CCW �̵� ����.");
    }

    // CCW �̵��� ������ �����մϴ�.
    public void DeactivateZLiftDown()
    {
        // CCW �̵� ���� ���� ����
        isZLiftCCW = false;
        ROT.DeactivateZLiftRotationCCW();
        CHM.DeActiveChainCCW();
        CHM1.DeActiveChainCCW();
    }
}