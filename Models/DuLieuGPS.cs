using System;

namespace DACS.Models;

public class DuLieuGPS
{
    public long Id { get; set; } 
    public int HanhTrinhIdHanhTrinh { get; set; }
    public int? ThietBiGPSIdThietBi { get; set; }
    
    public decimal KinhDo { get; set; } 
    public decimal ViDo { get; set; }   
    public decimal TocDo { get; set; }  
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public virtual HanhTrinh HanhTrinh { get; set; } = null!;
}