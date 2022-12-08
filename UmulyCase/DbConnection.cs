namespace UmulyCase
{
    public class DbConnection
    {
        public  string ConnectionString { get; set; } = string.Empty;
        public DbConnection(string option)
        {
            this.ConnectionString = option;
        }
    }
}
