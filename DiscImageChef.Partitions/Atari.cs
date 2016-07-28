﻿// /***************************************************************************
// The Disc Image Chef
// ----------------------------------------------------------------------------
//
// Filename       : Atari.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Partitioning scheme plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Manages Atari ST GEMDOS partitions.
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
using System.Collections.Generic;
using System.Text;
using DiscImageChef;

// Information learnt from XNU source and testing against real disks
using DiscImageChef.Console;


namespace DiscImageChef.PartPlugins
{
    class AtariPartitions : PartPlugin
    {
        const UInt32 TypeGEMDOS = 0x0047454D;
        const UInt32 TypeBigGEMDOS = 0x0042474D;
        const UInt32 TypeExtended = 0x0058474D;
        const UInt32 TypeLinux = 0x004C4E58;
        const UInt32 TypeSwap = 0x00535750;
        const UInt32 TypeRAW = 0x00524157;
        const UInt32 TypeNetBSD = 0x004E4244;
        const UInt32 TypeNetBSDSwap = 0x004E4253;
        const UInt32 TypeSysV = 0x00554E58;
        const UInt32 TypeMac = 0x004D4143;
        const UInt32 TypeMinix = 0x004D4958;
        const UInt32 TypeMinix2 = 0x004D4E58;

        public AtariPartitions()
        {
            Name = "Atari partitions";
            PluginUUID = new Guid("d1dd0f24-ec39-4c4d-9072-be31919a3b5e");
        }

