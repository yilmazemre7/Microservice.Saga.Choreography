using Microsoft.EntityFrameworkCore;

namespace Order.API.Models.Contexts
{
    public class OrdeAPIDbContext :DbContext
    {
        public OrdeAPIDbContext(DbContextOptions options) : base(options)
        {
        }

        protected OrdeAPIDbContext()
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
    }
}
