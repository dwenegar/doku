// Copyright (c) Simone Livieri. For terms of use, see LICENSE.txt

namespace Doku.Logging
{
    internal interface ILogHandler
    {
        void Handle(ref LogRecord logRecord);

        void Close();
    }
}
