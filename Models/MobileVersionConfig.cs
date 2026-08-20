namespace ParrotsAPI2.Models
{
    public class MobileVersionConfig
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool ForceUpdate { get; set; } = false;
    }
}
