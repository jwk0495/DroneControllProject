using UnityEngine;

public class ManagerWrite1 : MonoBehaviour
{
    public ActUtlManager actUtlManager;
    public GameObject Cube;
    public GameObject Carriage;
    public GameObject GussetPanel;

    private bool prevCubeTriggerState = false;
    private bool prevCarriageTriggerState = false;
    private bool prevGussetPanelTriggerState = false;

 
    public void WriteDevice()
    {
        //// Cube ���� ���� Ȯ�� �� PLC�� ���� (X0)
        if (Cube != null)
        {
            Trigger cubeTrigger = Cube.GetComponent<Trigger>();
            bool currentCubeTriggerState = cubeTrigger.TriggerSensor;
            if (currentCubeTriggerState != prevCubeTriggerState) // ���� ���� �ÿ��� ����
            {
                string command = currentCubeTriggerState ? "X0:1" : "X0:0";
                actUtlManager.SendCommandToPlc(command);
                prevCubeTriggerState = currentCubeTriggerState;
            }
        }

        // Carriage ���� ���� Ȯ�� �� PLC�� ���� (X1)
        if (Carriage != null)
        {
            Trigger carriageTrigger = Carriage.GetComponent<Trigger>();
            bool currentCarriageTriggerState = carriageTrigger.TriggerSensor;
            if (currentCarriageTriggerState != prevCarriageTriggerState) // ���� ���� �ÿ��� ����
            {
                string command = currentCarriageTriggerState ? "X1:1" : "X1:0";
                actUtlManager.SendCommandToPlc(command);
                prevCarriageTriggerState = currentCarriageTriggerState;
            }
        }
        // GussetPanel ���� ���� Ȯ�� �� PLC�� ���� (X1)
        if (GussetPanel != null)
        {
            Trigger GussetPanelTrigger = GussetPanel.GetComponent<Trigger>();
            bool currentGussetPanelTriggerState = GussetPanelTrigger.TriggerSensor;
            if (currentGussetPanelTriggerState != prevGussetPanelTriggerState) // ���� ���� �ÿ��� ����
            {
                string command = currentGussetPanelTriggerState ? "X2:1" : "X2:0";
                actUtlManager.SendCommandToPlc(command);
                prevGussetPanelTriggerState = currentGussetPanelTriggerState;
            }
        }
    }
}