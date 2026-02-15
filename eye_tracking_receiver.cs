using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System;

public class eye_tracking_receiver : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client;
    public int port = 5005;

    public float eyeX;
    public float eyeY;
    public float eyeDist;

    void Start()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void ReceiveData()
    {
        client = new UdpClient(port);

        while (true)
        {
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = client.Receive(ref anyIP);

            string text = Encoding.UTF8.GetString(data);
            EyeData parsed = JsonUtility.FromJson<EyeData>(text);

            eyeX = -parsed.x;
            eyeY = parsed.y;
            eyeDist = parsed.d;
            //Debug.Log("X: " + eyeX);
            //Debug.Log("Y: " + eyeY);
            //Debug.Log("Eye Distance: " + eyeDist);
        }
    }

    void OnApplicationQuit()
    {
        receiveThread.Abort();
        client.Close();
    }

    [System.Serializable]
    class EyeData
    {
        public float x;
        public float y;
        public float d;
    }
}
