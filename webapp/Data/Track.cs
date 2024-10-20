using System.ComponentModel.DataAnnotations;

public class Track
{
    [Key]
    public Guid Id { get; set; }
    public Guid AdvertisementId { get; set; }
    public string DriverId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

}
