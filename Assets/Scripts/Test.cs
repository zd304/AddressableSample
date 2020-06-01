using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using System.Xml;

public class Test : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    private bool copyOver = false;
    private bool lastCopyOver = false;

    string error;
    Vector2 scrollPos = Vector2.zero;

    private void Awake()
    {
        Application.logMessageReceived += HandleLog;

        StartCoroutine(CopyFromCache(Application.streamingAssetsPath, Application.persistentDataPath + "/Bundles"));
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }
    
    void Update()
    {
        if (copyOver && copyOver != lastCopyOver)
        {
            Addressables.LoadAssetAsync<Sprite>("Assets/Res/boy_01.png").Completed += (o) =>
            {
                spriteRenderer.sprite = o.Result;
            };
        }
        lastCopyOver = copyOver;
    }

    private IEnumerator CopyFromCache(string srcDir, string destDir)
    {
        if (Directory.Exists(destDir))
        {
            copyOver = true;
            yield break;
        }

        UnityWebRequest request = UnityWebRequest.Get(srcDir + "/Bundles/Package.data");
        yield return request.SendWebRequest();

        if (request.error == null)
        {
            Directory.CreateDirectory(destDir);

            string text = request.downloadHandler.text;
            if (!string.IsNullOrEmpty(text))
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(text);

                XmlNode rootNode = doc.SelectSingleNode("Root");

                for (int i = 0; i < rootNode.ChildNodes.Count; ++i)
                {
                    XmlElement filesEle = rootNode.ChildNodes[i] as XmlElement;
                    if (filesEle.Name == "Files")
                    {
                        for (int j = 0; j < filesEle.ChildNodes.Count; ++j)
                        {
                            XmlElement fileEle = filesEle.ChildNodes[j] as XmlElement;
                            if (fileEle.Name == "File")
                            {
                                string dir = fileEle.GetAttribute("dir");
                                string name = fileEle.GetAttribute("name");
                                yield return CopyFromCacheFile(srcDir + "/" + dir + "/" + name, destDir + "/" + name);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogError("没有这个文件：" + srcDir + "/Package.data" + " --- " + request.error);
        }

        copyOver = true;
    }

    private IEnumerator CopyFromCacheFile(string srcPath, string destPath)
    {
        UnityWebRequest request = UnityWebRequest.Get(srcPath);
        yield return request.SendWebRequest();

        if (request.error == null)
        {
            byte[] bytes = request.downloadHandler.data;
            if (bytes != null && bytes.Length > 0)
            {
                File.WriteAllBytes(destPath, bytes);
            }
        }
        else
        {
            Debug.LogError("没有这个文件：" + srcPath + " --- " + request.error);
        }
    }

    void HandleLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            error += message;
            error += "\n";
        }
    }

    private void OnGUI()
    {
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        GUILayout.TextField(error);
        GUILayout.EndScrollView();
    }
}
