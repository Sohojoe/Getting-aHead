
using UnityEngine;


public class PlayerInput : MonoBehaviour
{
    GettingAHeadAgent _gettingAHeadAgent;
    private void Awake()
    {
        _gettingAHeadAgent = GetComponent<GettingAHeadAgent>();
    }

    private void Update()
    {
        var x = Input.GetAxis("Horizontal") * Time.deltaTime;
        _gettingAHeadAgent.Jump = Input.GetButton("Fire1");
        _gettingAHeadAgent.MoveRight = true ? x > 0f : false;
        _gettingAHeadAgent.MoveLeft = true ? x < 0f : false;
    }
}