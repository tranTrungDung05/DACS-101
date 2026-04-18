using Microsoft.EntityFrameworkCore;
using DACS.Models;

namespace DACS.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // 1. Đăng ký các Model vào Database
    public DbSet<TaiKhoan> TaiKhoans { get; set; }
    public DbSet<ChucVu> ChucVus { get; set; }
    public DbSet<Quyen> Quyens { get; set; }
    public DbSet<ChiTietQuyen> ChiTietQuyens { get; set; }
    public DbSet<PhuongTien> PhuongTiens { get; set; }
    public DbSet<ThietBiGPS> ThietBiGPS { get; set; }
    public DbSet<HanhTrinh> HanhTrinhs { get; set; }
    public DbSet<DuLieuGPS> DuLieuGPS { get; set; }
    public DbSet<KhachHang> KhachHangs { get; set; }
    public DbSet<HopDong> HopDongs { get; set; }
    public DbSet<ChiTietHopDong> ChiTietHopDongs { get; set; }
    public DbSet<PhieuViPham> PhieuViPhams { get; set; }
    public DbSet<QuyDinh> QuyDinhs { get; set; }
    public DbSet<ChiTietViPham> ChiTietViPhams { get; set; }

    // 2. Cấu hình chi tiết bằng Fluent API
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- CẤU HÌNH KHÓA CHÍNH (ÉP BUỘC) ---
        // Chỉ định rõ ràng để EF không còn thắc mắc
        modelBuilder.Entity<ChucVu>().HasKey(c => c.IdChucVu);
        modelBuilder.Entity<Quyen>().HasKey(q => q.IdQuyen);
        modelBuilder.Entity<QuyDinh>().HasKey(qd => qd.IdQuyDinh);
        modelBuilder.Entity<TaiKhoan>().HasKey(tk => tk.IdTaiKhoan);
        modelBuilder.Entity<PhuongTien>().HasKey(pt => pt.IdPhuongTien);
        modelBuilder.Entity<ThietBiGPS>().HasKey(tb => tb.IdThietBi);
        modelBuilder.Entity<HanhTrinh>().HasKey(ht => ht.IdHanhTrinh);
        modelBuilder.Entity<HopDong>().HasKey(hd => hd.IdHopDong);
        modelBuilder.Entity<PhieuViPham>().HasKey(pvp => pvp.IdPhieuViPham);
        modelBuilder.Entity<DuLieuGPS>().HasKey(gps => gps.Id); // Theo file mới em đã sửa cho anh có trường Id

        // --- Cấu hình Mối quan hệ (Foreign Keys) ---
        modelBuilder.Entity<TaiKhoan>()
            .HasOne(t => t.ChucVu)
            .WithMany(c => c.TaiKhoans)
            .HasForeignKey(t => t.IdChucVu);

        modelBuilder.Entity<PhuongTien>()
            .HasOne(p => p.ThietBiGPS)
            .WithMany(t => t.PhuongTiens)
            .HasForeignKey(p => p.IdThietBi);

        modelBuilder.Entity<HanhTrinh>()
            .HasOne(h => h.PhuongTien)
            .WithMany(p => p.HanhTrinhs)
            .HasForeignKey(h => h.PhuongTienIdPhuongTien);

        modelBuilder.Entity<DuLieuGPS>()
            .HasOne(d => d.HanhTrinh)
            .WithMany(h => h.DuLieuGPS)
            .HasForeignKey(d => d.HanhTrinhIdHanhTrinh);

        // --- Cấu hình Khóa chính phức hợp cho các bảng trung gian ---
        modelBuilder.Entity<ChiTietQuyen>().HasKey(ct => new { ct.IdChucVu, ct.IdQuyen });
        modelBuilder.Entity<ChiTietHopDong>().HasKey(ct => new { ct.IdPhuongTien, ct.IdHopDong });
        modelBuilder.Entity<ChiTietViPham>().HasKey(ct => new { ct.IdPhieuViPham, ct.IdQuyDinh });

        // Khóa chính không theo quy ước
        modelBuilder.Entity<KhachHang>().HasKey(kh => kh.MaCccd);

        // --- Cấu hình Kiểu dữ liệu chính xác (Decimal) ---
        modelBuilder.Entity<DuLieuGPS>(entity => {
            entity.Property(e => e.KinhDo).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.ViDo).HasColumnType("decimal(11, 8)");
            entity.Property(e => e.TocDo).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<HopDong>().Property(h => h.TongTien).HasColumnType("decimal(18, 2)");
        modelBuilder.Entity<PhieuViPham>().Property(p => p.TienPhat).HasColumnType("decimal(18, 2)");
        modelBuilder.Entity<QuyDinh>().Property(q => q.MucPhat).HasColumnType("decimal(18, 2)");
        modelBuilder.Entity<HanhTrinh>().Property(h => h.TongQuangDuong).HasColumnType("decimal(18, 2)");
        modelBuilder.Entity<ChiTietHopDong>().Property(c => c.GiaThue).HasColumnType("decimal(18, 2)");

        // --- Cấu hình Unique Index ---
        modelBuilder.Entity<PhuongTien>().HasIndex(p => p.BienSo).IsUnique();
        modelBuilder.Entity<ThietBiGPS>().HasIndex(t => t.MaImei).IsUnique();

        // --- SEED DATA ---
        modelBuilder.Entity<ChucVu>().HasData(
            new ChucVu { IdChucVu = 1, TenChucVu = "Admin", MoTa = "Quản trị viên toàn quyền hệ thống" },
            new ChucVu { IdChucVu = 2, TenChucVu = "Manager", MoTa = "Quản lý phương tiện" }
        );

        modelBuilder.Entity<TaiKhoan>().HasData(
            new TaiKhoan 
            { 
                IdTaiKhoan = 1, 
                TenDangNhap = "admin", 
                MatKhau = "123", 
                HoTen = "Super Admin", 
                Email = "admin@gps.com", 
                IdChucVu = 1 
            }
        );
    }
}