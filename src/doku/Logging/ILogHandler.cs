// Copyright 2021 Simone Livieri. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the LICENSE file in the project root for more information.

namespace Dwenegar.Doku.Logging
{
    internal interface ILogHandler
    {
        void Handle(ref LogRecord logRecord);

        void Close();
    }
}
