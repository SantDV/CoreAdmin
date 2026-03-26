namespace GymDashboard.Models;

public class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime JoinDate { get; set; }
    public bool IsActive { get; set; } = true;
}
