using UnityEngine;

public class ForkMove1 : MonoBehaviour
{
    // ����Ƽ �����Ϳ��� �巡�� �� ������� ������ GameObject ������
    
    
    private float MoveSpeed = 0.2f;
    private float MoveAmountY = 0.272f;

    // �� ������ Ȧ���� ���� ��ġ�� ��ǥ ��ġ ������
    private Vector3 StartPosition;   // PipeHolder1
    private Vector3 TargetPosition;  // PipeHolder1

   
    private bool isForkMoveRigt = false;
    private bool isForkMoveLeft = false;

    
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (isForkMoveRigt && !isForkMoveLeft)
        {
            
             transform.localPosition = Vector3.MoveTowards(transform.localPosition, TargetPosition, MoveSpeed * Time.deltaTime);
           
        }

        if (isForkMoveLeft && !isForkMoveRigt)
        {
            // �� PipeHolder�� ��ġ�� ����
           
             transform.localPosition = Vector3.MoveTowards(transform.localPosition, TargetPosition, MoveSpeed * Time.deltaTime);
           
        }
    }

    public void ActivateRight()
    {
        if (isForkMoveRigt || isForkMoveLeft) return;
        isForkMoveRigt = true;
        
        StartPosition = transform.localPosition;
        TargetPosition = StartPosition + new Vector3(0, MoveAmountY, 0);
        
    }

    public void DeactivateRight()
    {
        isForkMoveRigt = false;
       
    }

    public void ActivateLeft()
    {
        if (isForkMoveRigt || isForkMoveLeft) return;
        isForkMoveLeft = true;
        
        StartPosition = transform.localPosition;
        TargetPosition = StartPosition + new Vector3(0, -MoveAmountY, 0);
       
    }

    public void DeactivateLeft()
    {
        isForkMoveLeft = false;
        
    }
}