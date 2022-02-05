using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PingMono : MonoSingleton<PingMono>
{
    private float _guiTimer;
    public List<float> delays => GameLaunch.Delays;
    // Update is called once per frame
    void Update()
    {
        if (delays == null) return;
        _guiTimer += Time.deltaTime;
        if(_guiTimer > 0.5f)
        {
            _guiTimer = 0;
            GameLaunch.PingVal = (int)(delays.Sum() * 1000 / Mathf.Max(delays.Count, 1));
            delays.Clear();
        }
    }
    private void OnGUI()
    {
        GUI.Label(new Rect(0, 0, 100, 100), $"!!Ping: {GameLaunch.PingVal}ms");
    }
}
