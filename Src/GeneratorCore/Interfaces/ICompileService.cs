using System.Threading.Tasks;
using GeneratorCore.Dto;
using TripleSix.Core.Services;

namespace GeneratorCore.Interfaces
{
    public interface ICompileService : IService
    {
        Task Compile(string outputPath, CardSetDto setConfig);
    }
}
