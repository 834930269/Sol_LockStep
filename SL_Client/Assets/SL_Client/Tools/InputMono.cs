using PlayerMsg;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputMono : MonoSingleton<InputMono>
{
    private static bool IsReplay => GameLaunch.Instance.IsReplay;
    [HideInInspector] public int floorMask;
    public float camRayLength = 100;

    public bool hasHitFloor;
    public Vector2 mousePos;
    public Vector2 inputUV;
    public bool isInputFire;
    public int skillId;
    public bool isSpeedUp;

    void Start()
    {
        floorMask = LayerMask.GetMask("Floor");
    }

    private void Update()
    {
        if (!IsReplay)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            inputUV.x = h;inputUV.y = v;

            isInputFire = Input.GetButton("Fire1");
            hasHitFloor = Input.GetMouseButtonDown(1);
            if (hasHitFloor)
            {
                Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit floorHit;
                if (Physics.Raycast(camRay, out floorHit, camRayLength, floorMask))
                {
                    mousePos = floorHit.point;
                }
            }
            skillId = -1;
            for (int i = 0; i < 6; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    skillId = i;
                }
            }

            isSpeedUp = Input.GetKeyDown(KeyCode.Space);


            GameLaunch.CurGameInput = new Msg_PlayerInput();
            Msg_PlayerInput pi = GameLaunch.CurGameInput;
            pi.MousePos.X = mousePos.x; pi.MousePos.Y = mousePos.y;
            pi.InputUV.X = inputUV.x; pi.InputUV.Y = inputUV.y;
            pi.IsInputFire = isInputFire;
            pi.SkillId = skillId;
            pi.IsSpeedUp = isSpeedUp;
        }
    }
}
