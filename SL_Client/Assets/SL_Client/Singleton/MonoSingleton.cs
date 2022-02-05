using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T: MonoSingleton<T>
{
    protected static T _instance = null;
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject DDOLGO = GameObject.Find("DontDestroyOnLoadGO");
                if (DDOLGO == null)
                {
                    DDOLGO = new GameObject("DontDestroyOnLoadGO");
                    DontDestroyOnLoad(DDOLGO);
                }
                GameObject go = new GameObject(typeof(T).ToString());
                go.transform.SetParent(DDOLGO.transform, false);
                _instance = go.AddComponent<T>();
            }
            return _instance;
        }
    }

    private void OnApplicationQuit()
    {
        _instance = null;
    }
}
