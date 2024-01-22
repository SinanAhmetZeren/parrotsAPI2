using Microsoft.EntityFrameworkCore;

namespace ParrotsAPI2.Data
{
    public class DataContext  : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
        
        public DbSet<Character> Characters => Set<Character>();

    }
}
