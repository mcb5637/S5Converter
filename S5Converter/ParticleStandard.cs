using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace S5Converter
{
    internal class ParticleStandard
    {
        [JsonInclude]
        public int Flags;
        [JsonInclude]
        public RpPrtStdEmitter[] Emitters = [];

        internal int Size => sizeof(int) + Emitters.Sum(x => x.GetSize(Flags));
        internal int SizeH => Size + ChunkHeader.Size;

        internal static ParticleStandard Read(BinaryReader s, bool header)
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
                r.Emitters[i] = RpPrtStdEmitter.Read(s, r.Flags);
            return r;
        }

        internal void Write(BinaryWriter s, bool header)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.PRTSTDPLUGIN,
                }.Write(s);
            }
            s.Write(Flags << 24 | Emitters.Length);
            foreach (RpPrtStdEmitter e in Emitters)
                e.Write(s, Flags);
        }
    }

    internal class RpPrtStdEmitter // TODO build emitter/particle? props from filled options
    {
        [JsonInclude]
        public int EmitterClassId = 0;
        [JsonInclude]
        public int EmitterFlags = 0;
        [JsonInclude]
        public int ParticleClassId = 0;
        [JsonInclude]
        public int MaxParticlesPerBatch = 0;

        [JsonInclude]
        public RpPrtStdPropertyTable ParticleProps = new();
        [JsonInclude]
        public RpPrtStdPropertyTable EmitterProps = new();
        [JsonInclude]
        public RpPrtStdParticleClass ParticleClass = new();
        [JsonInclude]
        public RpPrtStdEmitterClass EmitterClass = new();

        [JsonInclude]
        public RpPrtStdEmitterStandard? EmitterStandard;
        [JsonInclude]
        public RpPrtStdEmitterPrtColor? Color;
        [JsonInclude]
        public RpPrtStdEmitterPrtTexCoords? TextureCoordinates;
        [JsonInclude]
        public RpPrtStdEmitterPrtMatrix? Matrix;
        [JsonInclude]
        public RpPrtStdEmitterPrtSize? ParticleSize;
        [JsonInclude]
        public RpPrtStdEmitterPrt2DRotate? Rotate;
        [JsonInclude]
        public RpPrtStdEmitterPTank? Tank;
        [JsonInclude]
        public RpPrtAdvEmtPointList? AdvPointList;
        [JsonInclude]
        public RpPrtAdvEmtCircle? AdvCircle;
        [JsonInclude]
        public RpPrtAdvEmtSphere? AdvSphere;
        [JsonInclude]
        public RpPrtAdvEmtPrtEmt? AdvEmittingEmitter;
        [JsonInclude]
        public RpPrtAdvEmtPrtMultiColor? AdvMultiColor;
        [JsonInclude]
        public RpPrtAdvEmtPrtMultiTexCoords? AdvMultiTexCoords;
        [JsonInclude]
        public RpPrtAdvEmtPrtMultiSize? AdvMultiSize;
        [JsonInclude]
        public RpPrtAdvEmtPrtMultiTexCoords? AdvMultiTexCoordsStep;


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

        internal static RpPrtStdEmitter Read(BinaryReader s, int flags)
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
            r.ParticleProps = RpPrtStdPropertyTable.Read(s, true);
            r.EmitterProps = RpPrtStdPropertyTable.Read(s, true);
            r.ParticleClass = RpPrtStdParticleClass.Read(s, true);
            r.EmitterClass = RpPrtStdEmitterClass.Read(s, true);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.STANDARD))
                r.EmitterStandard = RpPrtStdEmitterStandard.Read(s, flags);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.PRTCOLOR))
                r.Color = RpPrtStdEmitterPrtColor.Read(s);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.PRTTEXCOORDS))
                r.TextureCoordinates = RpPrtStdEmitterPrtTexCoords.Read(s);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.PRTMATRIX))
                r.Matrix = RpPrtStdEmitterPrtMatrix.Read(s);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.PRTSIZE))
                r.ParticleSize = RpPrtStdEmitterPrtSize.Read(s);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.PRT2DROTATE))
                r.Rotate = RpPrtStdEmitterPrt2DRotate.Read(s);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.PTANK))
                r.Tank = RpPrtStdEmitterPTank.Read(s);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERPOINTLIST))
                r.AdvPointList = RpPrtAdvEmtPointList.Read(s);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERCIRCLE))
                r.AdvCircle = RpPrtAdvEmtCircle.Read(s);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERSPHERE))
                r.AdvSphere = RpPrtAdvEmtSphere.Read(s);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERPRTEMITTER))
                r.AdvEmittingEmitter = RpPrtAdvEmtPrtEmt.Read(s);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERPRTMULTICOLOR))
                r.AdvMultiColor = RpPrtAdvEmtPrtMultiColor.Read(s);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERPRTMULTITEXCOORDS))
                r.AdvMultiTexCoords = RpPrtAdvEmtPrtMultiTexCoords.Read(s);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERPRTMULTISIZE))
                r.AdvMultiSize = RpPrtAdvEmtPrtMultiSize.Read(s);
            if (r.EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERPRTMULTITEXCOORDSSTEP))
                r.AdvMultiTexCoordsStep = RpPrtAdvEmtPrtMultiTexCoords.Read(s);
            return r;
        }

        internal void Write(BinaryWriter s, int flags)
        {
            s.Write(EmitterClassId);
            s.Write(EmitterFlags);
            s.Write(ParticleClassId);
            s.Write(MaxParticlesPerBatch);
            s.Write(1);
            ParticleProps.Write(s, true);
            EmitterProps.Write(s, true);
            ParticleClass.Write(s, true);
            EmitterClass.Write(s, true);
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.STANDARD))
            {
                if (EmitterStandard == null)
                    throw new IOException("EmitterStandard mismatch");
                EmitterStandard.Write(s, flags);
            }
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.PRTCOLOR))
            {
                if (Color == null)
                    throw new IOException("Color mismatch");
                Color.Write(s);
            }
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.PRTTEXCOORDS))
            {
                if (TextureCoordinates == null)
                    throw new IOException("TextureCoordinates mismatch");
                TextureCoordinates.Write(s);
            }
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.PRTMATRIX))
            {
                if (Matrix == null)
                    throw new IOException("Matrix mismatch");
                Matrix.Write(s);
            }
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.PRTSIZE))
            {
                if (ParticleSize == null)
                    throw new IOException("ParticleSize mismatch");
                ParticleSize.Write(s);
            }
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.PRT2DROTATE))
            {
                if (Rotate == null)
                    throw new IOException("Rotate mismatch");
                Rotate.Write(s);
            }
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.PTANK))
            {
                if (Tank == null)
                    throw new IOException("Tank mismatch");
                Tank.Write(s);
            }
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERPOINTLIST))
            {
                if (AdvPointList == null)
                    throw new IOException("Ex_Fog mismatch");
                AdvPointList.Write(s);
            }
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERCIRCLE))
            {
                if (AdvCircle == null)
                    throw new IOException("Ex_Circular mismatch");
                AdvCircle.Write(s);
            }
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERSPHERE))
            {
                if (AdvSphere == null)
                    throw new IOException("Unknown1000008 mismatch");
                AdvSphere.Write(s);
            }
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERPRTEMITTER))
            {
                if (AdvEmittingEmitter == null)
                    throw new IOException("Unknown1000001 mismatch");
                AdvEmittingEmitter.Write(s);
            }
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERPRTMULTICOLOR))
            {
                if (AdvMultiColor == null)
                    throw new IOException("Unknown1000002 mismatch");
                AdvMultiColor.Write(s);
            }
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERPRTMULTITEXCOORDS))
            {
                if (AdvMultiTexCoords == null)
                    throw new IOException("Unknown1000003 mismatch");
                AdvMultiTexCoords.Write(s);
            }
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERPRTMULTISIZE))
            {
                if (AdvMultiSize == null)
                    throw new IOException("Unknown1000005 mismatch");
                AdvMultiSize.Write(s);
            }
            if (EmitterProps.Ids.Contains(RpPrtStdPropertyTable.Properties.ADVPROPERTYCODEEMITTERPRTMULTITEXCOORDSSTEP))
            {
                if (AdvMultiTexCoordsStep == null)
                    throw new IOException("Unknown1000004 mismatch");
                AdvMultiTexCoordsStep.Write(s);
            }
        }
    }

    internal class RpPrtStdPropertyTable
    {
        public enum Properties : int
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

        [JsonInclude]
        public int Id = 0;
        [JsonInclude]
        public Properties[] Ids = [];
        [JsonInclude]
        public int[] Data = [];


        internal int Size => sizeof(int) * 2 + Ids.Length * 2 * sizeof(int);
        internal int SizeH => Size + ChunkHeader.Size;

        internal static RpPrtStdPropertyTable Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.PRTSTDGLOBALDATA);
            RpPrtStdPropertyTable r = new()
            {
                Id = s.ReadInt32(),
            };
            int nProps = s.ReadInt32();
            r.Ids = new Properties[nProps];
            for (int i = 0; i < nProps; ++i)
                r.Ids[i] = (Properties)s.ReadInt32();
            r.Data = new int[nProps];
            for (int i = 0; i < nProps; ++i)
                r.Data[i] = s.ReadInt32();
            return r;
        }

        internal void Write(BinaryWriter s, bool header)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.PRTSTDGLOBALDATA,
                }.Write(s);
            }
            if (Ids.Length != Data.Length)
                throw new IOException("RpPrtStdPropertyTable ids and data missmatch");
            s.Write(Id);
            s.Write(Ids.Length);
            foreach (Properties i in Ids)
                s.Write((int)i);
            foreach (int i in Data)
                s.Write(i);
        }

        public static bool operator ==(RpPrtStdPropertyTable a, RpPrtStdPropertyTable b)
        {
            if (a.Id != b.Id) return false;
            if (!a.Ids.SequenceEqual(b.Ids)) return false;
            //return true;
            return a.Data.SequenceEqual(b.Data);
        }
        public static bool operator !=(RpPrtStdPropertyTable a, RpPrtStdPropertyTable b)
        {
            return !(a == b);
        }
    }

    internal class RpPrtStdParticleClass
    {
        [JsonInclude]
        public int Id = 0;
        [JsonInclude]
        public int PropertyId = 0;

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

        internal void Write(BinaryWriter s, bool header)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.PRTSTDGLOBALDATA,
                }.Write(s);
            }
            s.Write(Id);
            s.Write(PropertyId);
        }
    }

    internal class RpPrtStdEmitterClass
    {
        [JsonInclude]
        public int Id = 0;
        [JsonInclude]
        public int PropertyId = 0;

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

        internal void Write(BinaryWriter s, bool header)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.PRTSTDGLOBALDATA,
                }.Write(s);
            }
            s.Write(Id);
            s.Write(PropertyId);
        }
    }

    internal class RpPrtStdEmitterStandard
    {
        [JsonInclude]
        public int Seed;
        [JsonInclude]
        public int MaxParticles;
        [JsonInclude]
        public Vec3 Force = new();
        [JsonInclude]
        public Vec3 EmitterPosition = new();
        [JsonInclude]
        public Vec3 EmitterSize = new();
        [JsonInclude]
        public float TimeBetweenEmissions;
        [JsonInclude]
        public float TimeBetweenEmissionsRandom;
        [JsonInclude]
        public int NumParticlesPerEmission;
        [JsonInclude]
        public float NumParticlesPerEmissionRandom;
        [JsonInclude]
        public float InitialVelocity;
        [JsonInclude]
        public float InitialVelocityRandom;
        [JsonInclude]
        public float ParticleLife;
        [JsonInclude]
        public float ParticleLifeRandom;
        [JsonInclude]
        public Vec3 InitialDirection = new();
        [JsonInclude]
        public Vec3 InitialDirectionRandom = new();
        [JsonInclude]
        public Vec3 ParticleSize = new();
        [JsonInclude]
        public RGBA Color = new();
        [JsonInclude]
        public Vec2[] TextureCoordinates = new Vec2[4];
        [JsonInclude]
        public Texture? ParticleTexture;
        [JsonInclude]
        public float ParticleRotation = -1;


        internal int GetSize(int flags)
        {
            int r = sizeof(int) * 10;
            r += Vec3.Size * 6;
            r += RGBA.Size;
            r += Vec2.Size * TextureCoordinates.Length;
            r += Texture.OptTextureSize(ref ParticleTexture);
            if (flags >= 3)
                r += sizeof(float);
            return r;
        }

        internal static RpPrtStdEmitterStandard Read(BinaryReader s, int flags)
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
                ParticleSize = Vec3.Read(s),
                Color = RGBA.Read(s),
            };
            for (int i = 0; i < 4; ++i)
                r.TextureCoordinates[i] = Vec2.Read(s);
            r.ParticleTexture = Texture.ReadOptText(s);
            if (flags >= 3)
                r.ParticleRotation = float.RadiansToDegrees(s.ReadSingle());
            return r;
        }

        internal void Write(BinaryWriter s, int flags)
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
            Color.Write(s);
            for (int i = 0; i < 4; ++i)
                TextureCoordinates[i].Write(s);
            Texture.WriteOptTexture(s, ref ParticleTexture);
            if (flags >= 3)
                s.Write(float.DegreesToRadians(ParticleRotation));
        }
    }

    internal class RpPrtStdEmitterPrtColor
    {
        [JsonInclude]
        public RGBAF StartColor = new();
        [JsonInclude]
        public RGBAF StartColorRandom = new();
        [JsonInclude]
        public RGBAF EndColor = new();
        [JsonInclude]
        public RGBAF EndColorRandom = new();

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
        [JsonInclude]
        public Vec2 StartUV0 = new();
        [JsonInclude]
        public Vec2 StartUV0Random = new();
        [JsonInclude]
        public Vec2 EndUV0 = new();
        [JsonInclude]
        public Vec2 EndUV0Random = new();
        [JsonInclude]
        public Vec2 StartUV1 = new();
        [JsonInclude]
        public Vec2 StartUV1Random = new();
        [JsonInclude]
        public Vec2 EndUV1 = new();
        [JsonInclude]
        public Vec2 EndUV1Random = new();

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
        [JsonInclude]
        public RwMatrix Matrix = new();
        [JsonInclude]
        public Vec3 LookAt = new();
        [JsonInclude]
        public Vec3 LookAtRandom = new();
        [JsonInclude]
        public Vec3 Up = new();
        [JsonInclude]
        public Vec3 UpRandom = new();
        [JsonInclude]
        public int Flags;


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

        internal void Write(BinaryWriter s)
        {
            Matrix.Write(s, true);
            LookAt.Write(s);
            LookAtRandom.Write(s);
            Up.Write(s);
            UpRandom.Write(s);
            s.Write(Flags);
        }
    }

    internal class RpPrtStdEmitterPrtSize
    {
        [JsonInclude]
        public Vec2 StartSize = new();
        [JsonInclude]
        public Vec2 StartSizeRandom = new();
        [JsonInclude]
        public Vec2 EndSize = new();
        [JsonInclude]
        public Vec2 EndSizeRandom = new();

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
        [JsonInclude]
        public float StartRotate;
        [JsonInclude]
        public float StartRotateRandom;
        [JsonInclude]
        public float EndRotate;
        [JsonInclude]
        public float EndRotateRandom;

        internal const int Size = sizeof(float) * 4;

        internal static RpPrtStdEmitterPrt2DRotate Read(BinaryReader s)
        {
            return new()
            {
                StartRotate = float.RadiansToDegrees(s.ReadSingle()),
                StartRotateRandom = float.RadiansToDegrees(s.ReadSingle()),
                EndRotate = float.RadiansToDegrees(s.ReadSingle()),
                EndRotateRandom = float.RadiansToDegrees(s.ReadSingle()),
            };
        }

        internal void Write(BinaryWriter s)
        {
            s.Write(float.DegreesToRadians(StartRotate));
            s.Write(float.DegreesToRadians(StartRotateRandom));
            s.Write(float.DegreesToRadians(EndRotate));
            s.Write(float.DegreesToRadians(EndRotateRandom));
        }
    }

    internal class RpPrtStdEmitterPTank
    {
        [Flags]
        public enum RpPTankDataFlags : int
        {
            NONE = ((int)0x00000000),
            POSITION = ((int)0x00000001),
            COLOR = ((int)0x00000002),
            SIZE = ((int)0x00000004),
            MATRIX = ((int)0x00000008),
            NORMAL = ((int)0x00000010),
            F2DROTATE = ((int)0x00000020),
            VTXCOLOR = ((int)0x00000040),
            VTX2TEXCOORDS = ((int)0x00000080),
            VTX4TEXCOORDS = ((int)0x00000100),
            CNSMATRIX = ((int)0x00008000),
            CNS2DROTATE = ((int)0x00020000),
            CNSVTXCOLOR = ((int)0x00040000),
            CNSVTX2TEXCOORDS = ((int)0x00080000),
            CNSVTX4TEXCOORDS = ((int)0x00100000),
            USECENTER = ((int)0x01000000),
            ARRAY = ((int)0x10000000),
            STRUCTURE = ((int)0x20000000),
        };


        [JsonInclude]
        public RpPTankDataFlags UpdateFlags;
        [JsonInclude]
        public RpPTankDataFlags EmitterFlags;
        [JsonInclude]
        public int SourceBlend;
        [JsonInclude]
        public int DestinationBlend;
        [JsonInclude]
        public bool VertexAlphaBlending;

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
        [JsonInclude]
        public bool UseDirection;
        [JsonInclude]
        public bool Random;
        [JsonInclude]
        public Vec3[]? PointList = [];
        [JsonInclude]
        public Vec3[]? DirectionList = [];


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
        [JsonInclude]
        public float Radius;
        [JsonInclude]
        public float RadiusGap;
        [JsonInclude]
        public float Height;
        [JsonInclude]
        public bool UseCircleEmission;
        [JsonInclude]
        public float DirRotation;

        internal const int Size = sizeof(int) * 5;

        internal static RpPrtAdvEmtCircle Read(BinaryReader s)
        {
            return new()
            {
                Radius = s.ReadSingle(),
                RadiusGap = s.ReadSingle(),
                Height = s.ReadSingle(),
                UseCircleEmission = s.ReadInt32() != 0,
                DirRotation = float.RadiansToDegrees(s.ReadSingle()),
            };
        }

        internal void Write(BinaryWriter s)
        {
            s.Write(Radius);
            s.Write(RadiusGap);
            s.Write(Height);
            s.Write(UseCircleEmission ? 1 : 0);
            s.Write(float.DegreesToRadians(DirRotation));
        }
    }

    internal class RpPrtAdvEmtSphere
    {
        [JsonInclude]
        public float Radius;
        [JsonInclude]
        public float RadiusGap;
        [JsonInclude]
        public bool UseSphereEmission;

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
        [JsonInclude]
        public float Time;
        [JsonInclude]
        public float TimeBias;
        [JsonInclude]
        public float TimeGap;
        [JsonInclude]
        public float TimeGapBias;

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
            [JsonInclude]
            public float Time;
            [JsonInclude]
            public float TimeBias;
            [JsonInclude]
            public RGBAF MidColor;
            [JsonInclude]
            public RGBAF MidColorBias;

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

        [JsonInclude]
        public RpPrtAdvEmtPrtColorItem[] List = [];

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
            [JsonInclude]
            public float Time;
            [JsonInclude]
            public float TimeBias;
            [JsonInclude]
            public TexCoord MidUV0;
            [JsonInclude]
            public TexCoord MidUV0Bias;
            [JsonInclude]
            public TexCoord MidUV1;
            [JsonInclude]
            public TexCoord MidUV1Bias;

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

        [JsonInclude]
        public RpPrtAdvEmtPrtTexCoordsItem[] List = [];

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
            [JsonInclude]
            public float Time;
            [JsonInclude]
            public float TimeBias;
            [JsonInclude]
            public Vec2 MidSize;
            [JsonInclude]
            public Vec2 MidSizeBias;

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

        [JsonInclude]
        public RpPrtAdvEmtPrtSizeItem[] List = [];

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
            s.Write(List.Length / 6);
            foreach (RpPrtAdvEmtPrtSizeItem f in List)
                f.Write(s);
        }
    }
}
