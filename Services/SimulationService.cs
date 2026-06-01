using DeliveryWeb.Data;
using DeliveryWeb.Hubs;
using DeliveryWeb.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DeliveryWeb.Services
{
    public class SimulationService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<TrackingHub> _hub;

        public SimulationService(IServiceScopeFactory scopeFactory, IHubContext<TrackingHub> hub)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
        }

        public async Task LancerSimulationAsync(int commandeId)
        {
            _ = Task.Run(async () =>
            {
                try { await SimulerDeplacementAsync(commandeId); }
                catch (Exception ex) { Console.WriteLine($"[Simulation] Erreur: {ex.Message}"); }
            });
            await Task.CompletedTask;
        }

        private async Task SimulerDeplacementAsync(int commandeId)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var commande = await db.Commandes
                .Include(c => c.Livreur)
                .FirstOrDefaultAsync(c => c.Id == commandeId);

            if (commande?.Livreur == null) return;

            var livreur = commande.Livreur;
            var nomLivreur = $"{livreur.Prenom} {livreur.Nom}";
            var cmdIdStr = commandeId.ToString();
            var livIdStr = livreur.Id;

            // ── ÉTAPE 1 : En route (3s) ──
            await Task.Delay(3000);
            await MettreAJourStatut(commandeId, StatutCommande.EnCours);
            await _hub.Clients.Group(cmdIdStr).SendAsync("StatutMisAJour", "EnCours", nomLivreur);
            await _hub.Clients.Group("livreur_" + livIdStr).SendAsync("StatutCommandeMisAJour", commandeId, "EnCours");

            // ── ÉTAPE 2 : Déplacement GPS ──
            double latS = commande.LatDepart, lngS = commande.LngDepart;
            double latE = commande.LatArrivee, lngE = commande.LngArrivee;
            const int steps = 25, delai = 1200;

            for (int i = 1; i <= steps; i++)
            {
                double t = (double)i / steps;
                double lat = latS + (latE - latS) * t;
                double lng = lngS + (lngE - lngS) * t;

                using var s2 = _scopeFactory.CreateScope();
                var db2 = s2.ServiceProvider.GetRequiredService<AppDbContext>();
                var liv = await db2.Livreurs.FindAsync(livreur.Id);
                if (liv != null)
                {
                    liv.LatitudeActuelle = lat;
                    liv.LongitudeActuelle = lng;
                    await db2.SaveChangesAsync();
                }

                await _hub.Clients.Group(cmdIdStr).SendAsync("RecevoirPosition", lat, lng);
                await Task.Delay(delai);
            }

            // ── ÉTAPE 3 : Livré ──
            await Task.Delay(2000);
            await MettreAJourStatut(commandeId, StatutCommande.Livree);
            await _hub.Clients.Group(cmdIdStr).SendAsync("StatutMisAJour", "Livree", nomLivreur);
            await _hub.Clients.Group("livreur_" + livIdStr).SendAsync("StatutCommandeMisAJour", commandeId, "Livree");
        }

        private async Task MettreAJourStatut(int commandeId, StatutCommande statut)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var cmd = await db.Commandes.FindAsync(commandeId);
            if (cmd != null)
            {
                cmd.Statut = statut;
                await db.SaveChangesAsync();
            }
        }
    }
}