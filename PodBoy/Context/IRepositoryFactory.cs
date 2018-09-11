namespace PodBoy.Context
{
    public interface IRepositoryFactory
    {
        IPodboyRepository Create();
    }
}