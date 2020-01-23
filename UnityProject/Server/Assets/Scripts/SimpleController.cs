using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleController : MonoBehaviour
{

    public float speed = 0.5f;

    // Start is called before the first frame update
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        
        Vector2 movement = new Vector2(Input.GetAxis("Horizontal") * speed * Time.deltaTime, Input.GetAxis("Vertical") * speed * Time.deltaTime);
        Vector3 position = transform.position;
        position.x += movement.x;
        position.z += movement.y;
        transform.Translate(movement.x, 0, movement.y);
    }
}
