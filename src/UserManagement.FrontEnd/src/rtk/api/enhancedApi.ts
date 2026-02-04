import { userManagementApi } from '../apiSlice';

const enhancedApi = userManagementApi.enhanceEndpoints({
  addTagTypes: ["Project", "Company", "Roles", "DataGroup", "User"],
  endpoints: {
      getApiProjectsByCompanyAndProjectTypeProjectId: {
      providesTags: (result, error, arg) => [
              {
                  type: "Project", company: arg.company,
                  projectType: arg.projectType,
                  projectId: arg.projectId,
              },
      ],
    },
    postApiProjectsByCompanyAndProjectTypeProjectIdSetshared: {
      invalidatesTags: (result, error, arg) => !error ? ["Project",
          {
              type: "Project",
              company: arg.company,
              projectType: arg.projectType,
                projectId: arg.projectId,
            },
            "User",
            "DataGroup",
            {
                type: "DataGroup",
                company: arg.company,
                projectType: arg.projectType,
                projectId: arg.projectId,
            },
      ] : [],
    },
    getApiProjects: {
        providesTags: (result) => {
            if (!result) return ["Project"];
            return [
                ...result.map(project => ({
                    type: "Project",
                    company: project.companyId,
                    projectType: project.projectId.type,
                    projectId: project.projectId.id,
                })),
                "Project"
            ];
        },
    },
    getApiCompaniesByCompanyIdAncestornames: {
      providesTags: (result, error, arg) => [
        { type: "Company", id: arg.companyId },
      ],
    },
    getApiRoles: {
        providesTags: ["Roles"],
    },
    getApiRolesByCompanyId: {
        providesTags: (result, error, arg) => [
            { type: "Roles", id: arg.companyId },
        ],
    },
    postApiRoles: {
        invalidatesTags: ["Roles"],
    },
    putApiRolesById: {
        invalidatesTags: ["Roles"],
    },
    postApiUsersdatapermissionsAdddatagroup: {
      invalidatesTags: (result, error, arg) => !error ? [
            "User",
            "DataGroup",
            { type: "DataGroup",
              company: arg.dataGroup.company,
              projectType: arg.dataGroup.projectType,
              projectId: arg.dataGroup.projectId,
            },
            {
                type: "Project",
                company: arg.dataGroup.company,
                projectType: arg.dataGroup.projectType,
                projectId: arg.dataGroup.projectId,
            },
      ] : [],
    },

    postApiUsersdatapermissionsUpdatedatagroup: {
      invalidatesTags: (result, error, arg) => !error ? [
          "User",
          { type: "DataGroup",
            company: arg.dataGroup.company,
            projectType: arg.dataGroup.projectType,
            projectId: arg.dataGroup.projectId, },
          { type: "DataGroup", id: `${arg.dataGroup.id}` },
            {
                type: "Project",
                company: arg.dataGroup.company,
                projectType: arg.dataGroup.projectType,
                projectId: arg.dataGroup.projectId, },
      ] : [],
    },
    getApiUsersdatapermissionsGetdatagroupsByCompanyAndProjectTypeProjectId: {
      providesTags: (result, error, arg) => {
        const tags = result ? result.map(dataGroup => ({ type: "DataGroup" as const, id: `${dataGroup.id}` })) : [];
            return tags.concat([{
                type: "DataGroup",
                company: arg.company,
                projectType: arg.projectType,
                projectId: arg.projectId,
            }]);
      },
    },
    getApiUsersdatapermissionsGetdatagroupById: {
      providesTags: (result, error, arg) => [
        { type: "DataGroup", id: `${arg.id}` },
      ],
    },
    deleteApiUsersdatapermissionsDeleteallvueruleById: {
      invalidatesTags: (result, error, arg) => !error ? [
        "User",
        { type: "DataGroup", id: `${arg.id}` },
        {
            type: "DataGroup",
            company: arg.company,
            projectType: arg.projectType,
            projectId: arg.projectId,
        },
        {
            type: "Project",
            company: arg.company,
            projectType: arg.projectType,
            projectId: arg.projectId,
        }
      ] : [],
    },
    // User-related cache invalidation
    getApiUsersGetusers: {
      providesTags: ["User"],
    },
    getApiUsersGetcompanies: {
      providesTags: ["Company"],
    },
    getApiUsersGetprojects: {
      providesTags: ["Project"],
    },
    postApiUserAddUser: {
      invalidatesTags: ["User"],
    },
    postApiUser: {
      invalidatesTags: ["User"],
    },
    deleteApiUserDeleteByUserId: {
      invalidatesTags: ["User"],
    },
  },
});

export { enhancedApi as userManagementApi };