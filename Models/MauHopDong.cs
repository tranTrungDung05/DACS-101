using System.ComponentModel.DataAnnotations;

namespace DACS.Models
{
    public class MauHopDong
    {
        [Key]
        public int IdMau { get; set; }
        
        [Required]
        [Display(Name = "Tên mẫu hợp đồng")]
        public string TenMau { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Nội dung (HTML)")]
        public string NoiDungHtml { get; set; } = string.Empty;

        public DateTime NgayTao { get; set; } = DateTime.Now;
        
        public bool LaMacDinh { get; set; } = false;
    }
}
