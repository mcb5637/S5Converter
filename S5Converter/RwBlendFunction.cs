using System.Text.Json.Serialization;

namespace S5Converter
{
    [JsonConverter(typeof(EnumJsonConverter<RwBlendFunction>))]
    internal enum RwBlendFunction : int
    {
        rwBLENDNABLEND = 0,
        rwBLENDZERO,            /**<(0,    0,    0,    0   ) */
        rwBLENDONE,             /**<(1,    1,    1,    1   ) */
        rwBLENDSRCCOLOR,        /**<(Rs,   Gs,   Bs,   As  ) */
        rwBLENDINVSRCCOLOR,     /**<(1-Rs, 1-Gs, 1-Bs, 1-As) */
        rwBLENDSRCALPHA,        /**<(As,   As,   As,   As  ) */
        rwBLENDINVSRCALPHA,     /**<(1-As, 1-As, 1-As, 1-As) */
        rwBLENDDESTALPHA,       /**<(Ad,   Ad,   Ad,   Ad  ) */
        rwBLENDINVDESTALPHA,    /**<(1-Ad, 1-Ad, 1-Ad, 1-Ad) */
        rwBLENDDESTCOLOR,       /**<(Rd,   Gd,   Bd,   Ad  ) */
        rwBLENDINVDESTCOLOR,    /**<(1-Rd, 1-Gd, 1-Bd, 1-Ad) */
        rwBLENDSRCALPHASAT,     /**<(f,    f,    f,    1   )  f = min (As, 1-Ad) */
    };
}
