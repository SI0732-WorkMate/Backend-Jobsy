using System.ComponentModel.DataAnnotations;
using Jobsy.UserAuthentication.Domain.Model.ValueObjects;

namespace Jobsy.UserAuthentication.Domain.Model.Aggregates;

public class User
{
    [Key]
    public int id { get; set; }
    
    [Required]
    [StringLength(35)]
    public string name { get; set; }
    
    [Required]
    public string email { get; set; }
    
    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [MaxLength(500)]
    public string password { get; set; }
    
    public Rol role { get; set; }
    
    [StringLength(50, ErrorMessage = "maximo 50 caracteres")]
    public string description  { get; set; }

    [StringLength(11, MinimumLength = 11, ErrorMessage = "El RUC debe tener 11 digitos")]
    public string? ruc { get; set; }

    public string? cv_url { get; set; }

    public string? cv_pdf_base64 { get; set; }

    public bool vacancy_notifications_enabled { get; set; } = false;

    [StringLength(300)]
    public string? vacancy_notification_keywords { get; set; }
    
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    
}
