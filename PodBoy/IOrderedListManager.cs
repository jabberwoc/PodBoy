using System.Threading.Tasks;
using PodBoy.Entity;

namespace PodBoy
{
    public interface IOrderedListManager<in T> where T : IHasOrder
    {
        Task<int> MoveItemToIndex(T source, int index);
    }
}