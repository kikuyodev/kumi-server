using System.ComponentModel.DataAnnotations.Schema;

namespace Kumi.Server.Database.Models;

[Table("accounts")]
public class Account
{
    [Column("id")]
    public int Id { get; set; }
    
    [Column("username")]
    public string Username { get; set; } = null!;
}
