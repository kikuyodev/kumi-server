using System.ComponentModel.DataAnnotations.Schema;

namespace Kumi.Server.Database.Models;

[Table("chat_channels")]
public class ChatChannel
{
    [Column("id")]
    public int Id { get; set; }
}
