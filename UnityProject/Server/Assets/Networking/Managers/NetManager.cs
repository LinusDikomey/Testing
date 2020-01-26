using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class NetManager : MonoBehaviour {
    public const float tickRate = 0.02f;

    Side side;
    protected Dictionary<uint, NetIdentity> netIdentities = new Dictionary<uint, NetIdentity>();
    float lastUpdateTime;
    float tickTimeCounter = 0f;
    private uint nextID = 1;
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
        //50 ticks: 0.02f
        float delta = Time.realtimeSinceStartup - lastUpdateTime;
        tickTimeCounter += delta;
        while (tickTimeCounter > tickRate) {
            StartTick();
            tickTimeCounter -= tickRate;
        }
        lastUpdateTime = Time.realtimeSinceStartup;

    }

    private void StartTick() {
        GameObject.FindGameObjectWithTag("Debug1").GetComponent<Text>().text = "Tick #" + tick + " as: " + side.ToString() + ", " + netIdentities.Count + " netIdentities registered";
        Tick(tick++);
    }

    public abstract void Tick(uint tick);

    /// <summary> Returns networked id</summary>
    public void RegisterObject(NetIdentity netIdentity, uint id) {
        if (netIdentities.ContainsKey(id)) {
            Debug.LogError("NetIdentity with already existing id tried to register!");
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
}
