using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YgoCardGenerator.Persistences.Entities
{
    [Table("texts")]
    public class Text
    {
        [Column("id")]
        [Key]
        public int Id { get; set; }

        [Column("name")]
        public string? Name { get; set; }

        [Column("desc")]
        public string? Desc { get; set; }

        [Column("str1")]
        public string? Str1 { get; set; }

        [Column("str2")]
        public string? Str2 { get; set; }

        [Column("str3")]
        public string? Str3 { get; set; }

        [Column("str4")]
        public string? Str4 { get; set; }

        [Column("str5")]
        public string? Str5 { get; set; }

        [Column("str6")]
        public string? Str6 { get; set; }

        [Column("str7")]
        public string? Str7 { get; set; }

        [Column("str8")]
        public string? Str8 { get; set; }

        [Column("str9")]
        public string? Str9 { get; set; }

        [Column("str10")]
        public string? Str10 { get; set; }

        [Column("str11")]
        public string? Str11 { get; set; }

        [Column("str12")]
        public string? Str12 { get; set; }

        [Column("str13")]
        public string? Str13 { get; set; }

        [Column("str14")]
        public string? Str14 { get; set; }

        [Column("str15")]
        public string? Str15 { get; set; }

        [Column("str16")]
        public string? Str16 { get; set; }
    }
}
