using System.Reactive;
using ReactiveUI;

namespace PodBoy
{
    public interface IPlayableViewModel
    {
        ReactiveCommand<IPlayable, Unit> PlayItemCommand { get; }
    }
}