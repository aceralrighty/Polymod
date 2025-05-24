namespace TBD.API.DTOs;

public class CreateServiceDto
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Price { get; set; }
    public int DurationInMinutes { get; set; }
    public Guid ProviderId { get; set; }
}