using System;
using System.Collections.Generic;

public class BananaShell 
{
    private List<string> history = new List<string>();
    public event Action OnOutputChanged;

    public BananaShell()
    {
        history.Add("Banana Shell [Version 1.0.0]");
        history.Add("(c) Unity Technologies & Banana Corp. All rights reserved.\n");
    }

    public string GetOutput()
    {
        return string.Join("\n", history) + "\n\n>";
    }

    public void Execute(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;
        
        history.Add("> " + input);

        var args = CommandParser.Parse(input);
        if (args.Length > 0)
        {
            string cmd = args[0];
            if (cmd == "ns")
            {
                string result = BananaShellCommands.NewScript(args);
                history.Add(result);
            }
            else
            {
                history.Add($"'{cmd}' is not recognized as an internal or external command, operable program or batch file.");
            }
        }

        OnOutputChanged?.Invoke();
    }
}
