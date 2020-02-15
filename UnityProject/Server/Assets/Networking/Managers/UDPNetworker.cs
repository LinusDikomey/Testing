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
using Package;

public class Networker {

    protected int remotePort;
    protected Socket socket;
    private Action<byte[], IPEndPoint> packetReceived;

    public Networker(int port, int remotePort, Action<byte[], IPEndPoint> packetReceived) {
        this.remotePort = remotePort;
        this.packetReceived = packetReceived;
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any, port));
    }

    private Thread listenThread;

    public void StartListener() {
        listenThread = new Thread(ListenerLoop);
        listenThread.Start();
    }

    public void Terminate() {
        socket.Close();
        if(listenThread.IsAlive)
            listenThread.Abort();
    }

    private void ListenerLoop() {
        EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0); //Create empty endpoint to store remote endpoint
        try {
            while (true) {
                byte[] bytes = new byte[10000];
                socket.ReceiveFrom(bytes, ref endPoint);
                packetReceived(bytes, endPoint as IPEndPoint);
                Debug.Log("Received broadcast from:" + (endPoint as IPEndPoint) + "  |  Message: " + PackageSerializer.encoding.GetString(bytes));
            }
        } catch (SocketException e) {
            Debug.LogError(e);
        } finally {
            socket.Close();
        }
    }

    public void SendPacket(byte id, byte[] send, IPEndPoint endPoint) {
        //Debug.Log("Sending package: " + PackageSerializer.encoding.GetString(send));
        byte[] withId = new byte[send.Length + 1];
        withId[0] = id;
        Array.Copy(send, 0, withId, 1, send.Length);
        socket.SendTo(withId, endPoint);
    }

    public void SendPacket(byte id, byte[] send, IPAddress address) {
        byte[] withId = new byte[send.Length + 1];
        withId[0] = id;
        Array.Copy(send, 0, withId, 1, send.Length);

        Debug.Log("Sent package: " + PackageSerializer.encoding.GetString(withId) + " to ip " + address);
        IPEndPoint ep = new IPEndPoint(address, remotePort);
        socket.SendTo(withId, ep);
    }
}
