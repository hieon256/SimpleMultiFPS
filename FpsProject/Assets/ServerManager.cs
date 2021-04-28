using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class EventClass
{
    public string userName;
    public string eventName;
    public string eventData;
}
public class ServerManager : MonoBehaviour
{
    private static ServerManager instance;
    public static ServerManager Instance
    {
        get { return instance; }
    }

    public void Start()
    {
        instance = GetComponent<ServerManager>();
    }

    public string userName;

    string Host = "127.0.0.1";
    int Port = 9000;

    TcpClient client;
    NetworkStream nwStream;

    byte[] recvData = new byte[0];
    private Queue<string> dataQueue = new Queue<string>(); // 수신용 쓰레드를 메인쓰레드에서 읽기 위해 담는 큐.
    private Queue<string> writeQueue = new Queue<string>(); // 수신용 쓰레드를 메인쓰레드에서 읽기 위해 담는 큐.

    bool Connected = false;

    private AsyncCallback m_fnReceiveHandler;
    private void Update()
    {
        if (client != null && !client.Connected)
        {
            CloseConnection();
            return;
        }

        if (dataQueue.Count > 0)
        {
            int count = dataQueue.Count;
            string dataJson = "";
            for (int i = 0; i < count; i++)
            {
                dataJson += dataQueue.Dequeue();

                EventClass eventData;
                try
                {
                    eventData = JsonUtility.FromJson<EventClass>(dataJson);
                    dataJson = "";
                }
                catch { Debug.LogWarning(dataJson); continue; }

                if (eventData.eventName == "UserUpdate")
                {
                    BattleManager.Instance.EnemyUpdate(eventData.userName, eventData.eventData);
                }
                if (eventData.eventName == "UserShoot")
                {
                    BattleManager.Instance.EnemyShoot(eventData.userName, eventData.eventData);
                }
                if (eventData.eventName == "Disconnection")
                {
                    BattleManager.Instance.EnemyDelete(eventData.userName);
                }
            }
        }
        dataQueue = new Queue<string>();

        if (writeQueue.Count > 0)
        {
            int count = writeQueue.Count;
            string sendData = "";
            for (int i = 0; i < count; i++)
            {
                sendData += writeQueue.Dequeue();
            }

            string countStr = Encoding.Default.GetByteCount(sendData).ToString();
            byte[] byteCount = Encoding.UTF8.GetBytes(countStr);

            byte[] Header = new byte[4];

            Array.Copy(byteCount, 0, Header, 0, byteCount.Length);

            byte[] byteData = Encoding.UTF8.GetBytes(sendData);

            byte[] bytesToSend = new byte[Header.Length + byteData.Length];

            Array.Copy(Header, 0, bytesToSend, 0, Header.Length);
            Array.Copy(byteData, 0, bytesToSend, Header.Length, byteData.Length);
            try
            {
                nwStream.Write(bytesToSend, 0, bytesToSend.Length);
            }
            catch { }
        }
    }
    private void OnApplicationQuit()
    {
        CloseConnection();
    }
    public void ConnectButton()
    {
        userName = BattleManager.Instance.nameTxt.text;

        if (userName.Trim() == string.Empty)
        {
            return;
        }

        OpenConnection();
    }
    void OpenConnection()
    {
        if (client != null)
        {

        }
        else // client == null
        {
            try
            {
                client = new TcpClient();
                client.Connect(Host, Port);

                Debug.Log("Connection Complete");
                Connected = true;
                //BattleManager.Instance.ConnectGame();

                nwStream = client.GetStream();

                EventClass eventClass = new EventClass();

                eventClass.userName = userName;
                eventClass.eventName = "Connection";
                eventClass.eventData = "{}";
                // send.
                string eventJson = JsonUtility.ToJson(eventClass);

                writeQueue.Enqueue( eventJson + "Partition");

                // receive.
                m_fnReceiveHandler = new AsyncCallback(handleDataReceive);

                // 비동기 자료 수신 BeginReceive.
                byte[] bytesToRead = new byte[256];

                nwStream.BeginRead(bytesToRead, 0, bytesToRead.Length, m_fnReceiveHandler, bytesToRead);

                BattleManager.Instance.ConnectGame();
            }
            catch (Exception ex)
            {
                client = null;
                Debug.Log("" + ex.Message);
                Connected = false;
            }
        }
    }
    public void CloseConnection()
    {
        if (client == null)
        {
            Debug.Log("already closed or be not open");
            return;
        }
        try
        {
            client.Close();
        }
        catch (Exception ex)
        {
            client = null;
            Debug.Log("" + ex.Message);
        }
        finally
        {
            client = null;
            BattleManager.Instance.DisconnectGame();
        }
        Debug.Log("Close success");

    }
    private void handleDataReceive(IAsyncResult ar)
    {
        byte[] buffer = (byte[])ar.AsyncState;

        int recvBytes = nwStream.EndRead(ar);

        int prevLength = recvData.Length;
        Array.Resize(ref recvData, recvData.Length + recvBytes);
        Array.Copy(buffer, 0, recvData, prevLength, recvBytes);

        if (recvData.Length > 4)
        {
            string byteCount = Encoding.UTF8.GetString(recvData, 0, 4);

            int bC = int.Parse(byteCount);

            if (recvData.Length >= 4 + bC)
            {
                string Data = Encoding.UTF8.GetString(recvData, 4, bC);

                DataToMainThread(Data);

                Array.Clear(recvData, 0, 4 + bC);
                Array.Copy(recvData, 4 + bC, recvData, 0, recvData.Length - (4 + bC));
                Array.Resize(ref recvData, recvData.Length - (4 + bC));
            }
        }

        nwStream.BeginRead(buffer, 0, buffer.Length, m_fnReceiveHandler, buffer);
    }
    private void DataToMainThread(string completeData)
    {
        string[] handleData = completeData.Split(new string[] { "Partition" }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < handleData.Length; i++)
        {
            if (handleData[i] == string.Empty)
                continue;

            dataQueue.Enqueue(handleData[i]); // 비동기 쓰레드에서 받은 데이타 큐에 담기.
        }
    }
    public void PlayerUpdate(CharacterClass cc)
    {
        if (client == null || !client.Connected)
            return;

        string ccJson = JsonUtility.ToJson(cc);

        EventClass ec = new EventClass();
        ec.userName = userName;
        ec.eventName = "UserUpdate";
        ec.eventData = ccJson;

        string eventJson = JsonUtility.ToJson(ec);

        writeQueue.Enqueue(eventJson + "Partition");
    }
    public void PlayerShoot(ShootingClass sc)
    {
        if (client == null || !client.Connected)
            return;

        string scJson = JsonUtility.ToJson(sc);

        EventClass ec = new EventClass();
        ec.userName = userName;
        ec.eventName = "UserShoot";
        ec.eventData = scJson;

        string eventJson = JsonUtility.ToJson(ec);

        writeQueue.Enqueue(eventJson + "Partition");
    }
}
