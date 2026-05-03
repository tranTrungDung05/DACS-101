using System;

namespace DACS.Models;

public class DuLieuGPS
{
    public long Id { get; set; } 
    public int HanhTrinhIdHanhTrinh { get; set; }
    public int? IdThietBi { get; set; }
    
    public double KinhDo { get; set; } 
    public double ViDo { get; set; }   
    public double TocDo { get; set; }  
    public double? GpsX { get; set; } // Thêm cột GpsX cho model AI
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public virtual HanhTrinh HanhTrinh { get; set; } = null!;
    public virtual ThietBiGPS? ThietBiGPS { get; set; }
}