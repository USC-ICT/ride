using System.Threading.Tasks;

namespace NonverbalBehaviorGenerator.Models
{
    public interface IContextFactory
    {
        Task<IContext> CreateContextAsync();
    }
}
