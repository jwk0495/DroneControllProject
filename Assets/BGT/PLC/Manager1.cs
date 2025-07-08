using UnityEngine;
// ActUtlType64Lib�� ���� Manager.cs���� ���� ������� �ʽ��ϴ�.

public class Manager1 : MonoBehaviour
{
    // ActUtlManager �ν��Ͻ��� ���� ���� (�ν����Ϳ��� �Ҵ�)
    public ActUtlManager actUtlManager;

    // Y ����̽� ���¸� ���� (PLC�κ��� �о��)
    private bool currentY0State;
    private bool currentY1State;
    private bool currentY2State;
    private bool currentY3State;
    private bool currentY4State;
    private bool currentY5State;
    private bool currentY6State;
    private bool currentY7State;

    // ������ Unity ������Ʈ ��ũ��Ʈ ���� (�ν����Ϳ��� �Ҵ�)
    public Chain1 chainInstance;
    public Chain1 chainInstance12;
    public PipeHolders pipeHolders;
    public ZLiftTigger zLift;
    public Base2Down base2down;
    // ManagerWrite ��ũ��Ʈ ���� (�ν����Ϳ��� �Ҵ�)
    public ManagerWrite1 managerWrite;

    void OnEnable()
    {
        // ActUtlManager���� ������ ���� �� ���� ���� ���� �̺�Ʈ�� ����
        if (actUtlManager != null)
        {
            ActUtlManager.OnPlcDataReceived += HandlePlcData;
            ActUtlManager.OnConnectionStatusChanged += HandleConnectionStatusChange;
        }
    }

    void OnDisable()
    {
        // ��ũ��Ʈ ��Ȱ��ȭ �� �̺�Ʈ ���� ���� (�޸� ���� ����)
        if (actUtlManager != null)
        {
            ActUtlManager.OnPlcDataReceived -= HandlePlcData;
            ActUtlManager.OnConnectionStatusChanged -= HandleConnectionStatusChange;
        }
    }

    void Update()
    {
        managerWrite.WriteDevice(); 
    }

    /// <summary>
    /// ActUtlManager�κ��� PLC �����͸� �����ϸ� ȣ��Ǵ� �ݹ� �Լ�
    /// �� �Լ��� Unity�� ���� �����忡�� ����˴ϴ�.
    /// </summary>
    /// <param name="receivedData">PLC�κ��� ���� ���� ���ڿ� ������ (��: "Y0YF:1234")</param>
    private void HandlePlcData(string receivedData)
    {
        // PLC ��� �������ݿ� ���� ���ŵ� ���ڿ��� �Ľ��ϰ� Unity ������Ʈ ���¸� ������Ʈ�մϴ�.
        // ActUtlManager�� Y0YF ���� ���� "Y0YF:��" ���·� �����ϴ�.
        if (receivedData.StartsWith("Y0YF:"))
        {
            string[] parts = receivedData.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[1], out int y0ToYFValue))
            {
                // Y0 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY0State, y0ToYFValue, 0, chainInstance != null ? chainInstance.ActivateChain : null, chainInstance != null ? chainInstance.DeactivateChain : null);
                // Y1 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY1State, y0ToYFValue, 1, chainInstance12 != null ? chainInstance12.ActivateChain : null, chainInstance12 != null ? chainInstance12.DeactivateChain : null);
                // Y2 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY2State, y0ToYFValue, 2, pipeHolders != null ? pipeHolders.ActivatePipeHoldersCW : null, pipeHolders != null ? pipeHolders.DeactivatePipeHoldersCW : null);
                // Y3 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY3State, y0ToYFValue, 3, pipeHolders != null ? pipeHolders.ActivatePipeHoldersCCW : null, pipeHolders != null ? pipeHolders.DeactivatePipeHoldersCCW : null);
                // Y4 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY4State, y0ToYFValue, 4, zLift != null ? zLift.ActivateZLiftUp : null, zLift != null ? zLift.DeactivateZLiftUp : null);
                // Y5 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY5State, y0ToYFValue, 5, zLift != null ? zLift.ActivateZLiftDown : null, zLift != null ? zLift.DeactivateZLiftDown : null);
                // Y6 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY6State, y0ToYFValue, 6, base2down != null ? base2down.ActiveDown : null, base2down != null ? base2down.DeactiveDown : null);
                // Y7 ��Ʈ ���� �� ���� ���� ����
                UpdateYStateBit(ref currentY7State, y0ToYFValue, 7, base2down != null ? base2down.ActiveUp : null, base2down != null ? base2down.DeactiveUp : null);
            }
        }
        // �ٸ� ������ PLC �����Ͱ� �ִٸ� ���⿡ �߰� �Ľ� ���� ����
    }

    /// <summary>
    /// PLC ���� ������ Ư�� ��Ʈ�� ���¸� �����ϰ�, ���� ���� �� Unity �׼��� ȣ���ϴ� ���� �޼���
    /// </summary>
    private void UpdateYStateBit(ref bool currentState, int wordValue, int bitIndex, System.Action activateAction, System.Action deactivateAction)
    {
        bool newBitState = ((wordValue >> bitIndex) & 1) == 1;
        if (newBitState != currentState)
        {
            currentState = newBitState;
            if (currentState)
            {
                activateAction?.Invoke();
                // Debug.Log($"Y{bitIndex} Ȱ��ȭ");
            }
            else
            {
                deactivateAction?.Invoke();
                // Debug.Log($"Y{bitIndex} ��Ȱ��ȭ");
            }
        }
    }

    /// <summary>
    /// PLC ���� ���� ��ȭ�� ó���ϴ� �ݹ� �Լ�
    /// </summary>
    private void HandleConnectionStatusChange(bool connected)
    {
        if (connected)
        {
            Debug.Log("Manager: PLC ������ Ȱ��ȭ�Ǿ����ϴ�.");
        }
        else
        {
            Debug.LogWarning("Manager: PLC ������ �������ų� �������� �ʾҽ��ϴ�. ��׶��忡�� �翬�� �õ� ��...");
        }
    }
}