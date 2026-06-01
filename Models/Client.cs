using DeliveryWeb.Models;
using Microsoft.AspNetCore.Identity;

namespace DeliveryWeb.Models
{
    public class Client : IdentityUser
    {
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public DateTime DateInscription { get; set; } = DateTime.Now;

        // Navigation
        public ICollection<Commande> Commandes { get; set; } = new List<Commande>();
        public ICollection<Avis> Avis { get; set; } = new List<Avis>();
    }
}