using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

public static class Provider_Window_BananaShell
{
    [MenuItem("Window/Workshop2D/Banana Shell", false, 100)]
    [Shortcut("Workshop2D/Banana Shell", KeyCode.KeypadPlus)]
    public static void ShowWindow()
    {
        BananaShellWindow.ShowWindow();
    }
}
