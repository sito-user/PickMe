namespace DeliveryWeb.Models
{
    public enum StatutCommande { EnAttente, Acceptee, EnCours, Livree, Annulee }
    public enum ModePaiement { Cash, Carte }

    public class Commande
    {
        public int Id { get; set; }
        public DateTime DateHeure { get; set; } = DateTime.Now;
        public StatutCommande Statut { get; set; } = StatutCommande.EnAttente;

        // Adresses
        public string AdresseDepart { get; set; } = string.Empty;
        public double LatDepart { get; set; }
        public double LngDepart { get; set; }
        public string AdresseArrivee { get; set; } = string.Empty;
        public double LatArrivee { get; set; }
        public double LngArrivee { get; set; }

        // Expéditeur & Destinataire
        public string NomExpediteur { get; set; } = string.Empty;
        public string TelExpediteur { get; set; } = string.Empty;
        public string NomDestinataire { get; set; } = string.Empty;
        public string TelDestinataire { get; set; } = string.Empty;

        // Colis
        public string? InfosColis { get; set; }
        public double? PoidsColis { get; set; }
        public bool Fragile { get; set; } = false;

        // Type véhicule & Express
        public string TypeVehicule { get; set; } = "Moto";
        public bool EstExpress { get; set; } = false;

        // Prix & Paiement
        public decimal PrixCalcule { get; set; }
        public ModePaiement ModePaiement { get; set; }

        // Clés étrangères
        public string ClientId { get; set; } = string.Empty;
        public string? LivreurId { get; set; }

        // Navigation
        public Client? Client { get; set; }
        public Livreur? Livreur { get; set; }
        public Avis? Avis { get; set; }
    }
}