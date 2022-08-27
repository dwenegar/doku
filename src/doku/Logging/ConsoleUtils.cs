// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

using System;

namespace Dwenegar.Doku.Logging
{
    internal static class ConsoleUtils
    {
        public static void WriteLine(string line, ConsoleColor? color)
        {
            if (color.HasValue)
            {
                Console.ForegroundColor = color.Value;
                Console.WriteLine(line);
                Console.ResetColor();
            }
            else
            {
                Console.ResetColor();
                Console.WriteLine(line);
            }
        }
    }
}
