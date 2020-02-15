using Package;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetPlayer : NetAttribute {

    public string name = "ERROR";

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

    public override void Start() {
        GameObject textObj = obj.transform.GetChild(0).gameObject;
        textTransform = textObj.GetComponent<RectTransform>();
        text = textObj.GetComponent<TextMeshPro>();
    }

    public override void ClientTick(byte[] dataPackage, ref PlayerInput input) {
        if (dataPackage == null)
            return;
        Data data = PackageSerializer.GetObject<Data>(dataPackage);
        Transform t = obj.GetComponent<Transform>();
        t.position = data.position;
        t.rotation = data.rotation;
        t.localScale = data.scale;
        name = data.name;
        if(data.name == null) {
            Debug.LogError(":::::::::::::name==null");
            name = "PACKAGE READ ERROR";
        }
        text.text = name;
        //textTransform.rotation = Quaternion.LookRotation(textTransform.position - Camera.main.transform.position);

        if (data.inputAuthority == id) {
            if (Input.GetAxis("Vertical") < 0) {
                input.forward = true;
            } else if (Input.GetAxis("Vertical") > 0) {
                input.back = true;
            }
            if (Input.GetAxis("Horizontal") < 0) {
                input.left = true;
            } else if (Input.GetAxis("Horizontal") > 0) {
                input.right = true;
            }
        }
    }

    int inputOK = 0;
    int inputNotOk = 0;
    uint lastNotOK = 0;
    uint tick = 0;

    public override byte[] ServerTick(ref Dictionary<uint, PlayerInput> inputs) {
        tick++;
        Transform t = obj.GetComponent<Transform>();
        if (inputs.ContainsKey(id)) {
            //GameObject.FindGameObjectWithTag("Debug2").GetComponent<Text>().text = "Input: ok";
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
        return PackageSerializer.GetBytes(new Data(t.position, t.rotation, t.localScale, name, id));
    }
}
