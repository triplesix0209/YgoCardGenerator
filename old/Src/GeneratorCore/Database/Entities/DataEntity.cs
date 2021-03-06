using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TripleSix.Core.Entities;

namespace GeneratorCore.Database.Entities
{
    [Table("datas")]
    public class DataEntity : BaseEntity<DataEntity>
    {
        [Column("id")]
        [Key]
        public int? Id { get; set; }

        [Column("ot")]
        public int? Ot { get; set; }

        [Column("alias")]
        public int? Alias { get; set; }

        [Column("setcode")]
        public int? SetCode { get; set; }

        [Column("type")]
        public int? Type { get; set; }

        [Column("atk")]
        public int? Atk { get; set; }

        [Column("def")]
        public int? Def { get; set; }

        [Column("level")]
        public int? Level { get; set; }

        [Column("race")]
        public int? Race { get; set; }

        [Column("attribute")]
        public int? Attribute { get; set; }

        [Column("category")]
        public int? Category { get; set; }
    }
}
