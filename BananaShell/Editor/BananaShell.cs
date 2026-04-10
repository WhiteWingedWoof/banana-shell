using System;
using System.Collections.Generic;

public class BananaShell 
{
    private List<string> history = new List<string>();
    private int lastInputIndex = -1;
    public event Action OnOutputChanged;

    public BananaShell(List<string> existingHistory = null)
    {
        if (existingHistory != null && existingHistory.Count > 0)
        {
            history = new List<string>(existingHistory);
        }
        else
        {
            history.Add("Banana Shell [Version 1.0.0]");
            history.Add("Copyright (C) Workshop2D Corporation. All rights reserved.\n");
        }
    }

    public List<string> GetHistoryList()
    {
        return history;
    }

    public string GetOutput()
    {
        return string.Join("\n", history);
    }

    public void EchoInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        lastInputIndex = history.Count;
        history.Add("> " + input);
        OnOutputChanged?.Invoke();
    }

    public static string FormatExecutionTime(double elapsedSeconds)
    {
        return elapsedSeconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture).PadLeft(6);
    }

    public void AppendExecutionTime(double elapsedSeconds)
    {
        if (lastInputIndex >= 0 && lastInputIndex < history.Count)
        {
            history[lastInputIndex] += $"<color=#888888> {FormatExecutionTime(elapsedSeconds)}s</color>";
            OnOutputChanged?.Invoke();
        }
    }

    public void ExecuteCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        var args = CommandParser.Parse(input);
        if (args.Length > 0)
        {
            string cmd = args[0];
            if (cmd == "ns")
            {
                string result = BananaShellCommands.NewScript(args);
                history.Add($"<color=#55FF55>{result}</color>");
            }
            else
            {
                history.Add($"<color=#FF5555>command '{cmd}' does not exist.</color>");
            }
        }

        history.Add("");

        OnOutputChanged?.Invoke();
    }
}
