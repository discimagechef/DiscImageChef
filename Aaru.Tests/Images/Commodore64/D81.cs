﻿// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DOS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Aaru unit testing.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2021 Natalia Portillo
// ****************************************************************************/

using System;
using System.IO;
using Aaru.Checksums;
using Aaru.CommonTypes;
using Aaru.DiscImages;
using Aaru.Filters;
using FluentAssertions.Execution;
using NUnit.Framework;

namespace Aaru.Tests.Images.Commodore64
{
    [TestFixture]
    public class D81
    {
        readonly string[] _testFiles =
        {
            "Strategiegames #01 (19xx)(-).d81.lz"
        };
        readonly ulong[] _sectors =
        {
            // Strategiegames #01 (19xx)(-).d81.lz
            3200
        };
        readonly uint[] _sectorSize =
        {
            // Strategiegames #01 (19xx)(-).d81.lz
            256
        };
        readonly MediaType[] _mediaTypes =
        {
            // Strategiegames #01 (19xx)(-).d81.lz
            MediaType.CBM_35_DD
        };
        readonly string[] _md5S =
        {
            // Strategiegames #01 (19xx)(-).d81.lz
            "e84d86b63e798747c42b27b58ab88665"
        };

        readonly string _dataFolder = Path.Combine(Consts.TEST_FILES_ROOT, "Media image formats", "Commodore D81");

        [Test]
        public void Info()
        {
            Environment.CurrentDirectory = _dataFolder;

            Assert.Multiple(() =>
            {
                for(int i = 0; i < _testFiles.Length; i++)
                {
                    var filter = new LZip();
                    filter.Open(_testFiles[i]);

                    var  image  = new ZZZRawImage();
                    bool opened = image.Open(filter);

                    Assert.AreEqual(true, opened, $"Open: {_testFiles[i]}");

                    using(new AssertionScope())
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.AreEqual(_sectors[i], image.Info.Sectors, _testFiles[i]);
                            Assert.AreEqual(_sectorSize[i], image.Info.SectorSize, _testFiles[i]);
                            Assert.AreEqual(_mediaTypes[i], image.Info.MediaType, _testFiles[i]);
                        });
                    }
                }
            });
        }

        // How many sectors to read at once
        const uint _sectorsToRead = 256;

        [Test]
        public void Hashes()
        {
            Environment.CurrentDirectory = _dataFolder;

            Assert.Multiple(() =>
            {
                for(int i = 0; i < _testFiles.Length; i++)
                {
                    var filter = new LZip();
                    filter.Open(_testFiles[i]);

                    var   image       = new ZZZRawImage();
                    bool  opened      = image.Open(filter);
                    ulong doneSectors = 0;

                    Assert.AreEqual(true, opened, $"Open: {_testFiles[i]}");
                    var ctx = new Md5Context();

                    while(doneSectors < image.Info.Sectors)
                    {
                        byte[] sector;

                        if(image.Info.Sectors - doneSectors >= _sectorsToRead)
                        {
                            sector      =  image.ReadSectors(doneSectors, _sectorsToRead);
                            doneSectors += _sectorsToRead;
                        }
                        else
                        {
                            sector      =  image.ReadSectors(doneSectors, (uint)(image.Info.Sectors - doneSectors));
                            doneSectors += image.Info.Sectors - doneSectors;
                        }

                        ctx.Update(sector);
                    }

                    Assert.AreEqual(_md5S[i], ctx.End(), _testFiles[i]);
                }
            });
        }
    }
}