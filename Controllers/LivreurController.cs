using DeliveryWeb.Data;
using DeliveryWeb.Hubs;
using DeliveryWeb.Models;
using DeliveryWeb.Services;
using DeliveryWeb.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DeliveryWeb.Controllers
{
    public class LivreurController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHubContext<TrackingHub> _hub;
        private readonly SimulationService _simulationService;

        public LivreurController(AppDbContext db, UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager, RoleManager<IdentityRole> roleManager,
            IHubContext<TrackingHub> hub, SimulationService simulationService)
        {
            _db = db; _userManager = userManager; _signInManager = signInManager;
            _roleManager = roleManager; _hub = hub; _simulationService = simulationService;
        }

        private async Task<Livreur?> GetCurrentLivreur()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;
            return await _db.Livreurs.FindAsync(user.Id);
        }

        [HttpGet] public IActionResult Register() => View();
        [HttpGet] public IActionResult EnAttente() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterLivreurViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            if (!await _roleManager.RoleExistsAsync("Livreur"))
                await _roleManager.CreateAsync(new IdentityRole("Livreur"));

            var livreur = new Livreur
            {
                UserName = model.Telephone,
                Email = model.Email,
                Nom = model.Nom,
                Prenom = model.Prenom,
                Telephone = model.Telephone,
                NumPermis = model.NumPermis,
                TypeVehicule = model.TypeVehicule,
                HoraireDebut = model.HoraireDebut,
                HoraireFin = model.HoraireFin,
                Statut = StatutLivreur.EnAttente
            };

            var result = await _userManager.CreateAsync(livreur, model.MotDePasse);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(model);
            }
            await _userManager.AddToRoleAsync(livreur, "Livreur");
            return RedirectToAction("EnAttente");
        }

        [Authorize(Roles = "Livreur")]
        public async Task<IActionResult> Index()
        {
            var livreur = await GetCurrentLivreur();
            if (livreur == null) return RedirectToAction("Login", "Account");
            if (livreur.Statut != StatutLivreur.Approuve) return RedirectToAction("EnAttente");

            var typeVehiculeStr = livreur.TypeVehicule.ToString();

            // IDs des commandes refusées par ce livreur
            var refusIds = await _db.RefusLivraisons
                .Where(r => r.LivreurId == livreur.Id)
                .Select(r => r.CommandeId)
                .ToListAsync();

            // ✅ Commandes disponibles — exclut les refusées
            var commandesDisponibles = await _db.Commandes
                .Where(c => c.Statut == StatutCommande.EnAttente
                         && c.TypeVehicule == typeVehiculeStr
                         && (c.LivreurId == null || c.LivreurId == livreur.Id)
                         && !refusIds.Contains(c.Id))
                .Include(c => c.Client)
                .OrderByDescending(c => c.DateHeure)
                .ToListAsync();

            // Mes commandes en cours
            var mesCommandes = await _db.Commandes
                .Where(c => c.LivreurId == livreur.Id && c.Statut != StatutCommande.EnAttente)
                .Include(c => c.Client)
                .OrderByDescending(c => c.DateHeure)
                .ToListAsync();

            ViewData["Title"] = "Tableau de bord";
            ViewData["Page"] = "dashboard";
            ViewData["UserName"] = $"{livreur.Prenom} {livreur.Nom}";
            ViewData["Livreur"] = livreur;
            ViewData["CommandesDisponibles"] = commandesDisponibles;
            ViewData["MesCommandes"] = mesCommandes;
            ViewData["EnCours"] = mesCommandes.Count(c =>
                c.Statut == StatutCommande.EnCours || c.Statut == StatutCommande.Acceptee);
            ViewData["Livrees"] = mesCommandes.Count(c => c.Statut == StatutCommande.Livree);

            return View();
        }

        [Authorize(Roles = "Livreur")]
        [HttpPost]
        public async Task<IActionResult> Accepter(int commandeId)
        {
            var livreur = await GetCurrentLivreur();
            var commande = await _db.Commandes.FindAsync(commandeId);
            if (commande == null || livreur == null) return NotFound();

            commande.LivreurId = livreur.Id;
            commande.Statut = StatutCommande.Acceptee;
            await _db.SaveChangesAsync();

            await _hub.Clients.Group(commandeId.ToString())
                .SendAsync("StatutMisAJour", "Acceptee", $"{livreur.Prenom} {livreur.Nom}");

            await _simulationService.LancerSimulationAsync(commandeId);

            TempData["Success"] = $"✅ Commande #{commandeId} acceptée !";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Livreur")]
        [HttpPost]
        public async Task<IActionResult> Refuser(int commandeId)
        {
            var livreur = await GetCurrentLivreur();
            var commande = await _db.Commandes.FindAsync(commandeId);
            if (commande == null || livreur == null) return NotFound();

            // ✅ Enregistrer le refus
            _db.RefusLivraisons.Add(new RefusLivraison
            {
                CommandeId = commandeId,
                LivreurId = livreur.Id,
                DateRefus = DateTime.Now
            });

            commande.LivreurId = null;
            commande.Statut = StatutCommande.EnAttente;
            await _db.SaveChangesAsync();

            TempData["Warning"] = $"Commande #{commandeId} refusée.";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Livreur")]
        [HttpPost]
        public async Task<IActionResult> MettreAJourStatut(int commandeId, string statut)
        {
            var livreur = await GetCurrentLivreur();
            var commande = await _db.Commandes.FindAsync(commandeId);
            if (commande == null) return NotFound();

            commande.Statut = statut switch
            {
                "EnRoute" => StatutCommande.EnCours,
                "EnLivraison" => StatutCommande.EnCours,
                "Livree" => StatutCommande.Livree,
                _ => commande.Statut
            };
            await _db.SaveChangesAsync();

            var nomLivreur = livreur != null ? $"{livreur.Prenom} {livreur.Nom}" : "";
            await _hub.Clients.Group(commandeId.ToString())
                .SendAsync("StatutMisAJour", statut, nomLivreur);

            TempData["Success"] = "Statut mis à jour !";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Livreur")]
        public async Task<IActionResult> Historique()
        {
            var livreur = await GetCurrentLivreur();
            if (livreur == null) return RedirectToAction("Login", "Account");

            var commandes = await _db.Commandes
                .Where(c => c.LivreurId == livreur.Id)
                .Include(c => c.Client)
                .OrderByDescending(c => c.DateHeure)
                .ToListAsync();

            ViewData["Title"] = "Mes livraisons";
            ViewData["Page"] = "historique";
            ViewData["UserName"] = $"{livreur.Prenom} {livreur.Nom}";
            return View(commandes);
        }

        [Authorize(Roles = "Livreur")]
        public async Task<IActionResult> Profil()
        {
            var livreur = await GetCurrentLivreur();
            if (livreur == null) return RedirectToAction("Login", "Account");

            var commandes = await _db.Commandes.Where(c => c.LivreurId == livreur.Id).ToListAsync();
            ViewData["Title"] = "Mon profil";
            ViewData["Page"] = "profil";
            ViewData["UserName"] = $"{livreur.Prenom} {livreur.Nom}";
            ViewData["NbTotal"] = commandes.Count;
            ViewData["NbLivrees"] = commandes.Count(c => c.Statut == StatutCommande.Livree);
            ViewData["Revenus"] = commandes.Where(c => c.Statut == StatutCommande.Livree).Sum(c => c.PrixCalcule);
            return View(livreur);
        }
    }
}