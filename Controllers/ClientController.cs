using DeliveryWeb.Data;
using DeliveryWeb.Hubs;
using DeliveryWeb.Models;
using DeliveryWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DeliveryWeb.Controllers
{
    [Authorize(Roles = "Client")]
    public class ClientController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly PrixService _prixService;
        private readonly SimulationService _simulationService;
        private readonly IHubContext<TrackingHub> _hub; // ✅ Ajouté


        private readonly IHttpClientFactory _httpClientFactory;

        public ClientController(
         AppDbContext db,
         UserManager<IdentityUser> userManager,
         PrixService prixService,
         SimulationService simulationService,
         IHttpClientFactory httpClientFactory,
         IHubContext<TrackingHub> hub) // ✅ Ajouté
        {
            _db = db;
            _userManager = userManager;
            _prixService = prixService;
            _simulationService = simulationService;
            _httpClientFactory = httpClientFactory;
            _hub = hub; // ✅ Ajouté
        }

        private async Task<Client?> GetCurrentClient()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _db.Clients.FindAsync(user.Id);
        }

        // ── DASHBOARD ──
        public async Task<IActionResult> Index()
        {
            var client = await GetCurrentClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var commandes = await _db.Commandes
                .Where(c => c.ClientId == client.Id)
                .Include(c => c.Livreur)
                .OrderByDescending(c => c.DateHeure)
                .ToListAsync();

            ViewData["Title"] = "Tableau de bord";
            ViewData["Page"] = "dashboard";
            ViewData["UserName"] = $"{client.Prenom} {client.Nom}";
            ViewData["Commandes"] = commandes;
            ViewData["EnCours"] = commandes.Count(c =>
                c.Statut == StatutCommande.EnCours ||
                c.Statut == StatutCommande.Acceptee);
            ViewData["Livrees"] = commandes.Count(c =>
                c.Statut == StatutCommande.Livree);
            ViewData["EnAttente"] = commandes.Count(c =>
                c.Statut == StatutCommande.EnAttente);

            return View();
        }

        // ── NOUVELLE COMMANDE (GET) ──
        public async Task<IActionResult> NouvelleCommande()
        {
            var client = await GetCurrentClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var livreurs = await _db.Livreurs
                .Where(l => l.Statut == StatutLivreur.Approuve)
                .ToListAsync();

            ViewData["Title"] = "Nouvelle commande";
            ViewData["Page"] = "commande";
            ViewData["UserName"] = $"{client.Prenom} {client.Nom}";
            ViewData["Livreurs"] = livreurs;

            return View();
        }

        // ── CALCUL PRIX (AJAX) ──
        [HttpPost]
        [IgnoreAntiforgeryToken]           // ← évite le rejet silencieux en mode AJAX
        [Produces("application/json")]     // ← force JSON même en cas d'erreur middleware
        public async Task<IActionResult> CalculerPrix([FromBody] CalculPrixRequest? req)
        {
            try
            {
                if (req == null)
                    return Json(new { succes = false, prix = "0", erreur = "Requête invalide" });

                var prix = await _prixService.CalculerAsync(
                    req.LatDepart, req.LngDepart,
                    req.LatArrivee, req.LngArrivee,
                    req.TypeVehicule,
                    req.EstExpress
                );
                return Json(new { succes = true, prix = prix.ToString("F0") });
            }
            catch (Exception ex)
            {
                return Json(new { succes = false, prix = "0", erreur = ex.Message });
            }
        }

        // ── SOUMETTRE COMMANDE (POST) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoumettreCommande(Commande commande)
        {
            var client = await GetCurrentClient();
            if (client == null) return RedirectToAction("Login", "Account");

            commande.ClientId = client.Id;
            commande.DateHeure = DateTime.Now;
            commande.Statut = StatutCommande.EnAttente;

            if (string.IsNullOrEmpty(commande.LivreurId))
                commande.LivreurId = null;

            try
            {
                if (commande.PrixCalcule <= 0)
                {
                    commande.PrixCalcule = await _prixService.CalculerAsync(
                        commande.LatDepart, commande.LngDepart,
                        commande.LatArrivee, commande.LngArrivee,
                        commande.TypeVehicule,
                        commande.EstExpress
                    );
                }

                _db.Commandes.Add(commande);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Commande #{commande.Id} passée ! En attente d'un livreur...";
                return RedirectToAction("Suivi", new { id = commande.Id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Erreur lors de l'enregistrement : {ex.Message}";
                return RedirectToAction("NouvelleCommande");
            }
        }

        // ── SUIVI EN TEMPS RÉEL ──
        public async Task<IActionResult> Suivi(int id)
        {
            var client = await GetCurrentClient();
            if (client == null) return RedirectToAction("Login", "Account");
            var commande = await _db.Commandes
                .Include(c => c.Livreur)
                .FirstOrDefaultAsync(c => c.Id == id && c.ClientId == client.Id);
            if (commande == null) return NotFound();
            ViewData["Title"] = $"Suivi commande #{id}";
            ViewData["Page"] = "suivi";
            ViewData["UserName"] = $"{client.Prenom} {client.Nom}";
            return View(commande);
        }

        // ✅ NOUVELLE MÉTHODE — À AJOUTER JUSTE ICI
        public async Task<IActionResult> SuiviActif()
        {
            var client = await GetCurrentClient();
            if (client == null) return RedirectToAction("Login", "Account");

            // Cherche la dernière commande active
            var commande = await _db.Commandes
                .Where(c => c.ClientId == client.Id &&
                       (c.Statut == StatutCommande.EnAttente ||
                        c.Statut == StatutCommande.Acceptee ||
                        c.Statut == StatutCommande.EnCours))
                .OrderByDescending(c => c.DateHeure)
                .FirstOrDefaultAsync();

            // Aucune commande active → aller à l'historique
            if (commande == null)
                return RedirectToAction("Historique");

            // Rediriger vers Suivi avec l'ID
            return RedirectToAction("Suivi", new { id = commande.Id });
        }

        // ── HISTORIQUE ──
        public async Task<IActionResult> Historique()
        {
            var client = await GetCurrentClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var commandes = await _db.Commandes
                .Where(c => c.ClientId == client.Id)
                .Include(c => c.Livreur)
                .Include(c => c.Avis)
                .OrderByDescending(c => c.DateHeure)
                .ToListAsync();

            ViewData["Title"] = "Mes commandes";
            ViewData["Page"] = "historique";
            ViewData["UserName"] = $"{client.Prenom} {client.Nom}";
            return View(commandes);
        }

        // ── NOTER (GET) ──
        public async Task<IActionResult> NoterCommande(int id)
        {
            var client = await GetCurrentClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var commande = await _db.Commandes
                .Include(c => c.Livreur)
                .FirstOrDefaultAsync(c =>
                    c.Id == id &&
                    c.ClientId == client.Id &&
                    c.Statut == StatutCommande.Livree);

            if (commande == null) return RedirectToAction("Historique");

            ViewData["Title"] = "Noter la livraison";
            ViewData["Page"] = "historique";
            ViewData["UserName"] = $"{client.Prenom} {client.Nom}";
            return View(commande);
        }

        // ── NOTER (POST) ──
        [HttpPost]
        public async Task<IActionResult> NoterCommande(
            int commandeId, int note, string? commentaire)
        {
            var client = await GetCurrentClient();
            if (client == null) return RedirectToAction("Login", "Account");

            var commande = await _db.Commandes
                .FirstOrDefaultAsync(c =>
                    c.Id == commandeId &&
                    c.ClientId == client.Id);

            if (commande == null || commande.LivreurId == null)
                return RedirectToAction("Historique");

            var avis = new Avis
            {
                CommandeId = commandeId,
                ClientId = client.Id,
                LivreurId = commande.LivreurId,
                Note = Math.Clamp(note, 1, 5),
                Commentaire = commentaire,
                DateAvis = DateTime.Now
            };

            _db.Avis.Add(avis);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Merci pour votre avis ! ⭐";
            return RedirectToAction("Historique");
        }

        // ── PROFIL CLIENT ──
        public async Task<IActionResult> Profil()
        {
            var client = await GetCurrentClient();
            if (client == null) return RedirectToAction("Login", "Account");

            // ✅ Stats calculées dans le controller
            var commandes = await _db.Commandes
                .Where(c => c.ClientId == client.Id)
                .ToListAsync();

            ViewData["Title"] = "Mon profil";
            ViewData["Page"] = "profil";
            ViewData["UserName"] = $"{client.Prenom} {client.Nom}";
            ViewData["NbTotal"] = commandes.Count;
            ViewData["NbLivrees"] = commandes.Count(c => c.Statut == StatutCommande.Livree);

            return View(client);
        }

        // ── GEOCODAGE SERVER-SIDE (évite le CORS) ──
        [HttpGet]
        public async Task<IActionResult> Geocoder(string adresse)
        {
            try
            {
                var url = $"https://nominatim.openstreetmap.org/search" +
                           $"?q={Uri.EscapeDataString(adresse + ", Abidjan, Côte d'Ivoire")}" +
                           $"&format=json&limit=1&countrycodes=ci";

                // ✅ Plus de new HttpClient()
                var http = _httpClientFactory.CreateClient();
                http.DefaultRequestHeaders.Add("User-Agent", "PickMeApp/1.0");

                var response = await http.GetStringAsync(url);
                var results = System.Text.Json.JsonSerializer
                    .Deserialize<List<NominatimResult>>(response);

                if (results == null || results.Count == 0)
                    return Json(new { found = false });

                return Json(new { found = true, lat = results[0].lat, lon = results[0].lon });
            }
            catch { return Json(new { found = false }); }
        }

        [HttpGet]
        public async Task<IActionResult> ReverseGeocode(double lat, double lon)
        {
            try
            {
                var inv = System.Globalization.CultureInfo.InvariantCulture;
                var url = $"https://nominatim.openstreetmap.org/reverse" +
                           $"?lat={lat.ToString(inv)}&lon={lon.ToString(inv)}&format=json";

                var http = _httpClientFactory.CreateClient();
                http.DefaultRequestHeaders.Add("User-Agent", "PickMeApp/1.0");

                var response = await http.GetStringAsync(url);
                var json = System.Text.Json.JsonDocument.Parse(response);
                var nom = json.RootElement.GetProperty("display_name").GetString() ?? $"{lat},{lon}";
                var adresse = string.Join(", ", nom.Split(',').Take(3));

                return Json(new { found = true, adresse });
            }
            catch { return Json(new { found = false, adresse = $"{lat}, {lon}" }); }
        }

        public class NominatimResult
        {
            public string lat { get; set; } = "";
            public string lon { get; set; } = "";
            public string display_name { get; set; } = "";
        }
    }

    // ── DTO ──
    public class CalculPrixRequest
    {
        public double LatDepart { get; set; }
        public double LngDepart { get; set; }
        public double LatArrivee { get; set; }
        public double LngArrivee { get; set; }
        public string TypeVehicule { get; set; } = "Moto";
        public bool EstExpress { get; set; } = false;
    }
}