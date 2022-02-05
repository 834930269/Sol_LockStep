using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ��ʾ��
/// </summary>
class MessageBox
{
    public BoxResult Result { get; set; }

    private GameObject go;

    public enum BoxResult
    {
        /// <summary>
        /// ��δ�����
        /// </summary>
        None,
        /// <summary>
        /// ѡ��1
        /// </summary>
        First,
        /// <summary>
        /// ѡ��2
        /// </summary>
        Second
    }

    public MessageBox(string messageInfo,string firstText,string secondText)
    {
        UnityEngine.Object asset = Resources.Load("Prefabs/MessageBox");
        go = UnityEngine.Object.Instantiate(asset) as GameObject;
        go.transform.Find("Bg/MessageBox/MessageInfo").GetComponent<Text>().text = messageInfo;

        Transform first = go.transform.Find("Bg/MessageBox/First");

        first.Find("Text").GetComponent<Text>().text = firstText;
        first.GetComponent<Button>().onClick.AddListener(() =>
        {
            Debug.LogError("�����");
            Result = BoxResult.First;
        });

        Transform second = go.transform.Find("Bg/MessageBox/Second");

        second.Find("Text").GetComponent<Text>().text = secondText;

        second.GetComponent<Button>().onClick.AddListener(() =>
        {
            Result = BoxResult.Second;
        });
    }

    public async Task<BoxResult> GetReplyAsync()
    {
        return await Task.Run<BoxResult>(() =>
        {
            while (true)
            {
                if(Result != BoxResult.None)
                {
                    return Result;
                }
            }
        });
    }

    public void Close()
    {
        GameObject.Destroy(go);
    }
}
