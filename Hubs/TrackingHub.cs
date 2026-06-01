using Microsoft.AspNetCore.SignalR;

namespace DeliveryWeb.Hubs
{
    public class TrackingHub : Hub
    {
        public async Task RejoindreGroupe(string commandeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, commandeId);
        }

        // ✅ Le livreur rejoint le groupe de son type de véhicule
        public async Task RejoindreGroupeLivreur(string livreurId, string typeVehicule)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "livreur_" + livreurId);
            await Groups.AddToGroupAsync(Context.ConnectionId, "vehicule_" + typeVehicule);
        }

        public async Task EnvoyerPosition(string commandeId, double lat, double lng)
        {
            await Clients.Group(commandeId).SendAsync("RecevoirPosition", lat, lng);
        }

        public async Task MettreAJourStatut(string commandeId, string statut)
        {
            await Clients.Group(commandeId).SendAsync("StatutMisAJour", statut);
        }
    }
}