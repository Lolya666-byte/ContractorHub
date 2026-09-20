namespace ContractorHub.Models
{
    public sealed class PermissionMatrixViewModel
    {
        public List<string> Roles { get; set; } = new();
        public List<PermissionMatrixRowViewModel> Permissions { get; set; } = new();
    }

    public sealed class PermissionMatrixRowViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public Dictionary<string, bool> Granted { get; set; } = new();
    }
}
