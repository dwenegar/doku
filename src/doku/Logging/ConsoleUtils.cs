// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

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
