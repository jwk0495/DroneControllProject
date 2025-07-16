using ActUtlType64Lib;
using System;
using System.Collections.Concurrent; // ������κ��� ������ ť ���
using System.Threading;
using UnityEngine;

public class ActUtlManager : MonoBehaviour
{
    private int logicalStationNumber = 1;

    private ActUtlType64 mxComponent;
    private Thread plcReadWriteThread;
    private volatile bool shutDownInitiated = false; // ������ ���� �÷���
    private volatile bool isConnected = false; // PLC ���� ����

    // Unity���� PLC�� ���� ��� ť (��: "SET_X0_1", "SET_X1_0")
    private ConcurrentQueue<string> sendCommandQueue = new ConcurrentQueue<string>();
    // PLC�κ��� ���� ������ ť (��: "Y0:1", "Y1:0", "Y0YF:1234")
    private ConcurrentQueue<string> receivedDataQueue = new ConcurrentQueue<string>();

    // �ܺο��� ���� ���³� ���� �����͸� �� �� �ֵ��� �̺�Ʈ ����
    public static event Action<bool> OnConnectionStatusChanged;
    public static event Action<string> OnPlcDataReceived; // PLC�κ��� ���� ������ ���� ��

    void Awake()
    {
        Application.quitting += OnApplicationQuitting; // �� ���� �� ������ ���� ����
    }

    void Start()
    {
        // PLC ��� ������ ����
        plcReadWriteThread = new Thread(PlcCommunicationLoop);
        plcReadWriteThread.IsBackground = true; // Unity �� ���� �� �����嵵 ����ǵ���
        plcReadWriteThread.Start();
    }

    void Update()
    {
        // ���� Unity �����忡�� ���ŵ� PLC ������ ó��
        while (receivedDataQueue.TryDequeue(out string data))
        {
            OnPlcDataReceived?.Invoke(data); // �����͸� �����ڿ��� ����
        }
    }

    /// <summary>
    /// PLC ����� ���� ��׶��� ������ ����
    /// </summary>
    private void PlcCommunicationLoop()
    {
        while (!shutDownInitiated)
        {
            try
            {
                // 1. PLC ���� �õ� (������ �������ų� �ʱ� ������ ��)
                if (!isConnected)
                {
                    mxComponent = new ActUtlType64();
                    mxComponent.ActLogicalStationNumber = logicalStationNumber;
                    int iRet = mxComponent.Open(); // ���� ȣ��, ������� ���⼭ ���ŷ��

                    if (iRet == 0)
                    {
                        isConnected = true;
                        Debug.Log("ActUtlManager: PLC ���� ����!");
                        OnConnectionStatusChanged?.Invoke(true);
                    }
                    else
                    {
                        isConnected = false;
                        Debug.LogError($"ActUtlManager: PLC ���� ����! ���� �ڵ�: {iRet}. �� �����̼� ��ȣ Ȯ�� ���.");
                        OnConnectionStatusChanged?.Invoke(false);
                        // ���� ���� �� ��� ��� �� ��õ�
                        Thread.Sleep(3000); // 3�� ���
                        continue; // ���� �������� �ٽ� ���� �õ�
                    }
                }

                // 2. PLC�κ��� Y ����̽� ���� �б�
                int blockCnt = 2; // Y0���� 1����(16��Ʈ) �б�
                int[] data = new int[blockCnt];
                int readRet = mxComponent.ReadDeviceBlock("Y0", blockCnt, out data[0]); // ���� ȣ��

                if (readRet == 0)
                {
                    // Y0���� YF������ ��Ʈ ���¸� ��Ÿ���� ���� ��
                    // �� ���� ���ڿ��� ��ȯ�Ͽ� ���� ������� ����
                    receivedDataQueue.Enqueue($"Y0YF:{data[0]}");
                    receivedDataQueue.Enqueue($"Y10Y1F:{data[1]}");
                }
                // 3. Unity���� ���� ��� ó�� (X ����̽� ���� ��)
                while (sendCommandQueue.TryDequeue(out string command))
                {
                    if (command.StartsWith("X"))
                    {
                        // "X0:1" �Ǵ� "X1:0" ������ ��� �Ľ�
                        string[] parts = command.Split(':');
                        if (parts.Length == 2 && short.TryParse(parts[1], out short value))
                        {
                            int writeRet = mxComponent.SetDevice(parts[0], value); // ���� ȣ��
                        }
                    }
                    // ���⿡ �ٸ� ������ ��� ó�� ������ �߰��� �� �ֽ��ϴ�.
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"ActUtlManager: PLC ��� ������ ����: {e.Message}");
                isConnected = false; // ���� �߻� �� ���� �������� �����Ͽ� �翬�� �õ�
                OnConnectionStatusChanged?.Invoke(false);
                // ���� �߻� �� ��� ��� �� ��õ�
                Thread.Sleep(3000); // 3�� ���
            }

            // �ʹ� ���� ������ ���� �ʵ��� ��� ��� (CPU ������ ����)
            Thread.Sleep(10); // 50ms ���� PLC�� ��� �õ� (���� ����)
        }

        // ������ ���� �� PLC ���� ����
        if (mxComponent != null && isConnected)
        {
            int closeRet = mxComponent.Close();
            if (closeRet == 0)
                Debug.Log("ActUtlManager: PLC ���� ���� ����.");
            else
                Debug.LogError($"ActUtlManager: PLC ���� ���� ����! ���� �ڵ�: {closeRet}");
        }
        Debug.Log("ActUtlManager: PLC ��� ������ ����.");
    }

    /// <summary>
    /// PLC�� ����� ���� �� ����ϴ� public �޼��� (�ٸ� ��ũ��Ʈ���� ȣ��)
    /// �� �޼���� ������κ��� �����մϴ�.
    /// </summary>
    /// <param name="command">������ ��� ���ڿ� (��: "X0:1")</param>
    public void SendCommandToPlc(string command)
    {
        if (isConnected)
        {
            sendCommandQueue.Enqueue(command); 
        }
    }

    /// <summary>
    /// ���ø����̼� ���� �� ȣ��Ǿ� �����带 �����ϰ� ����
    /// </summary>
    private void OnApplicationQuitting()
    {
        shutDownInitiated = true; // ������ ���� ���� ��ȣ
        if (plcReadWriteThread != null && plcReadWriteThread.IsAlive)
        {
            plcReadWriteThread.Join(1000); // �����尡 ����� ������ �ִ� 1�� ���
        }
        Debug.Log("ActUtlManager: OnApplicationQuitting ó�� �Ϸ�.");
    }

    void OnDestroy()
    {
        Application.quitting -= OnApplicationQuitting;
        if (!shutDownInitiated)
        {
            OnApplicationQuitting();
        }
    }
}