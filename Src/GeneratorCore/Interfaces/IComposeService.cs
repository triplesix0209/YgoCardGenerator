using System.Threading.Tasks;
using GeneratorCore.Dto;
using TripleSix.Core.Services;

namespace GeneratorCore.Interfaces
{
    public interface IComposeService : IService
    {
        Task Write(ComposeDataDto input, string outputFilename);
    }
}
