using System.Threading.Tasks;
using GeneratorCore.Dto;
using TripleSix.Core.Services;

namespace GeneratorCore.Services
{
    public interface ICardComposeService : IService
    {
        Task Write(CardComposeDataDto input, string fileName);
    }
}
