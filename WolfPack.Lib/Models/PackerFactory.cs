using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;
using WolfPack.Lib.Services;

namespace WolfPack.Lib.Models
{
    public abstract class PackerFactory
    {
        public abstract IPacker GetPacker();
    }
}
