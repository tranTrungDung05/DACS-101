namespace DACS.Models;

public class HopDong
{
    public int IdHopDong { get; set; }
    public DateOnly NgayTao { get; set; } = new DateOnly();
    public Decimal TongTien { get; set; }
    public bool TrangThai { get; set; } = true; // Mặc định hợp đồng mới tạo là đang hoạt động (true)
    public string MaCccd { get; set; } = string.Empty;
    public int IdTaiKhoan { get; set; }
    public virtual TaiKhoan TaiKhoan { get; set; } = null!;
    public virtual KhachHang KhachHang { get; set; } = null!;
    public virtual ICollection<ChiTietHopDong> ChiTietHopDongs { get; set; } = new List<ChiTietHopDong>();
}