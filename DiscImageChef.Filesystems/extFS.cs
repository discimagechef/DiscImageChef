﻿// /***************************************************************************
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
// Copyright © 2011-2018 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using DiscImageChef.CommonTypes;
using DiscImageChef.DiscImages;
using Schemas;

namespace DiscImageChef.Filesystems
{
    // Information from the Linux kernel
    public class extFS : Filesystem
    {
        const int SB_POS = 0x400;

        /// <summary>
        ///     ext superblock magic
        /// </summary>
        const ushort EXT_MAGIC = 0x137D;

        public extFS()
        {
            Name = "Linux extended Filesystem";
            PluginUuid = new Guid("076CB3A2-08C2-4D69-BC8A-FCAA2E502BE2");
            CurrentEncoding = Encoding.GetEncoding("iso-8859-15");
        }

        public extFS(Encoding encoding)
        {
            Name = "Linux extended Filesystem";
            PluginUuid = new Guid("076CB3A2-08C2-4D69-BC8A-FCAA2E502BE2");
            CurrentEncoding = encoding ?? Encoding.GetEncoding("iso-8859-15");
        }

        public extFS(ImagePlugin imagePlugin, Partition partition, Encoding encoding)
        {
            Name = "Linux extended Filesystem";
            PluginUuid = new Guid("076CB3A2-08C2-4D69-BC8A-FCAA2E502BE2");
            CurrentEncoding = encoding ?? Encoding.GetEncoding("iso-8859-15");
        }

        public override bool Identify(ImagePlugin imagePlugin, Partition partition)
        {
            if(imagePlugin.GetSectorSize() < 512) return false;

            ulong sbSectorOff = SB_POS / imagePlugin.GetSectorSize();
            uint sbOff = SB_POS % imagePlugin.GetSectorSize();

            if(sbSectorOff + partition.Start >= partition.End) return false;

            byte[] sbSector = imagePlugin.ReadSector(sbSectorOff + partition.Start);
            byte[] sb = new byte[512];
            Array.Copy(sbSector, sbOff, sb, 0, 512);

            ushort magic = BitConverter.ToUInt16(sb, 0x038);

            return magic == EXT_MAGIC;
        }

        public override void GetInformation(ImagePlugin imagePlugin, Partition partition, out string information)
        {
            information = "";

            StringBuilder sb = new StringBuilder();

            if(imagePlugin.GetSectorSize() < 512) return;

            ulong sbSectorOff = SB_POS / imagePlugin.GetSectorSize();
            uint sbOff = SB_POS % imagePlugin.GetSectorSize();

            if(sbSectorOff + partition.Start >= partition.End) return;

            byte[] sblock = imagePlugin.ReadSector(sbSectorOff + partition.Start);
            byte[] sbSector = new byte[512];
            Array.Copy(sblock, sbOff, sbSector, 0, 512);

            extFSSuperBlock extSb = new extFSSuperBlock
            {
                inodes = BitConverter.ToUInt32(sbSector, 0x000),
                zones = BitConverter.ToUInt32(sbSector, 0x004),
                firstfreeblk = BitConverter.ToUInt32(sbSector, 0x008),
                freecountblk = BitConverter.ToUInt32(sbSector, 0x00C),
                firstfreeind = BitConverter.ToUInt32(sbSector, 0x010),
                freecountind = BitConverter.ToUInt32(sbSector, 0x014),
                firstdatazone = BitConverter.ToUInt32(sbSector, 0x018),
                logzonesize = BitConverter.ToUInt32(sbSector, 0x01C),
                maxsize = BitConverter.ToUInt32(sbSector, 0x020)
            };

            sb.AppendLine("ext filesystem");
            sb.AppendFormat("{0} zones on volume", extSb.zones);
            sb.AppendFormat("{0} free blocks ({1} bytes)", extSb.freecountblk, extSb.freecountblk * 1024);
            sb.AppendFormat("{0} inodes on volume, {1} free ({2}%)", extSb.inodes, extSb.freecountind,
                            extSb.freecountind * 100 / extSb.inodes);
            sb.AppendFormat("First free inode is {0}", extSb.firstfreeind);
            sb.AppendFormat("First free block is {0}", extSb.firstfreeblk);
            sb.AppendFormat("First data zone is {0}", extSb.firstdatazone);
            sb.AppendFormat("Log zone size: {0}", extSb.logzonesize);
            sb.AppendFormat("Max zone size: {0}", extSb.maxsize);

            XmlFsType = new FileSystemType
            {
                Type = "ext",
                FreeClusters = extSb.freecountblk,
                FreeClustersSpecified = true,
                ClusterSize = 1024,
                Clusters = (long)((partition.End - partition.Start + 1) * imagePlugin.GetSectorSize() / 1024)
            };

            information = sb.ToString();
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

        /// <summary>
        ///     ext superblock
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        struct extFSSuperBlock
        {
            /// <summary>0x000, inodes on volume</summary>
            public uint inodes;
            /// <summary>0x004, zones on volume</summary>
            public uint zones;
            /// <summary>0x008, first free block</summary>
            public uint firstfreeblk;
            /// <summary>0x00C, free blocks count</summary>
            public uint freecountblk;
            /// <summary>0x010, first free inode</summary>
            public uint firstfreeind;
            /// <summary>0x014, free inodes count</summary>
            public uint freecountind;
            /// <summary>0x018, first data zone</summary>
            public uint firstdatazone;
            /// <summary>0x01C, log zone size</summary>
            public uint logzonesize;
            /// <summary>0x020, max zone size</summary>
            public uint maxsize;
            /// <summary>0x024, reserved</summary>
            public uint reserved1;
            /// <summary>0x028, reserved</summary>
            public uint reserved2;
            /// <summary>0x02C, reserved</summary>
            public uint reserved3;
            /// <summary>0x030, reserved</summary>
            public uint reserved4;
            /// <summary>0x034, reserved</summary>
            public uint reserved5;
            /// <summary>0x038, 0x137D (little endian)</summary>
            public ushort magic;
        }
    }
}