using MediatR;
using Vue.Common.Auth.Permissions;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetRolesByCompany;

public class GetRolesByCompanyQuery : IRequest<IEnumerable<RoleDto>>
{
    public string CompanyId { get; }

    public GetRolesByCompanyQuery(string companyId)
    {
        CompanyId = companyId;
    }
}
