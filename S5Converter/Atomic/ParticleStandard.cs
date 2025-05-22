using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter.Atomic
{
    internal class ParticleStandard
    {
        public required int Flags;
        public required RpPrtStdEmitter[] Emitters = [];

        internal int Size => sizeof(int) + Emitters.Sum(x => x.GetSize(Flags));
        internal int SizeH => Size + ChunkHeader.Size;

        internal static ParticleStandard Read(BinaryReader s, bool header, bool convertRad)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.PRTSTDPLUGIN);
            int f = s.ReadInt32();
            int nEmitters = f & 0xFFFFFF;
            ParticleStandard r = new()
            {
                Flags = f >> 24,
                Emitters = new RpPrtStdEmitter[nEmitters],
            };
            for (int i = 0; i < nEmitters; ++i)
                r.Emitters[i] = RpPrtStdEmitter.Read(s, r.Flags, convertRad);
            return r;
        }

        internal void Write(BinaryWriter s, bool header, bool convertRad, uint versionNum, uint buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.PRTSTDPLUGIN,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            s.Write(Flags << 24 | Emitters.Length);
            foreach (RpPrtStdEmitter e in Emitters)
                e.Write(s, Flags, convertRad, versionNum, buildNum);
        }
    }

    internal class RpPrtStdEmitter : IJsonOnDeserialized
    {
        internal int EmitterClassId = 0;
        public required int EmitterFlags = 0;
        internal int ParticleClassId = 0;
        public required int MaxParticlesPerBatch = 0;

        internal RpPrtStdPropertyTable<ParticleProperties> ParticleProps = new();
        internal RpPrtStdPropertyTable<EmitterProperties> EmitterProps = new();
        internal RpPrtStdParticleClass ParticleClass = new();
        internal RpPrtStdEmitterClass EmitterClass = new();

        public int ParticlePropsId
        {
            get => ParticleClass.PropertyId;
            set => ParticleClass.PropertyId = value;
        }
        public int EmitterPropsId
        {
            get => EmitterClass.PropertyId;
            set => EmitterClass.PropertyId = value;
        }

        public RpPrtStdEmitterStandard? EmitterStandard;
        public RpPrtStdEmitterPrtColor? Color;
        public RpPrtStdEmitterPrtTexCoords? TextureCoordinates;
        public RpPrtStdEmitterPrtMatrix? Matrix;
        public RpPrtStdEmitterPrtSize? ParticleSize;
        public RpPrtStdEmitterPrt2DRotate? Rotate;
        public RpPrtStdEmitterPTank? Tank;
        public RpPrtAdvEmtPointList? AdvPointList;
        public RpPrtAdvEmtCircle? AdvCircle;
        public RpPrtAdvEmtSphere? AdvSphere;
        public RpPrtAdvEmtPrtEmt? AdvEmittingEmitter;
        public RpPrtAdvEmtPrtMultiColor? AdvMultiColor;
        public RpPrtAdvEmtPrtMultiTexCoords? AdvMultiTexCoords;
        public RpPrtAdvEmtPrtMultiSize? AdvMultiSize;
        public RpPrtAdvEmtPrtMultiTexCoords? AdvMultiTexCoordsStep;


        public void OnDeserialized()
        {
            int idemit = 0;
            int idpart = 0;
            List<EmitterProperties> propemit = [EmitterProperties.EMITTER];
            List<int> sizesemit = [40];
            List<ParticleProperties> proppart = [];
            List<int> sizespart = [];
            if (EmitterStandard != null)
            {
                idemit |= RpPrtStdEmitterStandard.EmitterId;
                propemit.Add(EmitterProperties.STANDARD);
                sizesemit.Add(RpPrtStdEmitterStandard.EmitterPropSize);
                idpart |= RpPrtStdEmitterStandard.ParticleId;
                proppart.Add(ParticleProperties.STANDARD);
                sizespart.Add(RpPrtStdEmitterStandard.ParticlePropSize);
                idpart |= 0x00000040; // velocity
                proppart.Add(ParticleProperties.VELOCITY);
                sizespart.Add(12);
            }
            if (Color != null)
            {
                idemit |= RpPrtStdEmitterPrtColor.EmitterId;
                propemit.Add(EmitterProperties.PRTCOLOR);
                sizesemit.Add(RpPrtStdEmitterPrtColor.PropSize);
                idpart |= RpPrtStdEmitterPrtColor.ParticleId;
                proppart.Add(ParticleProperties.COLOR);
                sizespart.Add(RpPrtStdEmitterPrtColor.ParticlePropSize);
            }
            if (TextureCoordinates != null)
            {
                idemit |= RpPrtStdEmitterPrtTexCoords.EmitterId;
                propemit.Add(EmitterProperties.PRTTEXCOORDS);
                sizesemit.Add(RpPrtStdEmitterPrtTexCoords.PropSize);
                idpart |= RpPrtStdEmitterPrtTexCoords.ParticleId;
                proppart.Add(ParticleProperties.TEXCOORDS);
                sizespart.Add(RpPrtStdEmitterPrtTexCoords.ParticlePropSize);
            }
            if (Matrix != null)
            {
                idemit |= RpPrtStdEmitterPrtMatrix.EmitterId;
                propemit.Add(EmitterProperties.PRTMATRIX);
                sizesemit.Add(RpPrtStdEmitterPrtMatrix.PropSize);
                idpart |= RpPrtStdEmitterPrtMatrix.ParticleId;
                proppart.Add(ParticleProperties.MATRIX);
                sizespart.Add(RpPrtStdEmitterPrtMatrix.ParticlePropSize);
            }
            if (Tank != null)
            {
                idemit |= RpPrtStdEmitterPTank.EmitterId;
                propemit.Add(EmitterProperties.PTANK);
                sizesemit.Add(RpPrtStdEmitterPTank.PropSize);
            }
            if (ParticleSize != null)
            {
                idemit |= RpPrtStdEmitterPrtSize.EmitterId;
                propemit.Add(EmitterProperties.PRTSIZE);
                sizesemit.Add(RpPrtStdEmitterPrtSize.PropSize);
                idpart |= RpPrtStdEmitterPrtSize.ParticleId;
                proppart.Add(ParticleProperties.SIZE);
                sizespart.Add(RpPrtStdEmitterPrtSize.ParticlePropSize);
            }
            if (Rotate != null)
            {
                idemit |= RpPrtStdEmitterPrt2DRotate.EmitterId;
                propemit.Add(EmitterProperties.PRT2DROTATE);
                sizesemit.Add(RpPrtStdEmitterPrt2DRotate.PropSize);
                idpart |= RpPrtStdEmitterPrt2DRotate.ParticleId;
                proppart.Add(ParticleProperties.PRT2DROTATE);
                sizespart.Add(RpPrtStdEmitterPrt2DRotate.ParticlePropSize);
            }
            if (AdvEmittingEmitter != null)
            {
                idemit |= RpPrtAdvEmtPrtEmt.EmitterId;
                propemit.Add(EmitterProperties.ADVPROPERTYCODEEMITTERPRTEMITTER);
                sizesemit.Add(RpPrtAdvEmtPrtEmt.PropSize);
                idpart |= RpPrtAdvEmtPrtEmt.ParticleId;
                proppart.Add(ParticleProperties.ADVPROPERTYCODEPARTICLEEMITTER);
                sizespart.Add(RpPrtAdvEmtPrtEmt.ParticlePropSize);
            }
            if (AdvMultiColor != null)
            {
                idemit |= RpPrtAdvEmtPrtMultiColor.EmitterId;
                propemit.Add(EmitterProperties.ADVPROPERTYCODEEMITTERPRTMULTICOLOR);
                sizesemit.Add(AdvMultiColor.PropSize);
                idpart |= RpPrtAdvEmtPrtMultiColor.ParticleId;
                proppart.Add(ParticleProperties.ADVPROPERTYCODEPARTICLEMULTICOLOR);
                sizespart.Add(AdvMultiColor.ParticlePropSize);
            }
            if (AdvMultiTexCoords != null)
            {
                idemit |= RpPrtAdvEmtPrtMultiTexCoords.EmitterId;
                propemit.Add(EmitterProperties.ADVPROPERTYCODEEMITTERPRTMULTITEXCOORDS);
                sizesemit.Add(AdvMultiTexCoords.PropSize);
                idpart |= RpPrtAdvEmtPrtMultiTexCoords.ParticleId;
                proppart.Add(ParticleProperties.ADVPROPERTYCODEPARTICLEMULTITEXCOORDS);
                sizespart.Add(AdvMultiTexCoords.ParticlePropSize);
            }
            if (AdvMultiTexCoordsStep != null)
            {
                idemit |= RpPrtAdvEmtPrtMultiTexCoords.EmitterIdSteps;
                propemit.Add(EmitterProperties.ADVPROPERTYCODEEMITTERPRTMULTITEXCOORDSSTEP);
                sizesemit.Add(AdvMultiTexCoordsStep.PropSize);
                idpart |= RpPrtAdvEmtPrtMultiTexCoords.ParticleIdSteps;
                proppart.Add(ParticleProperties.ADVPROPERTYCODEPARTICLEMULTITEXCOORDSSTEP);
                sizespart.Add(AdvMultiTexCoordsStep.ParticlePropSizeStep);
            }
            if (AdvMultiSize != null)
            {
                idemit |= RpPrtAdvEmtPrtMultiSize.EmitterId;
                propemit.Add(EmitterProperties.ADVPROPERTYCODEEMITTERPRTMULTISIZE);
                sizesemit.Add(AdvMultiSize.PropSize);
                idpart |= RpPrtAdvEmtPrtMultiSize.ParticleId;
                proppart.Add(ParticleProperties.ADVPROPERTYCODEPARTICLEMULTISIZE);
                sizespart.Add(AdvMultiSize.ParticlePropSize);
            }
            if (AdvPointList != null)
            {
                idemit |= RpPrtAdvEmtPointList.EmitterId;
                propemit.Add(EmitterProperties.ADVPROPERTYCODEEMITTERPOINTLIST);
                sizesemit.Add(AdvPointList.PropSize);
            }
            if (AdvCircle != null)
            {
                idemit |= RpPrtAdvEmtCircle.EmitterId;
                propemit.Add(EmitterProperties.ADVPROPERTYCODEEMITTERCIRCLE);
                sizesemit.Add(RpPrtAdvEmtCircle.PropSize);
            }
            if (AdvSphere != null)
            {
                idemit |= RpPrtAdvEmtSphere.EmitterId;
                propemit.Add(EmitterProperties.ADVPROPERTYCODEEMITTERSPHERE);
                sizesemit.Add(RpPrtAdvEmtSphere.PropSize);
            }
            EmitterProps = new()
            {
                Id = EmitterClass.PropertyId,
                Ids = [.. propemit],
                Stride = [.. sizesemit],
            };
            ParticleProps = new()
            {
                Id = ParticleClass.PropertyId,
                Ids = [.. proppart],
                Stride = [.. sizespart],
            };
            EmitterClassId = idemit;
            EmitterClass.Id = idemit;
            ParticleClassId = idpart;
            ParticleClass.Id = idpart;
        }

        internal int GetSize(int flags)
        {
            int r = sizeof(int) * 5;
            r += ParticleProps.SizeH + EmitterProps.SizeH + RpPrtStdParticleClass.SizeH + RpPrtStdEmitterClass.SizeH;
            if (EmitterStandard != null)
                r += EmitterStandard.GetSize(flags);
            if (Color != null)
                r += RpPrtStdEmitterPrtColor.Size;
            if (TextureCoordinates != null)
                r += RpPrtStdEmitterPrtTexCoords.Size;
            if (Matrix != null)
                r += RpPrtStdEmitterPrtMatrix.Size;
            if (ParticleSize != null)
                r += RpPrtStdEmitterPrtSize.Size;
            if (Rotate != null)
                r += RpPrtStdEmitterPrt2DRotate.Size;
            if (Tank != null)
                r += RpPrtStdEmitterPTank.Size;
            if (AdvPointList != null)
                r += AdvPointList.Size;
            if (AdvCircle != null)
                r += RpPrtAdvEmtCircle.Size;
            if (AdvSphere != null)
                r += RpPrtAdvEmtSphere.Size;
            if (AdvEmittingEmitter != null)
                r += RpPrtAdvEmtPrtEmt.Size;
            if (AdvMultiColor != null)
                r += AdvMultiColor.Size;
            if (AdvMultiTexCoords != null)
                r += AdvMultiTexCoords.Size;
            if (AdvMultiSize != null)
                r += AdvMultiSize.Size;
            if (AdvMultiTexCoordsStep != null)
                r += AdvMultiTexCoordsStep.Size;
            return r;
        }

        internal static RpPrtStdEmitter Read(BinaryReader s, int flags, bool convertRad)
        {
            RpPrtStdEmitter r = new()
            {
                EmitterClassId = s.ReadInt32(),
                EmitterFlags = s.ReadInt32(),
                ParticleClassId = s.ReadInt32(),
                MaxParticlesPerBatch = s.ReadInt32(),
            };
            if (s.ReadInt32() == 0)
                throw new IOException("non inline particle class not supported");
            r.ParticleProps = RpPrtStdPropertyTable<ParticleProperties>.Read(s, true);
            r.EmitterProps = RpPrtStdPropertyTable<EmitterProperties>.Read(s, true);
            r.ParticleClass = RpPrtStdParticleClass.Read(s, true);
            r.EmitterClass = RpPrtStdEmitterClass.Read(s, true);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.STANDARD))
                r.EmitterStandard = RpPrtStdEmitterStandard.Read(s, flags, convertRad);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.PRTCOLOR))
                r.Color = RpPrtStdEmitterPrtColor.Read(s);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.PRTTEXCOORDS))
                r.TextureCoordinates = RpPrtStdEmitterPrtTexCoords.Read(s);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.PRTMATRIX))
                r.Matrix = RpPrtStdEmitterPrtMatrix.Read(s);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.PRTSIZE))
                r.ParticleSize = RpPrtStdEmitterPrtSize.Read(s);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.PRT2DROTATE))
                r.Rotate = RpPrtStdEmitterPrt2DRotate.Read(s, convertRad);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.PTANK))
                r.Tank = RpPrtStdEmitterPTank.Read(s);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERPOINTLIST))
                r.AdvPointList = RpPrtAdvEmtPointList.Read(s);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERCIRCLE))
                r.AdvCircle = RpPrtAdvEmtCircle.Read(s, convertRad);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERSPHERE))
                r.AdvSphere = RpPrtAdvEmtSphere.Read(s);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERPRTEMITTER))
                r.AdvEmittingEmitter = RpPrtAdvEmtPrtEmt.Read(s);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERPRTMULTICOLOR))
                r.AdvMultiColor = RpPrtAdvEmtPrtMultiColor.Read(s);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERPRTMULTITEXCOORDS))
                r.AdvMultiTexCoords = RpPrtAdvEmtPrtMultiTexCoords.Read(s);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERPRTMULTISIZE))
                r.AdvMultiSize = RpPrtAdvEmtPrtMultiSize.Read(s);
            if (r.EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERPRTMULTITEXCOORDSSTEP))
                r.AdvMultiTexCoordsStep = RpPrtAdvEmtPrtMultiTexCoords.Read(s);
            return r;
        }

        internal void Write(BinaryWriter s, int flags, bool convertRad, uint versionNum, uint buildNum)
        {
            s.Write(EmitterClassId);
            s.Write(EmitterFlags);
            s.Write(ParticleClassId);
            s.Write(MaxParticlesPerBatch);
            s.Write(1);
            ParticleProps.Write(s, true, versionNum, buildNum);
            EmitterProps.Write(s, true, versionNum, buildNum);
            ParticleClass.Write(s, true, versionNum, buildNum);
            EmitterClass.Write(s, true, versionNum, buildNum);
            if (EmitterProps.Ids.Contains(EmitterProperties.STANDARD))
            {
                if (EmitterStandard == null)
                    throw new IOException("EmitterStandard mismatch");
                EmitterStandard.Write(s, flags, convertRad, versionNum, buildNum);
            }
            if (EmitterProps.Ids.Contains(EmitterProperties.PRTCOLOR))
            {
                if (Color == null)
                    throw new IOException("Color mismatch");
                Color.Write(s);
            }
            if (EmitterProps.Ids.Contains(EmitterProperties.PRTTEXCOORDS))
            {
                if (TextureCoordinates == null)
                    throw new IOException("TextureCoordinates mismatch");
                TextureCoordinates.Write(s);
            }
            if (EmitterProps.Ids.Contains(EmitterProperties.PRTMATRIX))
            {
                if (Matrix == null)
                    throw new IOException("Matrix mismatch");
                Matrix.Write(s, versionNum, buildNum);
            }
            if (EmitterProps.Ids.Contains(EmitterProperties.PRTSIZE))
            {
                if (ParticleSize == null)
                    throw new IOException("ParticleSize mismatch");
                ParticleSize.Write(s);
            }
            if (EmitterProps.Ids.Contains(EmitterProperties.PRT2DROTATE))
            {
                if (Rotate == null)
                    throw new IOException("Rotate mismatch");
                Rotate.Write(s, convertRad);
            }
            if (EmitterProps.Ids.Contains(EmitterProperties.PTANK))
            {
                if (Tank == null)
                    throw new IOException("Tank mismatch");
                Tank.Write(s);
            }
            if (EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERPOINTLIST))
            {
                if (AdvPointList == null)
                    throw new IOException("Ex_Fog mismatch");
                AdvPointList.Write(s);
            }
            if (EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERCIRCLE))
            {
                if (AdvCircle == null)
                    throw new IOException("Ex_Circular mismatch");
                AdvCircle.Write(s, convertRad);
            }
            if (EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERSPHERE))
            {
                if (AdvSphere == null)
                    throw new IOException("Unknown1000008 mismatch");
                AdvSphere.Write(s);
            }
            if (EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERPRTEMITTER))
            {
                if (AdvEmittingEmitter == null)
                    throw new IOException("Unknown1000001 mismatch");
                AdvEmittingEmitter.Write(s);
            }
            if (EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERPRTMULTICOLOR))
            {
                if (AdvMultiColor == null)
                    throw new IOException("Unknown1000002 mismatch");
                AdvMultiColor.Write(s);
            }
            if (EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERPRTMULTITEXCOORDS))
            {
                if (AdvMultiTexCoords == null)
                    throw new IOException("Unknown1000003 mismatch");
                AdvMultiTexCoords.Write(s);
            }
            if (EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERPRTMULTISIZE))
            {
                if (AdvMultiSize == null)
                    throw new IOException("Unknown1000005 mismatch");
                AdvMultiSize.Write(s);
            }
            if (EmitterProps.Ids.Contains(EmitterProperties.ADVPROPERTYCODEEMITTERPRTMULTITEXCOORDSSTEP))
            {
                if (AdvMultiTexCoordsStep == null)
                    throw new IOException("Unknown1000004 mismatch");
                AdvMultiTexCoordsStep.Write(s);
            }
        }
    }

    public enum EmitterProperties : int
    {
        EMITTER = 0,
        STANDARD = 1,
        PRTCOLOR = 2,
        PRTTEXCOORDS = 3,
        PRT2DROTATE = 4,
        PRTSIZE = 5,
        PTANK = 6,
        PRTVELOCITY = 7,
        PRTMATRIX = 8,

        ADVPROPERTYCODEEMITTERPRTEMITTER = 0x1000001,
        ADVPROPERTYCODEEMITTERPRTMULTICOLOR = 0x1000002,
        ADVPROPERTYCODEEMITTERPRTMULTITEXCOORDS = 0x1000003,
        ADVPROPERTYCODEEMITTERPRTMULTITEXCOORDSSTEP = 0x1000004,
        ADVPROPERTYCODEEMITTERPRTMULTISIZE = 0x1000005,
        ADVPROPERTYCODEEMITTERPOINTLIST = 0x1000006,
        ADVPROPERTYCODEEMITTERCIRCLE = 0x1000007,
        ADVPROPERTYCODEEMITTERSPHERE = 0x1000008,
    };
    public enum ParticleProperties : int
    {
        STANDARD = 0,
        POSITION = 1,
        COLOR = 2,
        TEXCOORDS = 3,
        PRT2DROTATE = 4,
        SIZE = 5,
        VELOCITY = 6,
        MATRIX = 7,

        ADVPROPERTYCODEPARTICLECHAIN = 0x1000000,
        ADVPROPERTYCODEPARTICLEEMITTER = 0x1000001,
        ADVPROPERTYCODEPARTICLEMULTICOLOR = 0x1000002,
        ADVPROPERTYCODEPARTICLEMULTITEXCOORDS = 0x1000003,
        ADVPROPERTYCODEPARTICLEMULTITEXCOORDSSTEP = 0x1000004,
        ADVPROPERTYCODEPARTICLEMULTISIZE = 0x1000005,
    };
    internal class RpPrtStdPropertyTable<Properties> where Properties : Enum
    {

        internal int Id = 0;
        internal Properties[] Ids = [];
        internal int[] Stride = [];


        internal int Size => sizeof(int) * 2 + Ids.Length * 2 * sizeof(int);
        internal int SizeH => Size + ChunkHeader.Size;

        internal static RpPrtStdPropertyTable<Properties> Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.PRTSTDGLOBALDATA);
            RpPrtStdPropertyTable<Properties> r = new()
            {
                Id = s.ReadInt32(),
            };
            int nProps = s.ReadInt32();
            r.Ids = new Properties[nProps];
            for (int i = 0; i < nProps; ++i)
                r.Ids[i] = (Properties)(object)s.ReadInt32();
            r.Stride = new int[nProps];
            for (int i = 0; i < nProps; ++i)
                r.Stride[i] = s.ReadInt32();
            return r;
        }

        internal void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.PRTSTDGLOBALDATA,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            if (Ids.Length != Stride.Length)
                throw new IOException("RpPrtStdPropertyTable ids and data missmatch");
            s.Write(Id);
            s.Write(Ids.Length);
            foreach (Properties i in Ids)
                s.Write((int)(object)i);
            foreach (int i in Stride)
                s.Write(i);
        }
    }

    internal class RpPrtStdParticleClass
    {
        internal int Id = 0;
        internal int PropertyId = 0;

        internal const int Size = sizeof(int) * 2;
        internal const int SizeH = Size + ChunkHeader.Size;

        internal static RpPrtStdParticleClass Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.PRTSTDGLOBALDATA);
            RpPrtStdParticleClass r = new()
            {
                Id = s.ReadInt32(),
                PropertyId = s.ReadInt32(),
            };
            return r;
        }

        internal void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.PRTSTDGLOBALDATA,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            s.Write(Id);
            s.Write(PropertyId);
        }
    }

    internal class RpPrtStdEmitterClass
    {
        internal int Id = 0;
        internal int PropertyId = 0;

        internal const int Size = sizeof(int) * 2;
        internal const int SizeH = Size + ChunkHeader.Size;

        internal static RpPrtStdEmitterClass Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.PRTSTDGLOBALDATA);
            RpPrtStdEmitterClass r = new()
            {
                Id = s.ReadInt32(),
                PropertyId = s.ReadInt32(),
            };
            return r;
        }

        internal void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.PRTSTDGLOBALDATA,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            s.Write(Id);
            s.Write(PropertyId);
        }
    }

    internal class RpPrtStdEmitterStandard
    {
        public int Seed = 0;
        public required int MaxParticles;
        public required Vec3 Force;
        public required Vec3 EmitterPosition;
        public required Vec3 EmitterSize;
        public required float TimeBetweenEmissions;
        public required float TimeBetweenEmissionsRandom;
        public required int NumParticlesPerEmission;
        public required int NumParticlesPerEmissionRandom;
        public required float InitialVelocity;
        public required float InitialVelocityRandom;
        public required float ParticleLife;
        public required float ParticleLifeRandom;
        public required Vec3 InitialDirection;
        public required Vec3 InitialDirectionRandom;
        public required Vec2 ParticleSize;
        public int ParticleSize_SeriMisstake = 0;
        public required RGBA Color = new();
        [JsonRequired]
        public Vec2[] TextureCoordinates = new Vec2[4];
        public required Texture? ParticleTexture;
        [JsonRequired]
        public float ParticleRotation = -1;

        internal const int EmitterId = 0x00000001;
        internal const int EmitterPropSize = 172;
        internal const int ParticleId = 0x00000001;
        internal const int ParticlePropSize = 16;

        internal int GetSize(int flags)
        {
            int r = sizeof(int) * 10;
            r += Vec3.Size * 6;
            r += RGBA.Size;
            r += Vec2.Size * TextureCoordinates.Length;
            r += Texture.OptTextureSize(ParticleTexture);
            if (flags >= 3)
                r += sizeof(float);
            return r;
        }

        internal static RpPrtStdEmitterStandard Read(BinaryReader s, int flags, bool convertRad)
        {
            RpPrtStdEmitterStandard r = new()
            {
                Seed = s.ReadInt32(),
                MaxParticles = s.ReadInt32(),
                Force = Vec3.Read(s),
                EmitterPosition = Vec3.Read(s),
                EmitterSize = Vec3.Read(s),
                TimeBetweenEmissions = s.ReadSingle(),
                TimeBetweenEmissionsRandom = s.ReadSingle(),
                NumParticlesPerEmission = s.ReadInt32(),
                NumParticlesPerEmissionRandom = s.ReadInt32(),
                InitialVelocity = s.ReadSingle(),
                InitialVelocityRandom = s.ReadSingle(),
                ParticleLife = s.ReadSingle(),
                ParticleLifeRandom = s.ReadSingle(),
                InitialDirection = Vec3.Read(s),
                InitialDirectionRandom = Vec3.Read(s),
                ParticleSize = Vec2.Read(s),
                ParticleSize_SeriMisstake = s.ReadInt32(),
                Color = RGBA.Read(s),
                ParticleTexture = null,
            };
            for (int i = 0; i < 4; ++i)
                r.TextureCoordinates[i] = Vec2.Read(s);
            r.ParticleTexture = Texture.ReadOptText(s);
            if (flags >= 3)
                r.ParticleRotation = s.ReadSingle().RadToDegOpt(convertRad);
            return r;
        }

        internal void Write(BinaryWriter s, int flags, bool convertRad, uint versionNum, uint buildNum)
        {
            s.Write(Seed);
            s.Write(MaxParticles);
            Force.Write(s);
            EmitterPosition.Write(s);
            EmitterSize.Write(s);
            s.Write(TimeBetweenEmissions);
            s.Write(TimeBetweenEmissionsRandom);
            s.Write(NumParticlesPerEmission);
            s.Write(NumParticlesPerEmissionRandom);
            s.Write(InitialVelocity);
            s.Write(InitialVelocityRandom);
            s.Write(ParticleLife);
            s.Write(ParticleLifeRandom);
            InitialDirection.Write(s);
            InitialDirectionRandom.Write(s);
            ParticleSize.Write(s);
            s.Write(ParticleSize_SeriMisstake);
            Color.Write(s);
            for (int i = 0; i < 4; ++i)
                TextureCoordinates[i].Write(s);
            Texture.WriteOptTexture(s, ref ParticleTexture, versionNum, buildNum);
            if (flags >= 3)
                s.Write(ParticleRotation.DegToRadOpt(convertRad));
        }
    }

    internal class RpPrtStdEmitterPrtColor
    {
        public required RGBAF StartColor;
        public required RGBAF StartColorRandom;
        public required RGBAF EndColor;
        public required RGBAF EndColorRandom;

        internal const int EmitterId = 0x00000002;
        internal const int PropSize = 64;
        internal const int ParticleId = 0x00000004;
        internal const int ParticlePropSize = 32;

        internal const int Size = RGBAF.Size * 4;

        internal static RpPrtStdEmitterPrtColor Read(BinaryReader s)
        {
            return new()
            {
                StartColor = RGBAF.Read(s),
                StartColorRandom = RGBAF.Read(s),
                EndColor = RGBAF.Read(s),
                EndColorRandom = RGBAF.Read(s),
            };
        }

        internal void Write(BinaryWriter s)
        {
            StartColor.Write(s);
            StartColorRandom.Write(s);
            EndColor.Write(s);
            EndColorRandom.Write(s);
        }
    }

    internal class RpPrtStdEmitterPrtTexCoords
    {
        public required Vec2 StartUV0;
        public required Vec2 StartUV0Random;
        public required Vec2 EndUV0;
        public required Vec2 EndUV0Random;
        public required Vec2 StartUV1;
        public required Vec2 StartUV1Random;
        public required Vec2 EndUV1;
        public required Vec2 EndUV1Random;

        internal const int EmitterId = 0x00000004;
        internal const int PropSize = 64;
        internal const int ParticleId = 0x00000008;
        internal const int ParticlePropSize = 32;

        internal const int Size = Vec2.Size * 8;

        internal static RpPrtStdEmitterPrtTexCoords Read(BinaryReader s)
        {
            return new()
            {
                StartUV0 = Vec2.Read(s),
                StartUV0Random = Vec2.Read(s),
                EndUV0 = Vec2.Read(s),
                EndUV0Random = Vec2.Read(s),
                StartUV1 = Vec2.Read(s),
                StartUV1Random = Vec2.Read(s),
                EndUV1 = Vec2.Read(s),
                EndUV1Random = Vec2.Read(s),
            };
        }

        internal void Write(BinaryWriter s)
        {
            StartUV0.Write(s);
            StartUV0Random.Write(s);
            EndUV0.Write(s);
            EndUV0Random.Write(s);
            StartUV1.Write(s);
            StartUV1Random.Write(s);
            EndUV1.Write(s);
            EndUV1Random.Write(s);
        }
    }

    internal class RpPrtStdEmitterPrtMatrix
    {
        public required RwMatrix Matrix;
        public required Vec3 LookAt;
        public required Vec3 LookAtRandom;
        public required Vec3 Up;
        public required Vec3 UpRandom;
        public required int Flags;

        internal const int EmitterId = 0x00000040;
        internal const int PropSize = 116;
        internal const int ParticleId = 0x00000080;
        internal const int ParticlePropSize = 0;

        internal const int Size = RwMatrix.SizeH + Vec3.Size * 4 + sizeof(int);

        internal static RpPrtStdEmitterPrtMatrix Read(BinaryReader s)
        {
            return new()
            {
                Matrix = RwMatrix.Read(s, true),
                LookAt = Vec3.Read(s),
                LookAtRandom = Vec3.Read(s),
                Up = Vec3.Read(s),
                UpRandom = Vec3.Read(s),
                Flags = s.ReadInt32(),
            };
        }

        internal void Write(BinaryWriter s, uint versionNum, uint buildNum)
        {
            Matrix.Write(s, true, versionNum, buildNum);
            LookAt.Write(s);
            LookAtRandom.Write(s);
            Up.Write(s);
            UpRandom.Write(s);
            s.Write(Flags);
        }
    }

    internal class RpPrtStdEmitterPrtSize
    {
        public required Vec2 StartSize;
        public required Vec2 StartSizeRandom;
        public required Vec2 EndSize;
        public required Vec2 EndSizeRandom;

        internal const int EmitterId = 0x00000010;
        internal const int PropSize = 32;
        internal const int ParticleId = 0x00000020;
        internal const int ParticlePropSize = 32;

        internal const int Size = Vec2.Size * 4;

        internal static RpPrtStdEmitterPrtSize Read(BinaryReader s)
        {
            return new()
            {
                StartSize = Vec2.Read(s),
                StartSizeRandom = Vec2.Read(s),
                EndSize = Vec2.Read(s),
                EndSizeRandom = Vec2.Read(s),
            };
        }

        internal void Write(BinaryWriter s)
        {
            StartSize.Write(s);
            StartSizeRandom.Write(s);
            EndSize.Write(s);
            EndSizeRandom.Write(s);
        }
    }

    internal class RpPrtStdEmitterPrt2DRotate
    {
        public required float StartRotate;
        public required float StartRotateRandom;
        public required float EndRotate;
        public required float EndRotateRandom;

        internal const int EmitterId = 0x00000008;
        internal const int PropSize = 16;
        internal const int ParticleId = 0x00000010;
        internal const int ParticlePropSize = 8;

        internal const int Size = sizeof(float) * 4;

        internal static RpPrtStdEmitterPrt2DRotate Read(BinaryReader s, bool convertRad)
        {
            return new()
            {
                StartRotate = s.ReadSingle().RadToDegOpt(convertRad),
                StartRotateRandom = s.ReadSingle().RadToDegOpt(convertRad),
                EndRotate = s.ReadSingle().RadToDegOpt(convertRad),
                EndRotateRandom = s.ReadSingle().RadToDegOpt(convertRad),
            };
        }

        internal void Write(BinaryWriter s, bool convertRad)
        {
            s.Write(StartRotate.DegToRadOpt(convertRad));
            s.Write(StartRotateRandom.DegToRadOpt(convertRad));
            s.Write(EndRotate.DegToRadOpt(convertRad));
            s.Write(EndRotateRandom.DegToRadOpt(convertRad));
        }
    }

    internal class RpPrtStdEmitterPTank
    {
        [Flags]
        public enum RpPTankDataFlags : int
        {
            NONE = 0x00000000,
            POSITION = 0x00000001,
            COLOR = 0x00000002,
            SIZE = 0x00000004,
            MATRIX = 0x00000008,
            NORMAL = 0x00000010,
            F2DROTATE = 0x00000020,
            VTXCOLOR = 0x00000040,
            VTX2TEXCOORDS = 0x00000080,
            VTX4TEXCOORDS = 0x00000100,
            CNSMATRIX = 0x00008000,
            CNS2DROTATE = 0x00020000,
            CNSVTXCOLOR = 0x00040000,
            CNSVTX2TEXCOORDS = 0x00080000,
            CNSVTX4TEXCOORDS = 0x00100000,
            USECENTER = 0x01000000,
            ARRAY = 0x10000000,
            STRUCTURE = 0x20000000,
        };


        public required RpPTankDataFlags UpdateFlags;
        public required RpPTankDataFlags EmitterFlags;
        public required int SourceBlend;
        public required int DestinationBlend;
        public required bool VertexAlphaBlending;

        internal const int EmitterId = 0x00000020;
        internal const int PropSize = 52;

        internal const int Size = sizeof(int) * 5;

        internal static RpPrtStdEmitterPTank Read(BinaryReader s)
        {
            return new()
            {
                UpdateFlags = (RpPTankDataFlags)s.ReadInt32(),
                EmitterFlags = (RpPTankDataFlags)s.ReadInt32(),
                SourceBlend = s.ReadInt32(),
                DestinationBlend = s.ReadInt32(),
                VertexAlphaBlending = s.ReadInt32() != 0,
            };
        }

        internal void Write(BinaryWriter s)
        {
            s.Write((int)UpdateFlags);
            s.Write((int)EmitterFlags);
            s.Write(SourceBlend);
            s.Write(DestinationBlend);
            s.Write(VertexAlphaBlending ? 1 : 0);
        }
    }

    internal class RpPrtAdvEmtPointList
    {
        public required bool UseDirection;
        public required bool Random;
        public Vec3[]? PointList;
        public Vec3[]? DirectionList;

        internal const int EmitterId = 0x00010000;
        internal int PropSize => 24 + ((PointList?.Length ?? 0) + (DirectionList?.Length ?? 0)) * 3 * sizeof(float);

        internal int Size => sizeof(int) * 5 + (PointList?.Length ?? 0) * Vec3.Size + (DirectionList?.Length ?? 0) * Vec3.Size;

        internal static RpPrtAdvEmtPointList Read(BinaryReader s)
        {
            int nElems = s.ReadInt32();
            RpPrtAdvEmtPointList r = new()
            {
                UseDirection = s.ReadInt32() != 0,
                Random = s.ReadInt32() != 0,
            };
            if (s.ReadInt32() != 0)
            {
                r.PointList = new Vec3[nElems];
                for (int i = 0; i < nElems; ++i)
                    r.PointList[i] = Vec3.Read(s);
            }
            if (s.ReadInt32() != 0)
            {
                r.DirectionList = new Vec3[nElems];
                for (int i = 0; i < nElems; ++i)
                    r.DirectionList[i] = Vec3.Read(s);
            }
            return r;
        }

        internal void Write(BinaryWriter s)
        {
            int nelems = PointList?.Length ?? 0;
            if (PointList == null && DirectionList != null)
                nelems = DirectionList.Length;
            if (PointList != null && DirectionList != null && PointList.Length != DirectionList.Length)
                throw new IOException("fogemitter length missmatch");
            s.Write(nelems);
            s.Write(UseDirection ? 1 : 0);
            s.Write(Random ? 1 : 0);
            if (PointList != null)
            {
                s.Write(1);
                foreach (Vec3 v in PointList)
                    v.Write(s);
            }
            else
            {
                s.Write(0);
            }
            if (DirectionList != null)
            {
                s.Write(1);
                foreach (Vec3 v in DirectionList)
                    v.Write(s);
            }
            else
            {
                s.Write(0);
            }
        }
    }

    internal class RpPrtAdvEmtCircle
    {
        public required float Radius;
        public required float RadiusGap;
        public required float Height;
        public required bool UseCircleEmission;
        public required float DirRotation;

        internal const int EmitterId = 0x00020000;
        internal const int PropSize = 20;

        internal const int Size = sizeof(int) * 5;

        internal static RpPrtAdvEmtCircle Read(BinaryReader s, bool convertRad)
        {
            return new()
            {
                Radius = s.ReadSingle(),
                RadiusGap = s.ReadSingle(),
                Height = s.ReadSingle(),
                UseCircleEmission = s.ReadInt32() != 0,
                DirRotation = s.ReadSingle().RadToDegOpt(convertRad),
            };
        }

        internal void Write(BinaryWriter s, bool convertRad)
        {
            s.Write(Radius);
            s.Write(RadiusGap);
            s.Write(Height);
            s.Write(UseCircleEmission ? 1 : 0);
            s.Write(DirRotation.DegToRadOpt(convertRad));
        }
    }

    internal class RpPrtAdvEmtSphere
    {
        public required float Radius;
        public required float RadiusGap;
        public required bool UseSphereEmission;

        internal const int EmitterId = 0x00030000;
        internal const int PropSize = 12;

        internal const int Size = sizeof(int) * 3;

        internal static RpPrtAdvEmtSphere Read(BinaryReader s)
        {
            return new()
            {
                Radius = s.ReadSingle(),
                RadiusGap = s.ReadSingle(),
                UseSphereEmission = s.ReadInt32() != 0,
            };
        }

        internal void Write(BinaryWriter s)
        {
            s.Write(Radius);
            s.Write(RadiusGap);
            s.Write(UseSphereEmission ? 1 : 0);
        }
    }

    internal class RpPrtAdvEmtPrtEmt
    {
        public required float Time;
        public required float TimeBias;
        public required float TimeGap;
        public required float TimeGapBias;

        internal const int EmitterId = 0x00000100;
        internal const int PropSize = 40;
        internal const int ParticleId = 0x00000100;
        internal const int ParticlePropSize = 8;

        internal const int Size = sizeof(int) * 4;

        internal static RpPrtAdvEmtPrtEmt Read(BinaryReader s)
        {
            return new()
            {
                Time = s.ReadSingle(),
                TimeBias = s.ReadSingle(),
                TimeGap = s.ReadSingle(),
                TimeGapBias = s.ReadSingle(),
            };
        }

        internal void Write(BinaryWriter s)
        {
            s.Write(Time);
            s.Write(TimeBias);
            s.Write(TimeGap);
            s.Write(TimeGapBias);
        }
    }

    internal class RpPrtAdvEmtPrtMultiColor
    {
        internal struct RpPrtAdvEmtPrtColorItem
        {
            public required float Time;
            public required float TimeBias;
            public required RGBAF MidColor;
            public required RGBAF MidColorBias;

            internal const int Size = sizeof(float) * 2 + RGBAF.Size * 2;

            internal static RpPrtAdvEmtPrtColorItem Read(BinaryReader s)
            {
                RpPrtAdvEmtPrtColorItem r = new()
                {
                    Time = s.ReadSingle(),
                    TimeBias = s.ReadSingle(),
                    MidColor = RGBAF.Read(s),
                    MidColorBias = RGBAF.Read(s),
                };
                return r;
            }

            internal readonly void Write(BinaryWriter s)
            {
                s.Write(Time);
                s.Write(TimeBias);
                MidColor.Write(s);
                MidColorBias.Write(s);
            }
        }

        public required RpPrtAdvEmtPrtColorItem[] List;

        internal const int EmitterId = 0x00000200;
        internal int PropSize => 20 + 40 * List.Length;
        internal const int ParticleId = 0x00000200;
        internal int ParticlePropSize => 4 + 36 * List.Length;

        internal int Size => sizeof(int) + RpPrtAdvEmtPrtColorItem.Size * List.Length;

        internal static RpPrtAdvEmtPrtMultiColor Read(BinaryReader s)
        {
            int nElem = s.ReadInt32();
            RpPrtAdvEmtPrtMultiColor r = new()
            {
                List = new RpPrtAdvEmtPrtColorItem[nElem],
            };
            for (int i = 0; i < r.List.Length; ++i)
                r.List[i] = RpPrtAdvEmtPrtColorItem.Read(s);
            return r;
        }

        internal void Write(BinaryWriter s)
        {
            s.Write(List.Length);
            foreach (RpPrtAdvEmtPrtColorItem f in List)
                f.Write(s);
        }
    }

    internal class RpPrtAdvEmtPrtMultiTexCoords
    {
        internal struct RpPrtAdvEmtPrtTexCoordsItem
        {
            public required float Time;
            public required float TimeBias;
            public required TexCoord MidUV0;
            public required TexCoord MidUV0Bias;
            public required TexCoord MidUV1;
            public required TexCoord MidUV1Bias;

            internal const int Size = sizeof(float) * 2 + TexCoord.Size * 4;

            internal static RpPrtAdvEmtPrtTexCoordsItem Read(BinaryReader s)
            {
                RpPrtAdvEmtPrtTexCoordsItem r = new()
                {
                    Time = s.ReadSingle(),
                    TimeBias = s.ReadSingle(),
                    MidUV0 = TexCoord.Read(s),
                    MidUV0Bias = TexCoord.Read(s),
                    MidUV1 = TexCoord.Read(s),
                    MidUV1Bias = TexCoord.Read(s),
                };
                return r;
            }

            internal readonly void Write(BinaryWriter s)
            {
                s.Write(Time);
                s.Write(TimeBias);
                MidUV0.Write(s);
                MidUV0Bias.Write(s);
                MidUV1.Write(s);
                MidUV1Bias.Write(s);
            }
        }

        public required RpPrtAdvEmtPrtTexCoordsItem[] List;

        internal const int EmitterId = 0x00000400;
        internal const int EmitterIdSteps = 0x00000800;
        internal int PropSize => 28 + 40 * List.Length;
        internal const int ParticleId = 0x00000400;
        internal const int ParticleIdSteps = 0x00000800;
        internal int ParticlePropSize => 4 + 36 * List.Length;
        internal int ParticlePropSizeStep => 4 + 20 * List.Length;

        internal int Size => sizeof(int) + RpPrtAdvEmtPrtTexCoordsItem.Size * List.Length;

        internal static RpPrtAdvEmtPrtMultiTexCoords Read(BinaryReader s)
        {
            int nElem = s.ReadInt32();
            RpPrtAdvEmtPrtMultiTexCoords r = new()
            {
                List = new RpPrtAdvEmtPrtTexCoordsItem[nElem],
            };
            for (int i = 0; i < r.List.Length; ++i)
                r.List[i] = RpPrtAdvEmtPrtTexCoordsItem.Read(s);
            return r;
        }

        internal void Write(BinaryWriter s)
        {
            s.Write(List.Length);
            foreach (RpPrtAdvEmtPrtTexCoordsItem f in List)
                f.Write(s);
        }
    }

    internal class RpPrtAdvEmtPrtMultiSize
    {
        internal struct RpPrtAdvEmtPrtSizeItem
        {
            public required float Time;
            public required float TimeBias;
            public required Vec2 MidSize;
            public required Vec2 MidSizeBias;

            internal const int Size = sizeof(float) * 2 + Vec2.Size * 2;

            internal static RpPrtAdvEmtPrtSizeItem Read(BinaryReader s)
            {
                RpPrtAdvEmtPrtSizeItem r = new()
                {
                    Time = s.ReadSingle(),
                    TimeBias = s.ReadSingle(),
                    MidSize = Vec2.Read(s),
                    MidSizeBias = Vec2.Read(s),
                };
                return r;
            }

            internal readonly void Write(BinaryWriter s)
            {
                s.Write(Time);
                s.Write(TimeBias);
                MidSize.Write(s);
                MidSizeBias.Write(s);
            }
        }

        public required RpPrtAdvEmtPrtSizeItem[] List;

        internal const int EmitterId = 0x00001000;
        internal int PropSize => 20 + 24 * List.Length;
        internal const int ParticleId = 0x00001000;
        internal int ParticlePropSize => 4 + 20 * List.Length;

        internal int Size => sizeof(int) + RpPrtAdvEmtPrtSizeItem.Size * List.Length;

        internal static RpPrtAdvEmtPrtMultiSize Read(BinaryReader s)
        {
            int nElem = s.ReadInt32();
            RpPrtAdvEmtPrtMultiSize r = new()
            {
                List = new RpPrtAdvEmtPrtSizeItem[nElem],
            };
            for (int i = 0; i < r.List.Length; ++i)
                r.List[i] = RpPrtAdvEmtPrtSizeItem.Read(s);
            return r;
        }

        internal void Write(BinaryWriter s)
        {
            s.Write(List.Length);
            foreach (RpPrtAdvEmtPrtSizeItem f in List)
                f.Write(s);
        }
    }
}
