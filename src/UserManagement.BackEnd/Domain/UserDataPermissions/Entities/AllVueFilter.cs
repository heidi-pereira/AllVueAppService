namespace UserManagement.BackEnd.Domain.UserDataPermissions.Entities
{
    public class AllVueFilter(int id, int variableConfigurationId, int entitySetId, int[] entityIds)
    {
        public int Id { get; private set; } = id;

        public int VariableConfigurationId { get; private set; } = variableConfigurationId;

        public int EntitySetId { get; private set; } = entitySetId;

        public int[] EntityIds { get; private set; } = entityIds;
        protected AllVueFilter() : this(0, 0, 0, Array.Empty<int>())
        {
        }
    }
}