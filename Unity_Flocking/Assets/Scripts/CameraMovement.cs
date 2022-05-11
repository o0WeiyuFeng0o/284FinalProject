using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float turnSpeed = 1f;

    private Vector2 rot;
    private bool locked = false;

    void Start() {
        rot = new Vector2(transform.eulerAngles.y, -transform.eulerAngles.x);
        Cursor.lockState = CursorLockMode.None;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) {
            transform.position += transform.forward * Time.deltaTime * movementSpeed;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) {
            transform.position -= transform.forward * Time.deltaTime * movementSpeed;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
            transform.position += transform.right * Time.deltaTime * movementSpeed;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
            transform.position -= transform.right * Time.deltaTime * movementSpeed;
        }
        if (Input.GetKey(KeyCode.Space)) {
            transform.position += new Vector3(0, 1, 0) * Time.deltaTime * movementSpeed;
        }
        if (Input.GetKey(KeyCode.LeftShift)) {
            transform.position += new Vector3(0, -1, 0) * Time.deltaTime * movementSpeed;
        }

        if (locked) {
            rot.x += Input.GetAxis("Mouse X") * turnSpeed;
            rot.y += Input.GetAxis("Mouse Y") * turnSpeed;
            transform.localRotation = Quaternion.Euler(-rot.y, rot.x , 0);
        }

        if (Input.GetKeyDown(KeyCode.L)) {
            if (locked) {
                Cursor.lockState = CursorLockMode.None;
                locked = false;
            } else { 
                Cursor.lockState = CursorLockMode.Locked;
                locked = true;
            }
        } 
    }
}
