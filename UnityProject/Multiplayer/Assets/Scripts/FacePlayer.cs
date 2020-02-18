using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacePlayer : MonoBehaviour {

    Transform textTransform;
    TextMesh text;

    void Start() {
        textTransform = GetComponentInChildren<Transform>();
        text = GetComponentInChildren<TextMesh>();
    }

    void Update() {
        textTransform.rotation = Quaternion.LookRotation(textTransform.position - Camera.main.transform.position);
    }
}
