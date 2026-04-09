using UnityEditor;
using UnityEngine;
using System.IO;
using PixieBox.Editor;

public static class BananaShellCommands
{
    // XXX Utils
    private static string GetActiveFolderPath()
    {
        return PixieBox.PathUtils.GetProjectWindowActiveFolder();
    }

    // ns
    public static string NewScript(string[] args)
    {
        string name = "NewScript";
        if (args.Length > 1) 
        {
            name = args[1];
        }

        // namespace implemented later
        // string namespaceStr = args.Length > 2 ? args[2] : "";
        
        string activeFolder = GetActiveFolderPath();
        string uniquePath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(activeFolder, name + ".cs"));
        
        string className = Path.GetFileNameWithoutExtension(uniquePath);
        
        // Ensure valid class name (replace invalid characters)
        className = className.Replace(" ", "_").Replace("-", "_");
        
        string template = 
$@"using UnityEngine;

public class {className} : MonoBehaviour
{{
    void Start()
    {{
        
    }}

    void Update()
    {{
        
    }}
}}";
        File.WriteAllText(uniquePath, template);
        AssetDatabase.ImportAsset(uniquePath);
        
        return $"success: new script created in : {activeFolder}";
    }


}
