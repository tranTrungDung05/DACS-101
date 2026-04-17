using System.ComponentModel.DataAnnotations;

namespace DACS.Models;

public class KhachHang
{
    [Key] // Đánh dấu đây là khóa chính vì nó không tên là "Id"
    public string MaCccd { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public DateOnly NgaySinh { get; set; }
    public string DiaChi { get; set; } = string.Empty;
    public string Sdt { get; set; } = string.Empty;

    public virtual ICollection<HopDong> HopDongs { get; set; } = new List<HopDong>();
    public virtual ICollection<PhieuViPham> PhieuViPhams { get; set; } = new List<PhieuViPham>();
}