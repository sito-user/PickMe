using DeliveryWeb.Models;
using Microsoft.AspNetCore.Identity;

namespace DeliveryWeb.Models
{
    public enum TypeVehicule { Moto, Voiture, Camionnette }
    public enum StatutLivreur { EnAttente, Approuve, Suspendu, Bloque }

    public class Livreur : IdentityUser
    {
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Telephone { get; set; } = string.Empty;
        public string NumPermis { get; set; } = string.Empty;
        public TypeVehicule TypeVehicule { get; set; }
        public TimeOnly HoraireDebut { get; set; }
        public TimeOnly HoraireFin { get; set; }
        public StatutLivreur Statut { get; set; } = StatutLivreur.EnAttente;
        public double? LatitudeActuelle { get; set; }
        public double? LongitudeActuelle { get; set; }

        // Navigation
        public ICollection<Commande> Commandes { get; set; } = new List<Commande>();
        public ICollection<Avis> Avis { get; set; } = new List<Avis>();
    }
}