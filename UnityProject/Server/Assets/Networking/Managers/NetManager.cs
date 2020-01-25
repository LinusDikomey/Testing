using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NetManager : MonoBehaviour {

    public const float tickRate = 1f;

    Side side;
    protected Dictionary<uint, NetBehaviour> netComponents = new Dictionary<uint, NetBehaviour>();
    float lastUpdateTime;
    float tickTimeCounter = 0f;
    protected uint nextID = 0;
    private uint tick = 0;

    public NetManager(Side side) {
        this.side = side;
    }

    protected void Start() {
        lastUpdateTime = Time.realtimeSinceStartup;
    }

    public Side GetSide() {
        return side;
    }

    protected void Update() {
        //25 ticks: 0.02f
        float delta = Time.realtimeSinceStartup - lastUpdateTime;
        tickTimeCounter += delta;
        while(tickTimeCounter > tickRate) {
            StartTick();
            tickTimeCounter -= tickRate;
        }
        lastUpdateTime = Time.realtimeSinceStartup;
        
    }

    private void StartTick() {
        Debug.LogFormat("Tick as: {0}, {1} components registered", side.ToString(), netComponents.Count);
        Tick(tick++);
    }

    public abstract void Tick(uint tick);

    /// <summary> Returns networked id</summary>
    public uint RegisterComponent(NetBehaviour netBehaviour) {
        if(nextID >= 32768 ) { //32767 should also work, safer this way
            Debug.LogError("ID overflow");
        }
        
        netComponents.Add(nextID++, netBehaviour);
        return nextID - 1;
    }

    public void SetComponentID(uint oldID, uint newID) {
        NetBehaviour comp = netComponents[oldID];
        netComponents.Remove(oldID);
        netComponents.Add(newID, comp);
    }

    public void RemoveComponent(uint id) {
        netComponents.Remove(id);
    }

    public enum Side {
        CLIENT,
        SERVER
    }
}
