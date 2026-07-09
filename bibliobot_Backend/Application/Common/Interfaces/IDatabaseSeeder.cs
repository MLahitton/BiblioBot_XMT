using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface IDatabaseSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
