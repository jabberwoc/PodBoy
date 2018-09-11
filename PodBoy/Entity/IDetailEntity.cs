using System;

namespace PodBoy.Entity
{
    public interface IDetailEntity : IEntity
    {
        string Title { get; }
        string Description { get; }
        Uri ImageUri { get; }
    }
}