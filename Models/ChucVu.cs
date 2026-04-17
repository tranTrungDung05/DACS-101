namespace DACS.Models;

public class ChucVu
{
    public int IdChucVu { get; set; }
    public string TenChucVu { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;

    public virtual ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
    public virtual ICollection<ChiTietQuyen> ChiTietQuyen { get; set; } = new List<ChiTietQuyen>();
}