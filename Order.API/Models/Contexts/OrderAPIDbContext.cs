using Microsoft.EntityFrameworkCore;

namespace Order.API.Models.Contexts
{
    public class OrderAPIDbContext :DbContext
    {
        public OrderAPIDbContext(DbContextOptions options) : base(options)
        {
        }

        protected OrderAPIDbContext()
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
    }
}
