// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : extFS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the Linux extended filesystem and shows information.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2016 Natalia Portillo
// ****************************************************************************/

using System;
using System.Text;
using DiscImageChef;
using System.Collections.Generic;

// Information from the Linux kernel
namespace DiscImageChef.Filesystems
{
    class extFS : Filesystem
    {
        public extFS()
        {
            Name = "Linux extended Filesystem";
            PluginUUID = new Guid("076CB3A2-08C2-4D69-BC8A-FCAA2E502BE2");
        }

        public extFS(ImagePlugins.ImagePlugin imagePlugin, ulong partitionStart, ulong partitionEnd)
        {
            Name = "Linux extended Filesystem";
            PluginUUID = new Guid("076CB3A2-08C2-4D69-BC8A-FCAA2E502BE2");
        }

        public override bool Identify(ImagePlugins.ImagePlugin imagePlugin, ulong partitionStart, ulong partitionEnd)
        {
            if((2 + partitionStart) >= imagePlugin.GetSectors())
                return false;

            byte[] sb_sector = imagePlugin.ReadSector(2 + partitionStart); // Superblock resides at 0x400

            UInt16 magic = BitConverter.ToUInt16(sb_sector, 0x038); // Here should reside magic number

            return magic == extFSMagic;
        }

        public override void GetInformation(ImagePlugins.ImagePlugin imagePlugin, ulong partitionStart, ulong partitionEnd, out string information)
        {
            information = "";

            StringBuilder sb = new StringBuilder();

            byte[] sb_sector = imagePlugin.ReadSector(2 + partitionStart); // Superblock resides at 0x400
            extFSSuperBlock ext_sb = new extFSSuperBlock();

            ext_sb.inodes = BitConverter.ToUInt32(sb_sector, 0x000);
            ext_sb.zones = BitConverter.ToUInt32(sb_sector, 0x004);
            ext_sb.firstfreeblk = BitConverter.ToUInt32(sb_sector, 0x008);
            ext_sb.freecountblk = BitConverter.ToUInt32(sb_sector, 0x00C);
            ext_sb.firstfreeind = BitConverter.ToUInt32(sb_sector, 0x010);
            ext_sb.freecountind = BitConverter.ToUInt32(sb_sector, 0x014);
            ext_sb.firstdatazone = BitConverter.ToUInt32(sb_sector, 0x018);
            ext_sb.logzonesize = BitConverter.ToUInt32(sb_sector, 0x01C);
            ext_sb.maxsize = BitConverter.ToUInt32(sb_sector, 0x020);

            sb.AppendLine("ext filesystem");
            sb.AppendFormat("{0} zones on volume", ext_sb.zones);
            sb.AppendFormat("{0} free blocks ({1} bytes)", ext_sb.freecountblk, ext_sb.freecountblk * 1024);
            sb.AppendFormat("{0} inodes on volume, {1} free ({2}%)", ext_sb.inodes, ext_sb.freecountind, ext_sb.freecountind * 100 / ext_sb.inodes);
            sb.AppendFormat("First free inode is {0}", ext_sb.firstfreeind);
            sb.AppendFormat("First free block is {0}", ext_sb.firstfreeblk);
            sb.AppendFormat("First data zone is {0}", ext_sb.firstdatazone);
            sb.AppendFormat("Log zone size: {0}", ext_sb.logzonesize);
            sb.AppendFormat("Max zone size: {0}", ext_sb.maxsize);

            xmlFSType = new Schemas.FileSystemType();
            xmlFSType.Type = "ext";
            xmlFSType.FreeClusters = ext_sb.freecountblk;
            xmlFSType.FreeClustersSpecified = true;
            xmlFSType.ClusterSize = 1024;
            xmlFSType.Clusters = (long)((partitionEnd - partitionStart + 1) * imagePlugin.GetSectorSize() / 1024);

            information = sb.ToString();
        }

        /// <summary>
        /// ext superblock magic
        /// </summary>
        public const UInt16 extFSMagic = 0x137D;

        /// <summary>
        /// ext superblock
        /// </summary>
        public struct extFSSuperBlock
        {
            /// <summary>0x000, inodes on volume</summary>
            public UInt32 inodes;
            /// <summary>0x004, zones on volume</summary>
            public UInt32 zones;
            /// <summary>0x008, first free block</summary>
            public UInt32 firstfreeblk;
            /// <summary>0x00C, free blocks count</summary>
            public UInt32 freecountblk;
            /// <summary>0x010, first free inode</summary>
            public UInt32 firstfreeind;
            /// <summary>0x014, free inodes count</summary>
            public UInt32 freecountind;
            /// <summary>0x018, first data zone</summary>
            public UInt32 firstdatazone;
            /// <summary>0x01C, log zone size</summary>
            public UInt32 logzonesize;
            /// <summary>0x020, max zone size</summary>
            public UInt32 maxsize;
            /// <summary>0x024, reserved</summary>
            public UInt32 reserved1;
            /// <summary>0x028, reserved</summary>
            public UInt32 reserved2;
            /// <summary>0x02C, reserved</summary>
            public UInt32 reserved3;
            /// <summary>0x030, reserved</summary>
            public UInt32 reserved4;
            /// <summary>0x034, reserved</summary>
            public UInt32 reserved5;
            /// <summary>0x038, 0x137D (little endian)</summary>
            public UInt16 magic;
        }

        public override Errno Mount()
        {
            return Errno.NotImplemented;
        }

        public override Errno Mount(bool debug)
        {
            return Errno.NotImplemented;
        }

        public override Errno Unmount()
        {
            return Errno.NotImplemented;
        }

        public override Errno MapBlock(string path, long fileBlock, ref long deviceBlock)
        {
            return Errno.NotImplemented;
        }

        public override Errno GetAttributes(string path, ref FileAttributes attributes)
        {
            return Errno.NotImplemented;
        }

        public override Errno ListXAttr(string path, ref List<string> xattrs)
        {
            return Errno.NotImplemented;
        }

        public override Errno GetXattr(string path, string xattr, ref byte[] buf)
        {
            return Errno.NotImplemented;
        }

        public override Errno Read(string path, long offset, long size, ref byte[] buf)
        {
            return Errno.NotImplemented;
        }

        public override Errno ReadDir(string path, ref List<string> contents)
        {
            return Errno.NotImplemented;
        }

        public override Errno StatFs(ref FileSystemInfo stat)
        {
            return Errno.NotImplemented;
        }

        public override Errno Stat(string path, ref FileEntryInfo stat)
        {
            return Errno.NotImplemented;
        }

        public override Errno ReadLink(string path, ref string dest)
        {
            return Errno.NotImplemented;
        }
    }
}