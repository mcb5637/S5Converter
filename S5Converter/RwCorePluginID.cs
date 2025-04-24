using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S5Converter
{
    internal enum RwCorePluginID : UInt32
    {
        NAOBJECT = 0,
        STRUCT = 1,
        STRING = 2,
        EXTENSION = 3,
        CAMERA = 5,
        TEXTURE = 6,
        MATERIAL = 7,
        MATLIST = 8,
        ATOMICSECT = 9,
        PLANESECT = 0xA,
        WORLD = 0xB,
        SPLINE = 0xC,
        MATRIX = 0xD,
        FRAMELIST = 0xE,
        GEOMETRY = 0xF,
        CLUMP = 0x10,
        LIGHT = 0x12,
        UNICODESTRING = 0x13,
        ATOMIC = 0x14,
        TEXTURENATIVE = 0x15,
        TEXDICTIONARY = 0x16,
        ANIMDATABASE = 0x17,
        IMAGE = 0x18,
        SKINANIMATION = 0x19,
        GEOMETRYLIST = 0x1A,
        ANIMANIMATION = 0x1B,
        HANIMANIMATION = 0x1B,
        TEAM = 0x1C,
        CROWD = 0x1D,
        DMORPHANIMATION = 0x1E,
        RIGHTTORENDER = 0x1f,
        MTEFFECTNATIVE = 0x20,
        MTEFFECTDICT = 0x21,
        TEAMDICTIONARY = 0x22,
        PITEXDICTIONARY = 0x23,
        TOC = 0x24,
        PRTSTDGLOBALDATA = 0x25,
        ALTPIPE = 0x26,
        PIPEDS = 0x27,
        PATCHMESH = 0x28,
        CHUNKGROUPSTART = 0x29,
        CHUNKGROUPEND = 0x2A,
        UVANIMDICT = 0x2B,
        COLLTREE = 0x2C,
        ENVIRONMENT = 0x2D,

        MORPHPLUGIN = 261,
        SKINPLUGIN = 278,
        HANIMPLUGIN = 286,
        USERDATAPLUGIN = 287,
        MATERIALEFFECTSPLUGIN = 288,
        PRTSTDPLUGIN = 304,
        UVANIMPLUGIN = 309,

        BINMESHPLUGIN = 1294,
    }
}
