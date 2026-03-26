using GymDashboard.Models;

namespace GymDashboard.Services;

public class ClientService
{
    private List<Client> _clients = new List<Client>
    {
        new Client { Id = 1, Name = "Juan Pérez", Email = "juan@example.com", Phone = "555-1234", JoinDate = DateTime.Now.AddMonths(-2), IsActive = true },
        new Client { Id = 2, Name = "María Gómez", Email = "maria@example.com", Phone = "555-5678", JoinDate = DateTime.Now.AddDays(-15), IsActive = true },
        new Client { Id = 3, Name = "Carlos López", Email = "carlos@example.com", Phone = "555-9012", JoinDate = DateTime.Now.AddMonths(-1), IsActive = false }
    };

    public Task<List<Client>> GetClientsAsync()
    {
        return Task.FromResult(_clients.ToList());
    }

    public Task AddClientAsync(Client client)
    {
        client.Id = _clients.Any() ? _clients.Max(c => c.Id) + 1 : 1;
        if (client.JoinDate == default) 
            client.JoinDate = DateTime.Now;
        _clients.Add(client);
        return Task.CompletedTask;
    }

    public Task DeleteClientAsync(int id)
    {
        var client = _clients.FirstOrDefault(c => c.Id == id);
        if (client != null)
        {
            _clients.Remove(client);
        }
        return Task.CompletedTask;
    }

    public Task UpdateClientAsync(Client updatedClient)
    {
        var index = _clients.FindIndex(c => c.Id == updatedClient.Id);
        if (index != -1)
        {
            _clients[index] = updatedClient;
        }
        return Task.CompletedTask;
    }
}
