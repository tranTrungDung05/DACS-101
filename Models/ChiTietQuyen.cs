namespace DACS.Models;

public class ChiTietQuyen
{
    public int IdChucVu { get; set; }
    public int IdQuyen { get; set; }

    public virtual ChucVu ChucVu { get; set; } = null!;
    public virtual Quyen Quyen { get; set; } = null!;
}