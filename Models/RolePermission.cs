namespace ContractorHub.Models
{
    public class RolePermission
    {
        public int Id { get; set; }
        [System.ComponentModel.DataAnnotations.StringLength(50)]
        public string Role { get; set; } = string.Empty;
        public int PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;
    }
}
