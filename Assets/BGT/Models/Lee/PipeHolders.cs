using UnityEngine;

public class PipeHolders : MonoBehaviour
{
    // ����Ƽ �����Ϳ��� �巡�� �� ������� ������ GameObject ������
    public GameObject PipeHolder1;
    public GameObject PipeHolder2;
    public GameObject PipeHolder3;
    public GameObject PipeHolder4;

    public Screw screwControl;

    private float MoveSpeed = 0.2f;
    private float MoveAmountY = 1.0f;

    // �� ������ Ȧ���� ���� ��ġ�� ��ǥ ��ġ ������
    private Vector3 PH1StartPosition;   // PipeHolder1
    private Vector3 PH1TargetPosition;  // PipeHolder1

    private Vector3 PH2StartPosition;   // PipeHolder2
    private Vector3 PH2TargetPosition;  // PipeHolder2

    private Vector3 PH3StartPosition;   // PipeHolder3
    private Vector3 PH3TargetPosition;  // PipeHolder3

    private Vector3 PH4StartPosition;   // PipeHolder4
    private Vector3 PH4TargetPosition;  // PipeHolder4

    private bool isPipeHoldersCW = false;
    private bool isPipeHoldersCCW = false;

    void Start()
    {
        //if (PipeHolder1 != null && PipeHolder1.GetComponent<Rigidbody>() != null)
        //{
        //    PipeHolder1.GetComponent<Rigidbody>().isKinematic = true;
        //}
        //if (PipeHolder2 != null && PipeHolder2.GetComponent<Rigidbody>() != null)
        //{
        //    PipeHolder2.GetComponent<Rigidbody>().isKinematic = true;
        //}
        //if (PipeHolder3 != null && PipeHolder3.GetComponent<Rigidbody>() != null)
        //{
        //    PipeHolder3.GetComponent<Rigidbody>().isKinematic = true;
        //}
        //if (PipeHolder4 != null && PipeHolder4.GetComponent<Rigidbody>() != null)
        //{
        //    PipeHolder4.GetComponent<Rigidbody>().isKinematic = true;
        //}
    }

    // Update is called once per frame
    void Update()
    {
        if (isPipeHoldersCW && !isPipeHoldersCCW)
        {
            // �� PipeHolder�� ��ġ�� ����
            if (PipeHolder1 != null)
            {
                PipeHolder1.transform.position = Vector3.MoveTowards(PipeHolder1.transform.position, PH1TargetPosition, MoveSpeed * Time.deltaTime);
            }
            if (PipeHolder2 != null)
            {
                PipeHolder2.transform.position = Vector3.MoveTowards(PipeHolder2.transform.position, PH2TargetPosition, MoveSpeed * Time.deltaTime);
            }
            if (PipeHolder3 != null)
            {
                PipeHolder3.transform.position = Vector3.MoveTowards(PipeHolder3.transform.position, PH3TargetPosition, MoveSpeed * Time.deltaTime);
            }
            if (PipeHolder4 != null)
            {
                PipeHolder4.transform.position = Vector3.MoveTowards(PipeHolder4.transform.position, PH4TargetPosition, MoveSpeed * Time.deltaTime);
            }
        }

        if (isPipeHoldersCCW && !isPipeHoldersCW)
        {
            // �� PipeHolder�� ��ġ�� ����
            if (PipeHolder1 != null)
            {
                PipeHolder1.transform.position = Vector3.MoveTowards(PipeHolder1.transform.position, PH1TargetPosition, MoveSpeed * Time.deltaTime);
            }
            if (PipeHolder2 != null)
            {
                PipeHolder2.transform.position = Vector3.MoveTowards(PipeHolder2.transform.position, PH2TargetPosition, MoveSpeed * Time.deltaTime);
            }
            if (PipeHolder3 != null)
            {
                PipeHolder3.transform.position = Vector3.MoveTowards(PipeHolder3.transform.position, PH3TargetPosition, MoveSpeed * Time.deltaTime);
            }
            if (PipeHolder4 != null)
            {
                PipeHolder4.transform.position = Vector3.MoveTowards(PipeHolder4.transform.position, PH4TargetPosition, MoveSpeed * Time.deltaTime);
            }
        }
    }

    public void ActivatePipeHoldersCW()
    {
        if (isPipeHoldersCW || isPipeHoldersCCW) return;
        isPipeHoldersCW = true;
        screwControl.ActivateScrewCW();
        // PipeHolder1 ����
        PH1StartPosition = PipeHolder1.transform.position;
        PH1TargetPosition = PH1StartPosition + new Vector3(0, 0, -MoveAmountY);
        // PipeHolder2 ����
        PH2StartPosition = PipeHolder2.transform.position;
        PH2TargetPosition = PH2StartPosition + new Vector3(0, 0, MoveAmountY);
        // PipeHolder3 ����
        PH3StartPosition = PipeHolder3.transform.position;
        PH3TargetPosition = PH3StartPosition + new Vector3(MoveAmountY, 0, 0);
        // PipeHolder4 ����
        PH4StartPosition = PipeHolder4.transform.position;
        PH4TargetPosition = PH4StartPosition + new Vector3(-MoveAmountY, 0, 0);
    }

    public void DeactivatePipeHoldersCW()
    {
        isPipeHoldersCW = false;
        screwControl.DeactivateScrewCW();
    }

    public void ActivatePipeHoldersCCW()
    {
        if (isPipeHoldersCW || isPipeHoldersCCW) return;
        isPipeHoldersCCW = true;
        screwControl.ActivateScrewCCW();
        // PipeHolder1 ����
        PH1StartPosition = PipeHolder1.transform.position;
        PH1TargetPosition = PH1StartPosition + new Vector3(0, 0, MoveAmountY);
        // PipeHolder2 ����
        PH2StartPosition = PipeHolder2.transform.position;
        PH2TargetPosition = PH2StartPosition + new Vector3(0, 0, -MoveAmountY);
        // PipeHolder3 ����
        PH3StartPosition = PipeHolder3.transform.position;
        PH3TargetPosition = PH3StartPosition + new Vector3(-MoveAmountY, 0, 0);
        // PipeHolder4 ����
        PH4StartPosition = PipeHolder4.transform.position;
        PH4TargetPosition = PH4StartPosition + new Vector3(MoveAmountY, 0, 0);
    }

    public void DeactivatePipeHoldersCCW()
    {
        isPipeHoldersCCW = false;
        screwControl.DeactivateScrewCCW();
    }
}