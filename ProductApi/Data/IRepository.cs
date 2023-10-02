using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductApi.Data
{
    public interface IRepository<T>
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetAsync(int id);
        Task<T> AddAsync(T entity);
        Task EditAsync(T entity);
        Task RemoveAsync(int id);
    }
}
