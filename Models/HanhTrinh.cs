using System;
using System.Collections.Generic;

namespace DACS.Models;

public class HanhTrinh
{
    public int IdHanhTrinh { get; set; }
    public DateTime NgayDi { get; set; } = DateTime.Now;
    public DateTime NgayDen { get; set; } = DateTime.Now;
    public decimal TongQuangDuong { get; set; } 
    public bool TrangThai { get; set; } = true;

    /// <summary>Kết quả phân loại hành vi: NORMAL, AGGRESSIVE, DROWSY (null = chưa phân tích)</summary>
    public string? PhanLoaiHanhVi { get; set; }

    public int IdPhuongTien { get; set; }
    public virtual PhuongTien PhuongTien { get; set; } = null!;

    public virtual ICollection<DuLieuGPS> DuLieuGPS { get; set; } = new List<DuLieuGPS>();
    public virtual ICollection<DuLieuGiaTocKe> DuLieuGiaTocKes { get; set; } = new List<DuLieuGiaTocKe>();
    public virtual ICollection<PhieuViPham> PhieuViPhams { get; set; } = new List<PhieuViPham>();
}