using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

public class Test : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    private bool copyOver = false;
    private bool lastCopyOver = false;

    string error;
    Vector2 scrollPos = Vector2.zero;

    string CopyDestDir
    {
        get
        {
#if UNITY_EDITOR
            return Application.persistentDataPath;
#else

#if UNITY_ANDROID
            return "/storage/emulated/0/Android/data/com.zd304.testaddressable/files";
#elif UNITY_IOS
#endif

#endif
        }
    }

    private void Awake()
    {
        Application.logMessageReceived += HandleLog;

        Debug.LogError("++ " + Application.persistentDataPath + " ++");

        Debug.LogError(" --- " + Addressables.BuildPath + " --- " + Addressables.RuntimePath);

        StartCoroutine(CopyFromCache(Application.streamingAssetsPath + "/Bundles/Android", Application.persistentDataPath + "/Bundles"));
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

        Directory.CreateDirectory(destDir);

        yield return CopyFromCacheFile(srcDir + "/catalog_2020.05.30.08.29.21.json", destDir + "/catalog_2020.05.30.08.29.21.json");
        yield return CopyFromCacheFile(srcDir + "/catalog_2020.05.30.08.29.21.hash", destDir + "/catalog_2020.05.30.08.29.21.hash");
        yield return CopyFromCacheFile(srcDir + "/res_assets_all_81c9c1a7a7a6a2e31e4cf2a132299abe.bundle", destDir + "/res_assets_all_81c9c1a7a7a6a2e31e4cf2a132299abe.bundle");

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
