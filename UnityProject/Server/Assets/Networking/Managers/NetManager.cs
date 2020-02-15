using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class NetManager : MonoBehaviour {
    public const uint TICKRATE = 25; //ALL tick time units in milliseconds

    Side side;
    protected Dictionary<uint, NetIdentity> netIdentities = new Dictionary<uint, NetIdentity>();
    long tickTimeCounter = 0;
    private uint nextID = 1;
    public uint tick { get; private set; } = 1;

    protected long lastTickTimestamp { get; private set; } = 0;
    private long lastUpdateTimestamp = 0;

    //private Stopwatch stopwatch;
    //Timer timer;

    public NetManager(Side side) {
        this.side = side;
    }

    protected void Start() {
        lastTickTimestamp = GetTimestamp();
        lastUpdateTimestamp = GetTimestamp();
    }

    public Side GetSide() {
        return side;
    }

    protected void Update() {
        long delta = GetTimestamp() - lastUpdateTimestamp;
        tickTimeCounter += delta;
        while (tickTimeCounter > TICKRATE) {
            GameObject.FindGameObjectWithTag("Debug1").GetComponent<Text>().text = ("Tick #" + tick + " as " + side + " at " + GetTimestamp());
            Tick(tick++);
            tickTimeCounter -= TICKRATE;
            lastTickTimestamp += TICKRATE;
        }
        lastUpdateTimestamp = GetTimestamp();
    }

    public abstract void Tick(uint tick);

    /// <summary> Returns networked id</summary>
    public void RegisterObject(NetIdentity netIdentity, uint id) {
        if (netIdentities.ContainsKey(id)) {
            UnityEngine.Debug.LogError("NetIdentity with already existing id tried to register!");
        }
        netIdentities.Add(id, netIdentity);
    }

    public void RemoveComponent(uint id) {
        netIdentities.Remove(id);
    }

    public uint GetFreeID() {
        while (netIdentities.ContainsKey(nextID)) {
            nextID++;
        }
        return nextID++;
    }

    public enum Side {
        CLIENT,
        SERVER
    }

    protected long GetTimestamp() {
        return DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    protected void SyncTick(uint lastTick, long lastTickTimestamp) {
        tickTimeCounter = 0;
        tick = lastTick;
        this.lastTickTimestamp = lastTickTimestamp;
        this.lastUpdateTimestamp = lastTickTimestamp;
    }
}
