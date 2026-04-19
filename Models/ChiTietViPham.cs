namespace DACS.Models
{
    public class ChiTietViPham
    {
        public int PhieuViPhamIdPhieuViPham { get; set; }
        public int QuyDinhIdQuyDinh { get; set; } 
        public decimal TocDoViPham { get; set; }
        public decimal ViDo { get; set; }
        public decimal KinhDo { get; set; }
        public DateTime ThoiGianXayRa { get; set; } = DateTime.Now;
        public virtual PhieuViPham PhieuViPham { get; set; } = null!;
        public virtual QuyDinh QuyDinh { get; set; } = null!;
    }
}
