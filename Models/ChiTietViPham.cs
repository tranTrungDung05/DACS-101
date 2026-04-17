namespace DACS.Models
{
    public class ChiTietViPham
    {
        public int IdPhieuViPham { get; set; }
        public int IdQuyDinh { get; set; } 
        public DateTime ThoiGianXayRa { get; set; } = DateTime.Now;
        public virtual PhieuViPham PhieuViPham { get; set; } = null!;
        public virtual QuyDinh QuyDinh { get; set; } = null!;
    }
}
