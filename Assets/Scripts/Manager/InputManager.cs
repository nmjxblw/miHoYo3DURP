using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private static InputManager _instance;
    public static InputManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<InputManager>();
            }
            return _instance;
        }
    }

    public InputControl inputControl;
    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(this.gameObject);
        }
        inputControl = new InputControl();
    }

    private void OnEnable()
    {
        inputControl.Enable();
    }

    private void Start() { }

    private void Update() { }

    private void OnDisable()
    {
        inputControl.Disable();
    }
}
