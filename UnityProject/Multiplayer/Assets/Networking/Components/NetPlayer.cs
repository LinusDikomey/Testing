using Package;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetPlayer : NetBehaviour {

    public string name = "ERROR";
    public bool hasAuthority = false;

    [Serializable]
    public struct Data {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public string name;
        public uint inputAuthority;

        public Data(Vector3 position, Quaternion rotation, Vector3 scale, string name, uint inputAuthority) {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
            this.name = name;
            this.inputAuthority = inputAuthority;
        }
    }

    RectTransform textTransform;
    TextMeshPro text;
    Transform t;

    public override void Start() {
        GameObject textObj = obj.transform.GetChild(0).gameObject;
        textTransform = textObj.GetComponent<RectTransform>();
        text = textObj.GetComponent<TextMeshPro>();
        t = obj.GetComponent<Transform>();
    }

    public override void ClientTick(ref PlayerInput input) {
        if (hasAuthority) {
            if (Input.GetAxisRaw("Vertical") < 0) {
                input.forward = true;
            } else if (Input.GetAxisRaw("Vertical") > 0) {
                input.back = true;
            }
            if (Input.GetAxisRaw("Horizontal") < 0) {
                input.left = true;
            } else if (Input.GetAxisRaw("Horizontal") > 0) {
                input.right = true;
            }
        }
    }

    int inputOK = 0;
    int inputNotOk = 0;
    uint lastNotOK = 0;
    uint tick = 0;

    public override void ServerTick(ref Dictionary<uint, PlayerInput> inputs) {
        tick++;
        if (inputs.ContainsKey(id)) {
            inputOK++;
            PlayerInput input = inputs[id];
            Vector2 movement = new Vector2();
            if (input.forward) {
                movement.y -= 0.15f;
            } else if (input.back) {
                movement.y += 0.15f;
            }
            if (input.left) {
                movement.x -= 0.15f;
            } else if (input.right) {
                movement.x += 0.15f;
            }
            t.Translate(new Vector3(movement.x, 0, movement.y));
        } else {
            inputNotOk++;
            lastNotOK = tick;
            Debug.Log("No input data received this frame from: " + name);
        }
        GameObject.FindGameObjectWithTag("Debug2").GetComponent<Text>().text = "Input ok percent: " + (inputOK / (float) (inputOK+inputNotOk)) * 100 + "%, not ok: " + inputNotOk + "\nLast notOK: " + lastNotOK;
    }

    public override byte[] GetData() {
        return PackageSerializer.GetBytes(new Data(t.position, t.rotation, t.localScale, name, id));
    }

    public override void SetData(byte[] dataBytes) {
        if (dataBytes == null)
            return;
        Data data = PackageSerializer.GetObject<Data>(dataBytes);
        Transform t = obj.GetComponent<Transform>();
        t.position = data.position;
        t.rotation = data.rotation;
        t.localScale = data.scale;
        hasAuthority = data.inputAuthority == id;
        name = data.name;
        if (data.name == null) {
            Debug.LogError(":::::::::::::name==null");
            name = "PACKAGE READ ERROR";
        }
        text.text = name;
    }
}
