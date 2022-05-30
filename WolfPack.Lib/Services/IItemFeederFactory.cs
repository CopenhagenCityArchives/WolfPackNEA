using System;
using System.Collections.Generic;
using System.Text;

namespace WolfPack.Lib.Services
{
    interface IItemFeederFactory
    {
        IItemFeeder GetItemFeeder(string location);
    }
}
