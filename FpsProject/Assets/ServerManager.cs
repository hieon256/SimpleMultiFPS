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
            for(int i = 0; i < count; i++)
            {
                sendData += writeQueue.Dequeue();
            }

            byte[] bytesToSend = Encoding.UTF8.GetBytes(sendData);
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

                writeQueue.Enqueue("Partition" + eventJson);

                // receive.
                m_fnReceiveHandler = new AsyncCallback(handleDataReceive);

                // 비동기 자료 수신 BeginReceive.
                byte[] bytesToRead = new byte[1024];

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

        if (recvBytes > 0)
        {
            string[] str = Encoding.UTF8.GetString(buffer, 0, recvBytes).Split(new string[] { "Partition" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < str.Length; i++)
            {
                dataQueue.Enqueue(str[i]); // 비동기 쓰레드에서 받은 데이타 큐에 담기.
            }
        }

        nwStream.BeginRead(buffer, 0, buffer.Length, m_fnReceiveHandler, buffer);
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

        writeQueue.Enqueue("Partition" + eventJson);
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

        writeQueue.Enqueue("Partition" + eventJson);
    }
}
