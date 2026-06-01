using DeliveryWeb.Data;
using DeliveryWeb.Models;
using DeliveryWeb.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _db;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _db = db;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpGet]
        public IActionResult Register() => View();

        // ── LOGIN ──
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            IdentityUser? user = null;

            if (model.TypeLogin == "email")
            {
                // ✅ Mode email → cherche uniquement par email
                if (!model.Identifiant.Contains("@"))
                {
                    ModelState.AddModelError("", "Veuillez entrer une adresse email valide");
                    return View(model);
                }
                user = await _userManager.FindByEmailAsync(model.Identifiant);
            }
            else
            {
                // ✅ Mode téléphone → cherche uniquement par téléphone (UserName)
                if (model.Identifiant.Contains("@"))
                {
                    ModelState.AddModelError("", "Veuillez entrer un numéro de téléphone, pas un email");
                    return View(model);
                }
                user = await _userManager.FindByNameAsync(model.Identifiant);
            }

            if (user == null)
            {
                ModelState.AddModelError("", "Identifiant ou mot de passe incorrect");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
    user, model.MotDePasse, isPersistent: true, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Identifiant ou mot de passe incorrect");
                return View(model);
            }

            // Redirection selon le rôle
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToAction("Index", "Admin");

            if (await _userManager.IsInRoleAsync(user, "Livreur"))
            {
                var livreur = await _db.Livreurs.FindAsync(user.Id);
                if (livreur?.Statut != StatutLivreur.Approuve)
                    return RedirectToAction("EnAttente", "Livreur");
                return RedirectToAction("Index", "Livreur");
            }

            return RedirectToAction("Index", "Client");
        }

        // ── REGISTER CLIENT ──
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Vérifier si email déjà utilisé
            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError("Email", "Cet email est déjà utilisé");
                return View(model);
            }

            // Vérifier si téléphone déjà utilisé
            if (await _userManager.FindByNameAsync(model.Telephone) != null)
            {
                ModelState.AddModelError("Telephone", "Ce numéro est déjà utilisé");
                return View(model);
            }

            if (!await _roleManager.RoleExistsAsync("Client"))
                await _roleManager.CreateAsync(new IdentityRole("Client"));

            var client = new Client
            {
                UserName = model.Telephone,
                Email = model.Email,
                EmailConfirmed = true,
                Nom = model.Nom,
                Prenom = model.Prenom,
                Telephone = model.Telephone,
                DateInscription = DateTime.Now
            };

            var result = await _userManager.CreateAsync(client, model.MotDePasse);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }

            await _userManager.AddToRoleAsync(client, "Client");
            await _signInManager.SignInAsync(client, isPersistent: false);
            return RedirectToAction("Index", "Client");
        }

        // ── LOGOUT ──
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
    }
}