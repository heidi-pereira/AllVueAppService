
type RoleWithDescription = {
    name: string;
    displayName?: string;
}
const predefinedRoles: RoleWithDescription[] = [
  {
    name: 'SystemAdministrator',
    displayName: 'System Administrator',
  },
  {
      name: 'Administrator',
  },
  {
     name: 'User',
  },
  {
      name: 'ReportViewer',
      displayName: 'Report Viewer',
  }
];

export const isRoleReadOnly = (roleName: string): boolean => {
    const role = predefinedRoles.find(r => r.name === roleName || roleName === r.displayName);
    return role != undefined;
};

export const displayRoleName = (roleName: string): string => {
    const role = predefinedRoles.find(r => r.name === roleName);
    if (role && role.displayName) {
        return role.displayName;
    }
    return roleName;
};


