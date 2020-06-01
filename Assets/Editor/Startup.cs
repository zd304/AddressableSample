using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using System.IO;
using System.Xml;
using System.Collections.Generic;

[InitializeOnLoad]
public class Startup
{
    static Startup()
    {
        BuildScript.buildCompleted += OnBuildCompleted;
        Debug.Log("打包回调注册完毕！");
    }

    static void OnBuildCompleted(AddressableAssetBuildResult rst)
    {
        XmlDocument doc = new XmlDocument();
        XmlElement rootEle = doc.CreateElement("Root");
        doc.AppendChild(rootEle);

        XmlElement filesEle = doc.CreateElement("Files");
        rootEle.AppendChild(filesEle);

        var e = rst.FileRegistry.GetFilePaths().GetEnumerator();
        while (e.MoveNext())
        {
            string s = e.Current;
            if (s.Contains(Application.streamingAssetsPath))
            {
                string name = Path.GetFileName(s);
                s = Path.GetDirectoryName(s);

                int index = s.IndexOf(Application.streamingAssetsPath) + Application.streamingAssetsPath.Length;
                s = s.Substring(index + 2);
                s = s.Replace("\\", "/");

                XmlElement fileEle = doc.CreateElement("File");
                fileEle.SetAttribute("dir", s);
                fileEle.SetAttribute("name", name);
                filesEle.AppendChild(fileEle);
            }
        }

        string packageFile = "Bundles/Package.data";
        string packagePath = Application.streamingAssetsPath + "/" + packageFile;
        if (File.Exists(packagePath))
        {
            return;
        }

        doc.Save(packagePath);

        string oldBundlesFolder = Application.persistentDataPath + "/Bundles";
        if (Directory.Exists(oldBundlesFolder))
        {
            string[] files = Directory.GetFiles(oldBundlesFolder, "*.*");
            for (int i = 0; i < files.Length; ++i)
            {
                string file = files[i];
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            Directory.Delete(oldBundlesFolder);
        }
    }
}
