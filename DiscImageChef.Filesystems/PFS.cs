﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : PFS.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Professional File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the Professional File System and shows information.
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
using System.Runtime.InteropServices;
using System.Text;
using DiscImageChef.CommonTypes;
using DiscImageChef.DiscImages;
using Schemas;

namespace DiscImageChef.Filesystems
{
    public class PFS : Filesystem
    {
        /// <summary>
        ///     Identifier for AFS (PFS v1)
        /// </summary>
        const uint AFS_DISK = 0x41465301;
        /// <summary>
        ///     Identifier for PFS v2
        /// </summary>
        const uint PFS2_DISK = 0x50465302;
        /// <summary>
        ///     Identifier for PFS v3
        /// </summary>
        const uint PFS_DISK = 0x50465301;
        /// <summary>
        ///     Identifier for multi-user AFS
        /// </summary>
        const uint MUAF_DISK = 0x6D754146;
        /// <summary>
        ///     Identifier for multi-user PFS
        /// </summary>
        const uint MUPFS_DISK = 0x6D755046;

        public PFS()
        {
            Name = "Professional File System";
            PluginUuid = new Guid("68DE769E-D957-406A-8AE4-3781CA8CDA77");
            CurrentEncoding = Encoding.GetEncoding("iso-8859-1");
        }

        public PFS(Encoding encoding)
        {
            Name = "Professional File System";
            PluginUuid = new Guid("68DE769E-D957-406A-8AE4-3781CA8CDA77");
            CurrentEncoding = encoding ?? Encoding.GetEncoding("iso-8859-1");
        }

        public PFS(ImagePlugin imagePlugin, Partition partition, Encoding encoding)
        {
            Name = "Professional File System";
            PluginUuid = new Guid("68DE769E-D957-406A-8AE4-3781CA8CDA77");
            CurrentEncoding = encoding ?? Encoding.GetEncoding("iso-8859-1");
        }

        public override bool Identify(ImagePlugin imagePlugin, Partition partition)
        {
            if(partition.Length < 3) return false;

            BigEndianBitConverter.IsLittleEndian = BitConverter.IsLittleEndian;

            byte[] sector = imagePlugin.ReadSector(2 + partition.Start);

            uint magic = BigEndianBitConverter.ToUInt32(sector, 0x00);

            return magic == AFS_DISK || magic == PFS2_DISK || magic == PFS_DISK || magic == MUAF_DISK ||
                   magic == MUPFS_DISK;
        }

        public override void GetInformation(ImagePlugin imagePlugin, Partition partition, out string information)
        {
            byte[] rootBlockSector = imagePlugin.ReadSector(2 + partition.Start);
            RootBlock rootBlock = BigEndianMarshal.ByteArrayToStructureBigEndian<RootBlock>(rootBlockSector);

            StringBuilder sbInformation = new StringBuilder();
            XmlFsType = new FileSystemType();

            switch(rootBlock.diskType)
            {
                case AFS_DISK:
                case MUAF_DISK:
                    sbInformation.Append("Professional File System v1");
                    XmlFsType.Type = "PFS v1";
                    break;
                case PFS2_DISK:
                    sbInformation.Append("Professional File System v2");
                    XmlFsType.Type = "PFS v2";
                    break;
                case PFS_DISK:
                case MUPFS_DISK:
                    sbInformation.Append("Professional File System v3");
                    XmlFsType.Type = "PFS v3";
                    break;
            }

            if(rootBlock.diskType == MUAF_DISK || rootBlock.diskType == MUPFS_DISK)
                sbInformation.Append(", with multi-user support");

            sbInformation.AppendLine();

            sbInformation
                .AppendFormat("Volume name: {0}", StringHandlers.PascalToString(rootBlock.diskname, CurrentEncoding))
                .AppendLine();
            sbInformation.AppendFormat("Volume has {0} free sectors of {1}", rootBlock.blocksfree, rootBlock.diskSize)
                         .AppendLine();
            sbInformation.AppendFormat("Volume created on {0}",
                                       DateHandlers.AmigaToDateTime(rootBlock.creationday, rootBlock.creationminute,
                                                                    rootBlock.creationtick)).AppendLine();
            if(rootBlock.extension > 0)
                sbInformation.AppendFormat("Root block extension resides at block {0}", rootBlock.extension)
                             .AppendLine();

            information = sbInformation.ToString();

            XmlFsType.CreationDate =
                DateHandlers.AmigaToDateTime(rootBlock.creationday, rootBlock.creationminute, rootBlock.creationtick);
            XmlFsType.CreationDateSpecified = true;
            XmlFsType.FreeClusters = rootBlock.blocksfree;
            XmlFsType.FreeClustersSpecified = true;
            XmlFsType.Clusters = rootBlock.diskSize;
            XmlFsType.ClusterSize = (int)imagePlugin.GetSectorSize();
            XmlFsType.VolumeName = StringHandlers.PascalToString(rootBlock.diskname, CurrentEncoding);
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
        ///     Boot block, first 2 sectors
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct BootBlock
        {
            /// <summary>
            ///     "PFS\1" disk type
            /// </summary>
            public uint diskType;
            /// <summary>
            ///     Boot code, til completion
            /// </summary>
            public byte[] bootCode;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct RootBlock
        {
            /// <summary>
            ///     Disk type
            /// </summary>
            public uint diskType;
            /// <summary>
            ///     Options
            /// </summary>
            public uint options;
            /// <summary>
            ///     Current datestamp
            /// </summary>
            public uint datestamp;
            /// <summary>
            ///     Volume creation day
            /// </summary>
            public ushort creationday;
            /// <summary>
            ///     Volume creation minute
            /// </summary>
            public ushort creationminute;
            /// <summary>
            ///     Volume creation tick
            /// </summary>
            public ushort creationtick;
            /// <summary>
            ///     AmigaDOS protection bits
            /// </summary>
            public ushort protection;
            /// <summary>
            ///     Volume label (Pascal string)
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] diskname;
            /// <summary>
            ///     Last reserved block
            /// </summary>
            public uint lastreserved;
            /// <summary>
            ///     First reserved block
            /// </summary>
            public uint firstreserved;
            /// <summary>
            ///     Free reserved blocks
            /// </summary>
            public uint reservedfree;
            /// <summary>
            ///     Size of reserved blocks in bytes
            /// </summary>
            public ushort reservedblocksize;
            /// <summary>
            ///     Blocks in rootblock, including bitmap
            /// </summary>
            public ushort rootblockclusters;
            /// <summary>
            ///     Free blocks
            /// </summary>
            public uint blocksfree;
            /// <summary>
            ///     Blocks that must be always free
            /// </summary>
            public uint alwaysfree;
            /// <summary>
            ///     Current bitmapfield number for allocation
            /// </summary>
            public uint rovingPointer;
            /// <summary>
            ///     Pointer to deldir
            /// </summary>
            public uint delDirPtr;
            /// <summary>
            ///     Disk size in sectors
            /// </summary>
            public uint diskSize;
            /// <summary>
            ///     Rootblock extension
            /// </summary>
            public uint extension;
            /// <summary>
            ///     Unused
            /// </summary>
            public uint unused;
        }
    }
}