using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public abstract class Networker {

    protected int port;
    protected System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

    protected Socket socket;

    public Networker(int port) {
        this.port = port;
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any, NetConstants.PORT));
    }

    public abstract void PacketReceived(byte[] bytes, IPEndPoint ipEndPoint);

    private Thread listenThread;

    public void StartListener() {
        listenThread = new Thread(ListenerLoop);
        listenThread.Start();
    }

    private void ListenerLoop() {
        EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0); //Create empty endpoint to store remote endpoint
        try {
            while (true) {
                Debug.Log("Waiting for broadcast on port " + port);
                //byte[] bytes = listener.Receive(ref groupEP);
                byte[] bytes = new byte[10000];
                socket.ReceiveFrom(bytes, ref endPoint);
                PacketReceived(bytes, endPoint as IPEndPoint);

                Debug.Log("Received broadcast from:" + (endPoint as IPEndPoint) + "  |  Message: " + encoding.GetString(bytes));
            }
        } catch (SocketException e) {
            Console.WriteLine(e);
        } finally {
            socket.Close();
        }
    }

    protected void SendPackage(byte id, byte[] send, IPEndPoint endPoint) {
        Debug.Log("Sending package: " + encoding.GetString(send));
        byte[] withId = new byte[send.Length + 1];
        withId[0] = id;
        Array.Copy(send, 0, withId, 1, send.Length);
        socket.SendTo(withId, endPoint);
    }

    protected void SendPackage(byte id, byte[] send, IPAddress address) {
        byte[] withId = new byte[send.Length + 1];
        withId[0] = id;
        Array.Copy(send, 0, withId, 1, send.Length);

        Debug.Log("Sent package: " + encoding.GetString(withId) + " to ip " + address);
        IPEndPoint ep = new IPEndPoint(address, port);
        socket.SendTo(withId, ep);
    }

    public T GetObject<T>(byte[] bytes) {
        T t = JsonUtility.FromJson<T>(encoding.GetString(bytes));
        return t;
    }

    public byte[] GetBytes(object obj) {
        string json = JsonUtility.ToJson(obj);
        return encoding.GetBytes(json);
    }
}
