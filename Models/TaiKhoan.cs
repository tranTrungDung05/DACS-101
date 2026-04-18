namespace DACS.Models;

public class TaiKhoan
{
    public int IdTaiKhoan { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhau { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int IdChucVu { get; set; }
    public virtual ChucVu? ChucVu { get; set; }
    public virtual ICollection<HopDong> HopDongs { get; set; } = new List<HopDong>();
}