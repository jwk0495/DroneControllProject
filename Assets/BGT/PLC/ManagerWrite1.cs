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
        //// Cube 센서 상태 확인 및 PLC로 전송 (X0)
        if (Cube != null)
        {
            Trigger cubeTrigger = Cube.GetComponent<Trigger>();
            bool currentCubeTriggerState = cubeTrigger.TriggerSensor;
            if (currentCubeTriggerState != prevCubeTriggerState) // 상태 변경 시에만 전송
            {
                string command = currentCubeTriggerState ? "X0:1" : "X0:0";
                actUtlManager.SendCommandToPlc(command);
                prevCubeTriggerState = currentCubeTriggerState;
            }
        }

        // Carriage 센서 상태 확인 및 PLC로 전송 (X1)
        if (Carriage != null)
        {
            Trigger carriageTrigger = Carriage.GetComponent<Trigger>();
            bool currentCarriageTriggerState = carriageTrigger.TriggerSensor;
            if (currentCarriageTriggerState != prevCarriageTriggerState) // 상태 변경 시에만 전송
            {
                string command = currentCarriageTriggerState ? "X1:1" : "X1:0";
                actUtlManager.SendCommandToPlc(command);
                prevCarriageTriggerState = currentCarriageTriggerState;
            }
        }
        // GussetPanel 센서 상태 확인 및 PLC로 전송 (X1)
        if (GussetPanel != null)
        {
            Trigger GussetPanelTrigger = GussetPanel.GetComponent<Trigger>();
            bool currentGussetPanelTriggerState = GussetPanelTrigger.TriggerSensor;
            if (currentGussetPanelTriggerState != prevGussetPanelTriggerState) // 상태 변경 시에만 전송
            {
                string command = currentGussetPanelTriggerState ? "X2:1" : "X2:0";
                actUtlManager.SendCommandToPlc(command);
                prevGussetPanelTriggerState = currentGussetPanelTriggerState;
            }
        }
    }
}