using System;

namespace DACS.Models;

public class DuLieuGiaTocKe
{
    public long Id { get; set; }
    public int HanhTrinhIdHanhTrinh { get; set; }
    public int? IdThietBi { get; set; }

    /// <summary>Gia tốc dọc (theo hướng di chuyển), đơn vị: g</summary>
    public double GiaTocDoc { get; set; }

    /// <summary>Gia tốc ngang (lệch trái/phải), đơn vị: g</summary>
    public double GiaTocNgang { get; set; }

    /// <summary>Gia tốc ngang mượt (accel_lat_smooth_g) cho model AI</summary>
    public double? GiaTocNgangMuot { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;

    public virtual HanhTrinh HanhTrinh { get; set; } = null!;
    public virtual ThietBiGPS? ThietBiGPS { get; set; }
}
