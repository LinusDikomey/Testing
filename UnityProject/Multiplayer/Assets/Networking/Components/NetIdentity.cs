using Package;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetIdentity : MonoBehaviour {

    public class NetAttribIDs {
        public const byte ID_TRANSFORM = 0;
        public const byte ID_PLAYER = 1;
    }

    public static NetBehaviour GetNewInstanceByID(byte id) {
        switch (id) {
            case NetAttribIDs.ID_TRANSFORM:
                return new NetTransform();
            case NetAttribIDs.ID_PLAYER:
                return new NetPlayer();
            default:
                return null;
        }
    }

    private const uint MAX_UINT = 4294967295;
    public uint id { get; private set; } = MAX_UINT;

    public byte[] netComponents;
    private NetManager netManager;
    [SerializeField]
    public string prefab { get; private set; }

    bool created = false;
    public string autoPrefab;

    public List<NetBehaviour> netAttributes = new List<NetBehaviour>();

    void Start() {
        if (!created) {
            netManager = GameObject.FindGameObjectWithTag("NetManager").GetComponent<NetManager>();
            Create(netManager.GetFreeID(), autoPrefab);
        }
    }

    public void Create(uint id, string prefab) {
        this.id = id;
        this.prefab = prefab;
        netManager = (NetManager)GameObject.FindGameObjectWithTag("NetManager").GetComponent<NetManager>();
        foreach (byte netComp in netComponents) {
            NetBehaviour newComp = GetNewInstanceByID(netComp);
            newComp.SetParentObj(gameObject, id);
            netAttributes.Add(newComp);
            newComp.Start();
        }
        Debug.LogFormat("Registering {0} with id {1}", prefab, id);
        netManager.RegisterObject(this, id);
        created = true;
    }

    public void ClientTick(ref PlayerInput input) {
        foreach (NetBehaviour comp in netAttributes) {
            comp.ClientTick(ref input);
        }
    }

    public ObjectUpdatePacket ServerTick(ref Dictionary<uint, PlayerInput> input) {
        foreach (NetBehaviour attrib in netAttributes) {
            attrib.ServerTick(ref input);
        }
        return new ObjectUpdatePacket(id, GetComponentPackets());
    }

    public ComponentPacket[] GetComponentPackets() {
        ComponentPacket[] compPackets = new ComponentPacket[netAttributes.Count];
        int index = 0;
        foreach (NetBehaviour attrib in netAttributes) {
            compPackets[index] = new ComponentPacket(attrib.GetData());
            index++;
        }
        return compPackets;
    }

    public ObjectData GetObjectData() {
        return new ObjectData(id, prefab, GetComponentPackets());
    }

    public void ApplyUpdates(ComponentPacket[] compPackets) {
        if (compPackets == null) return;
        int index = 0;
        foreach (ComponentPacket compPacket in compPackets) {
            netAttributes[index].SetData(compPacket.bytes);

            index++;
        }
    }

    public T GetNetComponent<T>() where T : NetBehaviour {
        foreach (NetBehaviour a in netAttributes) {
            if (typeof(T) == a.GetType()) {
                return (T)a;
            }
        }
        return null;
    }

    public T RequireNetComponent<T>() where T : NetBehaviour {
        T t = GetNetComponent<T>();
        if (t == null) {
            Debug.LogError("RequireNetComponent didn't find required component. " +
                "object id: " + id + ", prefab used: " + prefab + ", nonexistent component: " + typeof(T).Name);
            Application.Quit(-2);
        }
        return t;
    }
}