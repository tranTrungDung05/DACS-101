namespace DACS.Models
{
    public class PhieuViPham
    {
        public int IdPhieuViPham { get; set; }
        public DateTime NgayViPham { get; set; } = DateTime.Now;
        public Decimal TienPhat { get; set; } 
        public string MoTa { get; set; } = string.Empty; 
        public bool TrangThai { get; set; } = true; // Mặc định phiếu vi phạm mới tạo là đang hoạt động (true)
        public int IdHanhTrinh { get; set; }
        public string MaCccd { get; set; } = string.Empty;
        public virtual HanhTrinh HanhTrinh { get; set; } = null!;
        public virtual KhachHang KhachHang { get; set; } = null!;
        public virtual ICollection<ChiTietViPham> ChiTietViPhams { get; set; } = new List<ChiTietViPham>();
    }
}
