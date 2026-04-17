namespace DACS.Models;

public class Quyen
{
    public int IdQuyen { get; set; }
    public string TenQuyen { get; set; } = string.Empty;
    public string MaQuyen { get; set; } = string.Empty;

    public virtual ICollection<ChiTietQuyen> ChiTietQuyens { get; set; } = new List<ChiTietQuyen>();
}