﻿using System.Collections;
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

public abstract class Networker {

    protected int port;

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

    public void Terminate() {
        socket.Close();
        listenThread.Abort();
    }

    private void ListenerLoop() {
        EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0); //Create empty endpoint to store remote endpoint
        try {
            while (true) {
                byte[] bytes = new byte[10000];
                socket.ReceiveFrom(bytes, ref endPoint);
                PacketReceived(bytes, endPoint as IPEndPoint);

                Debug.Log("Received broadcast from:" + (endPoint as IPEndPoint) + "  |  Message: " + PackageSerializer.encoding.GetString(bytes));
            }
        } catch (SocketException e) {
            Console.WriteLine(e);
        } finally {
            socket.Close();
        }
    }

    protected void SendPackage(byte id, byte[] send, IPEndPoint endPoint) {
        Debug.Log("Sending package: " + PackageSerializer.encoding.GetString(send));
        byte[] withId = new byte[send.Length + 1];
        withId[0] = id;
        Array.Copy(send, 0, withId, 1, send.Length);
        socket.SendTo(withId, endPoint);
    }

    protected void SendPackage(byte id, byte[] send, IPAddress address) {
        byte[] withId = new byte[send.Length + 1];
        withId[0] = id;
        Array.Copy(send, 0, withId, 1, send.Length);

        Debug.Log("Sent package: " + PackageSerializer.encoding.GetString(withId) + " to ip " + address);
        IPEndPoint ep = new IPEndPoint(address, port);
        socket.SendTo(withId, ep);
    }
}
