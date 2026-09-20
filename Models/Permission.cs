namespace ContractorHub.Models
{
    public class Permission
    {
        public int Id { get; set; }
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string Code { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.StringLength(200)]
        public string Name { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.StringLength(100)]
        public string Category { get; set; } = string.Empty;
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