        public override bool GetInformation(ImagePlugins.ImagePlugin imagePlugin, out List<CommonTypes.Partition> partitions)
        {
            partitions = new List<CommonTypes.Partition>();

            if(imagePlugin.GetSectorSize() < 512)
                return false;

            BigEndianBitConverter.IsLittleEndian = BitConverter.IsLittleEndian;

            byte[] sector = imagePlugin.ReadSector(0);

            AtariTable table = new AtariTable();
            table.boot = new byte[342];
            table.icdEntries = new AtariEntry[8];
            table.unused = new byte[12];
            table.entries = new AtariEntry[4];

            Array.Copy(sector, 0, table.boot, 0, 342);

            for(int i = 0; i < 8; i++)
            {
                table.icdEntries[i].type = BigEndianBitConverter.ToUInt32(sector, 342 + i * 12 + 0);
                table.icdEntries[i].start = BigEndianBitConverter.ToUInt32(sector, 342 + i * 12 + 4);
                table.icdEntries[i].length = BigEndianBitConverter.ToUInt32(sector, 342 + i * 12 + 8);
            }

            Array.Copy(sector, 438, table.unused, 0, 12);

            table.size = BigEndianBitConverter.ToUInt32(sector, 450);

            for(int i = 0; i < 4; i++)
            {
                table.entries[i].type = BigEndianBitConverter.ToUInt32(sector, 454 + i * 12 + 0);
                table.entries[i].start = BigEndianBitConverter.ToUInt32(sector, 454 + i * 12 + 4);
                table.entries[i].length = BigEndianBitConverter.ToUInt32(sector, 454 + i * 12 + 8);
            }

            table.badStart = BigEndianBitConverter.ToUInt32(sector, 502);
            table.badLength = BigEndianBitConverter.ToUInt32(sector, 506);
            table.checksum = BigEndianBitConverter.ToUInt16(sector, 510);

            Checksums.SHA1Context sha1Ctx = new Checksums.SHA1Context();
            sha1Ctx.Init();
            sha1Ctx.Update(table.boot);
            DicConsole.DebugWriteLine("Atari partition plugin", "Boot code SHA1: {0}", sha1Ctx.End());

            for(int i = 0; i < 8; i++)
            {
                DicConsole.DebugWriteLine("Atari partition plugin", "table.icdEntries[{0}].flag = 0x{1:X2}", i, (table.icdEntries[i].type & 0xFF000000) >> 24);
                DicConsole.DebugWriteLine("Atari partition plugin", "table.icdEntries[{0}].type = 0x{1:X6}", i, (table.icdEntries[i].type & 0x00FFFFFF));
                DicConsole.DebugWriteLine("Atari partition plugin", "table.icdEntries[{0}].start = {1}", i, table.icdEntries[i].start);
                DicConsole.DebugWriteLine("Atari partition plugin", "table.icdEntries[{0}].length = {1}", i, table.icdEntries[i].length);
            }

            DicConsole.DebugWriteLine("Atari partition plugin", "table.size = {0}", table.size);

            for(int i = 0; i < 4; i++)
            {
                DicConsole.DebugWriteLine("Atari partition plugin", "table.entries[{0}].flag = 0x{1:X2}", i, (table.entries[i].type & 0xFF000000) >> 24);
                DicConsole.DebugWriteLine("Atari partition plugin", "table.entries[{0}].type = 0x{1:X6}", i, (table.entries[i].type & 0x00FFFFFF));
                DicConsole.DebugWriteLine("Atari partition plugin", "table.entries[{0}].start = {1}", i, table.entries[i].start);
                DicConsole.DebugWriteLine("Atari partition plugin", "table.entries[{0}].length = {1}", i, table.entries[i].length);
            }

            DicConsole.DebugWriteLine("Atari partition plugin", "table.badStart = {0}", table.badStart);
            DicConsole.DebugWriteLine("Atari partition plugin", "table.badLength = {0}", table.badLength);
            DicConsole.DebugWriteLine("Atari partition plugin", "table.checksum = 0x{0:X4}", table.checksum);

            bool validTable = false;
            ulong partitionSequence = 0;
            for(int i = 0; i < 4; i++)
            {
                UInt32 type = table.entries[i].type & 0x00FFFFFF;

                if(type == TypeGEMDOS || type == TypeBigGEMDOS || type == TypeLinux ||
                    type == TypeSwap || type == TypeRAW || type == TypeNetBSD ||
                    type == TypeNetBSDSwap || type == TypeSysV || type == TypeMac ||
                    type == TypeMinix || type == TypeMinix2)
                {
                    validTable = true;

                    if(table.entries[i].start <= imagePlugin.GetSectors())
                    {
                        if((table.entries[i].start + table.entries[i].length) > imagePlugin.GetSectors())
                            DicConsole.DebugWriteLine("Atari partition plugin", "WARNING: End of partition goes beyond device size");

                        ulong sectorSize = imagePlugin.GetSectorSize();
                        if(sectorSize == 2448 || sectorSize == 2352)
                            sectorSize = 2048;

                        CommonTypes.Partition part = new CommonTypes.Partition();
                        part.PartitionLength = table.entries[i].length * sectorSize;
                        part.PartitionSectors = table.entries[i].length;
                        part.PartitionSequence = partitionSequence;
                        part.PartitionName = "";
                        part.PartitionStart = table.entries[i].start * sectorSize;
                        part.PartitionStartSector = table.entries[i].start;

                        byte[] partType = new byte[3];
                        partType[0] = (byte)((type & 0xFF0000) >> 16);
                        partType[1] = (byte)((type & 0x00FF00) >> 8);
                        partType[2] = (byte)(type & 0x0000FF);
                        part.PartitionType = Encoding.ASCII.GetString(partType);

                        switch(type)
                        {
                            case TypeGEMDOS:
                                part.PartitionDescription = "Atari GEMDOS partition";
                                break;
                            case TypeBigGEMDOS:
                                part.PartitionDescription = "Atari GEMDOS partition bigger than 32 MiB";
                                break;
                            case TypeLinux:
                                part.PartitionDescription = "Linux partition";
                                break;
                            case TypeSwap:
                                part.PartitionDescription = "Swap partition";
                                break;
                            case TypeRAW:
                                part.PartitionDescription = "RAW partition";
                                break;
                            case TypeNetBSD:
                                part.PartitionDescription = "NetBSD partition";
                                break;
                            case TypeNetBSDSwap:
                                part.PartitionDescription = "NetBSD swap partition";
                                break;
                            case TypeSysV:
                                part.PartitionDescription = "Atari UNIX partition";
                                break;
                            case TypeMac:
                                part.PartitionDescription = "Macintosh partition";
                                break;
                            case TypeMinix:
                            case TypeMinix2:
                                part.PartitionDescription = "MINIX partition";
                                break;
                            default:
                                part.PartitionDescription = "Unknown partition type";
                                break;
                        }

                        partitions.Add(part);
                        partitionSequence++;
                    }
                }

                if(type == TypeExtended)
                {
                    byte[] extendedSector = imagePlugin.ReadSector(table.entries[i].start);
                    AtariTable extendedTable = new AtariTable();
                    extendedTable.entries = new AtariEntry[4];

                    for(int j = 0; j < 4; j++)
                    {
                        extendedTable.entries[j].type = BigEndianBitConverter.ToUInt32(extendedSector, 454 + j * 12 + 0);
                        extendedTable.entries[j].start = BigEndianBitConverter.ToUInt32(extendedSector, 454 + j * 12 + 4);
                        extendedTable.entries[j].length = BigEndianBitConverter.ToUInt32(extendedSector, 454 + j * 12 + 8);
                    }

                    for(int j = 0; j < 4; j++)
                    {
                        UInt32 extendedType = extendedTable.entries[j].type & 0x00FFFFFF;

                        if(extendedType == TypeGEMDOS || extendedType == TypeBigGEMDOS || extendedType == TypeLinux ||
                            extendedType == TypeSwap || extendedType == TypeRAW || extendedType == TypeNetBSD ||
                            extendedType == TypeNetBSDSwap || extendedType == TypeSysV || extendedType == TypeMac ||
                            extendedType == TypeMinix || extendedType == TypeMinix2)
                        {
                            validTable = true;
                            if(extendedTable.entries[j].start <= imagePlugin.GetSectors())
                            {
                                if((extendedTable.entries[j].start + extendedTable.entries[j].length) > imagePlugin.GetSectors())
                                    DicConsole.DebugWriteLine("Atari partition plugin", "WARNING: End of partition goes beyond device size");

                                ulong sectorSize = imagePlugin.GetSectorSize();
                                if(sectorSize == 2448 || sectorSize == 2352)
                                    sectorSize = 2048;

                                CommonTypes.Partition part = new CommonTypes.Partition();
                                part.PartitionLength = extendedTable.entries[j].length * sectorSize;
                                part.PartitionSectors = extendedTable.entries[j].length;
                                part.PartitionSequence = partitionSequence;
                                part.PartitionName = "";
                                part.PartitionStart = extendedTable.entries[j].start * sectorSize;
                                part.PartitionStartSector = extendedTable.entries[j].start;

                                byte[] partType = new byte[3];
                                partType[0] = (byte)((extendedType & 0xFF0000) >> 16);
                                partType[1] = (byte)((extendedType & 0x00FF00) >> 8);
                                partType[2] = (byte)(extendedType & 0x0000FF);
                                part.PartitionType = Encoding.ASCII.GetString(partType);

                                switch(extendedType)
                                {
                                    case TypeGEMDOS:
                                        part.PartitionDescription = "Atari GEMDOS partition";
                                        break;
                                    case TypeBigGEMDOS:
                                        part.PartitionDescription = "Atari GEMDOS partition bigger than 32 MiB";
                                        break;
                                    case TypeLinux:
                                        part.PartitionDescription = "Linux partition";
                                        break;
                                    case TypeSwap:
                                        part.PartitionDescription = "Swap partition";
                                        break;
                                    case TypeRAW:
                                        part.PartitionDescription = "RAW partition";
                                        break;
                                    case TypeNetBSD:
                                        part.PartitionDescription = "NetBSD partition";
                                        break;
                                    case TypeNetBSDSwap:
                                        part.PartitionDescription = "NetBSD swap partition";
                                        break;
                                    case TypeSysV:
                                        part.PartitionDescription = "Atari UNIX partition";
                                        break;
                                    case TypeMac:
                                        part.PartitionDescription = "Macintosh partition";
                                        break;
                                    case TypeMinix:
                                    case TypeMinix2:
                                        part.PartitionDescription = "MINIX partition";
                                        break;
                                    default:
                                        part.PartitionDescription = "Unknown partition type";
                                        break;
                                }

                                partitions.Add(part);
                                partitionSequence++;
                            }
                        }
                    }
                }
            }

            if(validTable)
            {
                for(int i = 0; i < 8; i++)
                {
                    UInt32 type = table.icdEntries[i].type & 0x00FFFFFF;

                    if(type == TypeGEMDOS || type == TypeBigGEMDOS || type == TypeLinux ||
                        type == TypeSwap || type == TypeRAW || type == TypeNetBSD ||
                        type == TypeNetBSDSwap || type == TypeSysV || type == TypeMac ||
                        type == TypeMinix || type == TypeMinix2)
                    {
                        if(table.icdEntries[i].start <= imagePlugin.GetSectors())
                        {
                            if((table.icdEntries[i].start + table.icdEntries[i].length) > imagePlugin.GetSectors())
                                DicConsole.DebugWriteLine("Atari partition plugin", "WARNING: End of partition goes beyond device size");

                            ulong sectorSize = imagePlugin.GetSectorSize();
                            if(sectorSize == 2448 || sectorSize == 2352)
                                sectorSize = 2048;

                            CommonTypes.Partition part = new CommonTypes.Partition();
                            part.PartitionLength = table.icdEntries[i].length * sectorSize;
                            part.PartitionSectors = table.icdEntries[i].length;
                            part.PartitionSequence = partitionSequence;
                            part.PartitionName = "";
                            part.PartitionStart = table.icdEntries[i].start * sectorSize;
                            part.PartitionStartSector = table.icdEntries[i].start;

                            byte[] partType = new byte[3];
                            partType[0] = (byte)((type & 0xFF0000) >> 16);
                            partType[1] = (byte)((type & 0x00FF00) >> 8);
                            partType[2] = (byte)(type & 0x0000FF);
                            part.PartitionType = Encoding.ASCII.GetString(partType);

                            switch(type)
                            {
                                case TypeGEMDOS:
                                    part.PartitionDescription = "Atari GEMDOS partition";
                                    break;
                                case TypeBigGEMDOS:
                                    part.PartitionDescription = "Atari GEMDOS partition bigger than 32 MiB";
                                    break;
                                case TypeLinux:
                                    part.PartitionDescription = "Linux partition";
                                    break;
                                case TypeSwap:
                                    part.PartitionDescription = "Swap partition";
                                    break;
                                case TypeRAW:
                                    part.PartitionDescription = "RAW partition";
                                    break;
                                case TypeNetBSD:
                                    part.PartitionDescription = "NetBSD partition";
                                    break;
                                case TypeNetBSDSwap:
                                    part.PartitionDescription = "NetBSD swap partition";
                                    break;
                                case TypeSysV:
                                    part.PartitionDescription = "Atari UNIX partition";
                                    break;
                                case TypeMac:
                                    part.PartitionDescription = "Macintosh partition";
                                    break;
                                case TypeMinix:
                                case TypeMinix2:
                                    part.PartitionDescription = "MINIX partition";
                                    break;
                                default:
                                    part.PartitionDescription = "Unknown partition type";
                                    break;
                            }

                            partitions.Add(part);
                            partitionSequence++;
                        }
                    }
                }
            }

            return partitions.Count > 0;
        }

