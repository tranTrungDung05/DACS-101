namespace DACS.Models;

public class ChiTietHopDong
{
    public int IdPhuongTien { get; set; }
    public int IdHopDong { get; set; } 
    public DateOnly NgayThue { get; set; } = new DateOnly();
    public DateOnly NgayTra { get; set; } = new DateOnly();
    public Decimal GiaThue { get; set; }
    public virtual PhuongTien PhuongTien { get; set; } = null!;
    public virtual HopDong HopDong { get; set; } = null!;
}
