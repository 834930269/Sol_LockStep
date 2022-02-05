using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMonoBehaviour
{
    void Awake();
    void Update();
    void Update(float elapseSeconds, float realElapseSeconds);
    void FixedUpdate();
    void LateUpdate();
    void OnGUI();
    void OnDisable();
    void OnDestroy();
    void OnRelease();
}
