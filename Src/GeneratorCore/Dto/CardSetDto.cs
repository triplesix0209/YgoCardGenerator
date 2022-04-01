using TripleSix.Core.Dto;

namespace GeneratorCore.Dto
{
    public class CardSetDto : DataDto
    {
        public string BasePath { get; set; }

        public string[] Marcos { get; set; }

        public string[] Packs { get; set; }

        public bool ComposeSilence { get; set; } = false;
    }
}
