using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text;
using WolfPack.Lib.Services;

namespace WolfPack.Lib.Models
{
    public class ZipPackerFactory : PackerFactory
    {
        private IFileSystem _fileSystem;
        private string _passPhrase;

        public ZipPackerFactory(string passPhrase, IFileSystem fileSystem)
        {
            _passPhrase = passPhrase;
            _fileSystem = fileSystem;
        }
        public override IPacker GetPacker()
        {
            return new ZipPacker(_passPhrase, _fileSystem);
        }
    }
}
