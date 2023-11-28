using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YgoCardGenerator.Persistences.Entities
{
    [Table("setcodes")]
    [Keyless]
    public class Setcode
    {
        [Column("officialcode")]
        public int? OfficialCode { get; set; }

        [Column("betacode")]
        public int? BetaCode { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("cardid")]
        public int? CardId { get; set; }
    }
}
