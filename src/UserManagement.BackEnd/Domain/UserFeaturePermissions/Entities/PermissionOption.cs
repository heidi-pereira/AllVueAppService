namespace UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities
{
    public class PermissionOption
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public PermissionFeature Feature { get; private set; }

        public PermissionOption(string name, PermissionFeature feature) 
        {
            Name = name;
            Feature = feature;
        }

        public PermissionOption(int id, string name, PermissionFeature feature)
        {
            Id = id;
            Name = name;
            Feature = feature;
        }
    }
}