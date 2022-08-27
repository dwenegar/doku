// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

namespace Dwenegar.Doku.Logging
{
    internal interface ILogHandler
    {
        void Handle(ref LogRecord logRecord);

        void Close();
    }
}
