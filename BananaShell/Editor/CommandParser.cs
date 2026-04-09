using System.Collections.Generic;

public static class CommandParser
{
    public static string[] Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new string[0];

        List<string> args = new List<string>();
        bool inQuotes = false;
        string currentArg = "";

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (!string.IsNullOrEmpty(currentArg))
                {
                    args.Add(currentArg);
                    currentArg = "";
                }
            }
            else
            {
                currentArg += c;
            }
        }

        if (!string.IsNullOrEmpty(currentArg))
        {
            args.Add(currentArg);
        }

        return args.ToArray();
    }
}
