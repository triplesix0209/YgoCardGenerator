using System.Threading.Tasks;
using GeneratorCore.Dto;
using TripleSix.Core.Services;

namespace GeneratorCore.Interfaces
{
    public interface IComposeService : IService
    {
        Task Compose(CardModelDto model, string outputPath, CardSetDto setConfig);

        Task Compose(CardDataDto data, string outputPath, CardSetDto setConfig);
    }
}
