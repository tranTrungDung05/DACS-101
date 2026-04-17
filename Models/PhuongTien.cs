namespace DACS.Models;

public class PhuongTien
{
    public int IdPhuongTien { get; set; }
    public string BienSo { get; set; } = string.Empty;
    public string LoaiPhuongTien { get; set; } = string.Empty;
    public string HangSanXuat { get; set; } = string.Empty;
    public string MauSac { get; set; } = string.Empty;
    public bool TrangThai { get; set; }
    public int IdThietBi { get; set; }
    public virtual ThietBiGPS ThietBiGPS { get; set; } = null!;
    public virtual ICollection<ChiTietHopDong> ChiTietHopDongs { get; set; } = new List<ChiTietHopDong>();
     public virtual ICollection<HanhTrinh> HanhTrinhs { get; set; } = new List<HanhTrinh>();
}