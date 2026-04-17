namespace DACS.Models
{
    public class ThietBiGPS
    {
        public int IdThietBi { get; set; }
        public string MaImei { get; set; } = string.Empty;
        public string LoaiThietBi { get; set; } = string.Empty;
        public DateOnly NgayKichHoat { get; set; } = new DateOnly();
        public bool TrangThai { get; set; } = true; // Mặc định thiết bị mới tạo là đang hoạt động (true)
        public virtual ICollection<PhuongTien> PhuongTiens { get; set; } = new List<PhuongTien>();
        public virtual ICollection<DuLieuGPS> DuLieuGPS { get; set; } = new List<DuLieuGPS>();
    }
}
