namespace DACS.Models
{
    public class QuyDinh
    {
        public int IdQuyDinh { get; set; }
        public string TenQuyDinh { get; set; } = string.Empty;
        public Decimal MucPhat { get; set; }
        public string MoTa { get; set; } = string.Empty;
        public virtual ICollection<ChiTietViPham> ChiTietViPhams { get; set; } = new List<ChiTietViPham>();
    }
}
