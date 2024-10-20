using System.ComponentModel.DataAnnotations;

public class Advertisement
{
    [Key]
    public Guid Id { get; set; }
    public string Text { get; set; }
    public string Ages { get; set; }
    public string Pet { get; set; }
    public string Times { get; set; }
    public string Where { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

}
public class AdvertisementViewModel
{
    [Required(ErrorMessage = "The advertisement text is required.")]
    public string Text { get; set; }

    [Required(ErrorMessage = "Please select at least one age range.")]
    public List<string> Ages { get; set; } = new List<string>();

    [Required(ErrorMessage = "Please select a pet preference.")]
    public string Pet { get; set; }

    [Required(ErrorMessage = "Please select at least one available time.")]
    public List<string> Times { get; set; } = new List<string>();

    [Required(ErrorMessage = "The location is required.")]
    public string Where { get; set; }
}