        /// <summary>
        /// Atari partition entry
        /// </summary>
        struct AtariEntry
        {
            /// <summary>
            /// First byte flag, three bytes type in ASCII.
            /// Flag bit 0 = active
            /// Flag bit 7 = bootable
            /// </summary>
            public UInt32 type;
            /// <summary>
            /// Starting sector
            /// </summary>
            public UInt32 start;
            /// <summary>
            /// Length in sectors
            /// </summary>
            public UInt32 length;
        }

        struct AtariTable
        {
            /// <summary>
            /// Boot code for 342 bytes
            /// </summary>
            public byte[] boot;
            /// <summary>
            /// 8 extra entries for ICDPro driver
            /// </summary>
            public AtariEntry[] icdEntries;
            /// <summary>
            /// Unused, 12 bytes
            /// </summary>
            public byte[] unused;
            /// <summary>
            /// Disk size in sectors
            /// </summary>
            public UInt32 size;
            /// <summary>
            /// 4 partition entries
            /// </summary>
            public AtariEntry[] entries;
            /// <summary>
            /// Starting sector of bad block list
            /// </summary>
            public UInt32 badStart;
            /// <summary>
            /// Length in sectors of bad block list
            /// </summary>
            public UInt32 badLength;
            /// <summary>
            /// Checksum for bootable disks
            /// </summary>
            public UInt16 checksum;
        }
    }
}