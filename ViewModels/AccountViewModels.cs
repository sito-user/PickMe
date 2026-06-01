using System.ComponentModel.DataAnnotations;

namespace DeliveryWeb.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Ce champ est requis")]
        public string Identifiant { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [DataType(DataType.Password)]
        public string MotDePasse { get; set; } = string.Empty;

        public string TypeLogin { get; set; } = "telephone"; // "telephone" ou "email"
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Le nom est requis")]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est requis")]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le téléphone est requis")]
        public string Telephone { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Email invalide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [MinLength(8, ErrorMessage = "Minimum 8 caractères")]
        [DataType(DataType.Password)]
        public string MotDePasse { get; set; } = string.Empty;

        [Required(ErrorMessage = "Veuillez confirmer le mot de passe")]
        [Compare("MotDePasse", ErrorMessage = "Les mots de passe ne correspondent pas")]
        [DataType(DataType.Password)]
        public string Confirmation { get; set; } = string.Empty;
    }
}