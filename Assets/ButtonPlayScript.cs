using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ButtonPlayScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Function Start");
    }

    // Update is called once per frame
    void Update()
    {
        //   Debug.Log("Function Update");
        transform.position += Vector3.up * 10.0f * Time.deltaTime;

    }
}
