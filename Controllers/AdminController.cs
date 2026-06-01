using DeliveryWeb.Data;
using DeliveryWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeliveryWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(AppDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // DASHBOARD
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            ViewData["Title"] = "Tableau de bord";
            ViewData["Page"] = "dashboard";
            ViewData["TotalAujourdhui"] = await _db.Commandes.CountAsync(c => c.DateHeure.Date == today);
            ViewData["RevenusMois"] = await _db.Commandes
                                                .Where(c => c.DateHeure >= firstDayOfMonth && c.Statut == StatutCommande.Livree)
                                                .SumAsync(c => (decimal?)c.PrixCalcule) ?? 0;
            ViewData["CommandesAnnulees"] = await _db.Commandes.CountAsync(c => c.Statut == StatutCommande.Annulee);
            ViewData["LivreursActifs"] = await _db.Livreurs.CountAsync(l => l.Statut == StatutLivreur.Approuve);
            ViewData["LivreursAttente"] = await _db.Livreurs.CountAsync(l => l.Statut == StatutLivreur.EnAttente);
            ViewData["CommandesRecentes"] = await _db.Commandes
                                                .Include(c => c.Client)
                                                .Include(c => c.Livreur)
                                                .OrderByDescending(c => c.DateHeure)
                                                .Take(8)
                                                .ToListAsync();
            return View();
        }

        // LISTE LIVREURS
        public async Task<IActionResult> Livreurs(string? statut)
        {
            var query = _db.Livreurs.AsQueryable();

            if (!string.IsNullOrEmpty(statut) && Enum.TryParse<StatutLivreur>(statut, out var s))
                query = query.Where(l => l.Statut == s);

            ViewData["Title"] = "Gestion des livreurs";
            ViewData["Page"] = "livreurs";
            ViewData["Statut"] = statut;
            return View(await query.OrderByDescending(l => l.Statut).ToListAsync());
        }

        // APPROUVER
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approuver(string livreurId)
        {
            var livreur = await _db.Livreurs.FindAsync(livreurId);
            if (livreur == null) return NotFound();
            livreur.Statut = StatutLivreur.Approuve;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"{livreur.Prenom} {livreur.Nom} a été approuvé.";
            return RedirectToAction("Livreurs");
        }

        // SUSPENDRE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspendre(string livreurId)
        {
            var livreur = await _db.Livreurs.FindAsync(livreurId);
            if (livreur == null) return NotFound();
            livreur.Statut = StatutLivreur.Suspendu;
            await _db.SaveChangesAsync();
            TempData["Warning"] = $"{livreur.Prenom} {livreur.Nom} a été suspendu.";
            return RedirectToAction("Livreurs");
        }

        // BLOQUER
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bloquer(string livreurId)
        {
            var livreur = await _db.Livreurs.FindAsync(livreurId);
            if (livreur == null) return NotFound();
            livreur.Statut = StatutLivreur.Bloque;
            await _db.SaveChangesAsync();
            TempData["Danger"] = $"{livreur.Prenom} {livreur.Nom} a été bloqué.";
            return RedirectToAction("Livreurs");
        }

        // TOUTES LES COMMANDES
        public async Task<IActionResult> Commandes(string? statut, string? livreurId, DateTime? dateDebut, DateTime? dateFin)
        {
            var query = _db.Commandes
                .Include(c => c.Client)
                .Include(c => c.Livreur)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statut) && Enum.TryParse<StatutCommande>(statut, out var s))
                query = query.Where(c => c.Statut == s);

            if (!string.IsNullOrEmpty(livreurId))
                query = query.Where(c => c.LivreurId == livreurId);

            if (dateDebut.HasValue)
                query = query.Where(c => c.DateHeure.Date >= dateDebut.Value.Date);

            if (dateFin.HasValue)
                query = query.Where(c => c.DateHeure.Date <= dateFin.Value.Date);

            ViewData["Title"] = "Toutes les commandes";
            ViewData["Page"] = "commandes";
            ViewData["Livreurs"] = await _db.Livreurs.Where(l => l.Statut == StatutLivreur.Approuve).ToListAsync();

            return View(await query.OrderByDescending(c => c.DateHeure).ToListAsync());
        }

        //Rapports 
        public async Task<IActionResult> Rapports()
        {
            var today = DateTime.Today;
            var firstDayMonth = new DateTime(today.Year, today.Month, 1);
            var firstDayYear = new DateTime(today.Year, 1, 1);

            // ✅ Calculs directement en SQL via EF
            var totalMois = await _db.Commandes
                .CountAsync(c => c.DateHeure >= firstDayMonth);

            var revenusMois = await _db.Commandes
                .Where(c => c.DateHeure >= firstDayMonth && c.Statut == StatutCommande.Livree)
                .SumAsync(c => (decimal?)c.PrixCalcule) ?? 0;

            var totalAnnee = await _db.Commandes
                .CountAsync(c => c.DateHeure >= firstDayYear);

            var revenusAnnee = await _db.Commandes
                .Where(c => c.DateHeure >= firstDayYear && c.Statut == StatutCommande.Livree)
                .SumAsync(c => (decimal?)c.PrixCalcule) ?? 0;

            var commandesAnnulees = await _db.Commandes
                .CountAsync(c => c.Statut == StatutCommande.Annulee);

            var commandesLivrees = await _db.Commandes
                .CountAsync(c => c.Statut == StatutCommande.Livree);

            // Stats par livreur en SQL
            var statsLivreurs = await _db.Commandes
                .Where(c => c.LivreurId != null && c.Statut == StatutCommande.Livree)
                .Include(c => c.Livreur)
                .GroupBy(c => new { c.LivreurId, c.Livreur!.Prenom, c.Livreur.Nom })
                .Select(g => new {
                    Nom = g.Key.Prenom + " " + g.Key.Nom,
                    NbLivr = g.Count(),
                    Revenus = g.Sum(c => c.PrixCalcule)
                })
                .OrderByDescending(x => x.Revenus)
                .ToListAsync();

            // Commandes du mois pour le tableau
            var commandesMois = await _db.Commandes
                .Where(c => c.DateHeure >= firstDayMonth)
                .Include(c => c.Livreur)
                .OrderByDescending(c => c.DateHeure)
                .ToListAsync();

            ViewData["Title"] = "Rapports";
            ViewData["Page"] = "rapports";
            ViewData["TotalMois"] = totalMois;
            ViewData["RevenusMois"] = revenusMois;
            ViewData["TotalAnnee"] = totalAnnee;
            ViewData["RevenusAnnee"] = revenusAnnee;
            ViewData["CommandesAnnulees"] = commandesAnnulees;
            ViewData["CommandesLivrees"] = commandesLivrees;
            ViewData["StatsLivreurs"] = statsLivreurs;

            return View(commandesMois);
        }
    }
}