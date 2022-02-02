using System.Threading.Tasks;
using TripleSix.Core.Services;

namespace GeneratorCore.Services
{
    public interface ICardComposeService : IService
    {
        Task Write(string fileName);
    }
}
