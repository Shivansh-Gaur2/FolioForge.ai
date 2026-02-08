using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FolioForge.Domain.Entities
{
    // This would be my base entity that would be keeping the properties
    //that all entities would be inheriting from. I would also be adding some common methods here that all entities would be using.
    // abstract class because we don't want to create an instance of this class, it's just a base for other entities to inherit from.
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        // Using DateTime for now may change it to DateTimeOffset in the future
        // if we need to handle time zones, but for now DateTime should be sufficient for our needs.
        public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; protected set; }
    }
}
