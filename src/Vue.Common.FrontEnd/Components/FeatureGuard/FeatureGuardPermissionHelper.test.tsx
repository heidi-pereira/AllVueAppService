import {featureGuardHasPermission, IFeatureGuardUser} from './FeatureGuardPermissionHelper';

describe('featureGuardHasPermission', () => {
  it('returns false when user is undefined', () => {
    const isAllowed = featureGuardHasPermission(undefined, ['READ']);
    expect(isAllowed).toBe(false);
  });

  it('returns true when no permissions are specified', () => {
    const user: IFeatureGuardUser = { featurePermissions: [{ code: 'READ', name: 'Read Access' }] };
    const isAllowed = featureGuardHasPermission(user);
    expect(isAllowed).toBe(true);
  });

  it('returns true when user has permission', () => {
    const user: IFeatureGuardUser = { featurePermissions: [{ code: 'READ', name: 'Read Access' }] };
    const isAllowed = featureGuardHasPermission(user, ['READ']);
    expect(isAllowed).toBe(true);
  });

  it('returns false when user does not have permission', () => {
    const user: IFeatureGuardUser = { featurePermissions: [{ code: 'WRITE', name: 'Write Access' }] };
    const isAllowed = featureGuardHasPermission(user, ['READ']);
    expect(isAllowed).toBe(false);
  });

  it('returns true when user has at least one of multiple permissions', () => {
    const user: IFeatureGuardUser = { featurePermissions: [{ code: 'WRITE', name: 'Write Access' }] };
    const isAllowed = featureGuardHasPermission(user, ['READ', 'WRITE']);
    expect(isAllowed).toBe(true);
  });

  it('returns false when user has none of multiple permissions', () => {
    const user: IFeatureGuardUser = { featurePermissions: [{ code: 'DELETE', name: 'Delete Access' }] };
    const isAllowed = featureGuardHasPermission(user, ['READ', 'WRITE']);
    expect(isAllowed).toBe(false);
  });

  it('returns false when user has some permissions and match is set to "all"', () => {
    const user: IFeatureGuardUser = { featurePermissions: [{ code: 'READ', name: 'Read Access' }] };
    const isAllowed = featureGuardHasPermission(user, ['READ', 'WRITE'], 'all');
    expect(isAllowed).toBe(false);
  });

  it('returns true when user has some permissions and match is set to "any"', () => {
    const user: IFeatureGuardUser = { featurePermissions: [{ code: 'READ', name: 'Read Access' }, { code: 'WRITE', name: 'Write Access' }] };
    const isAllowed = featureGuardHasPermission(user, ['READ', 'WRITE'], 'any');
    expect(isAllowed).toBe(true);
  });

  it('returns value of customCheck when provided', () => {
    const user: IFeatureGuardUser = { featurePermissions: [{ code: 'READ', name: 'Read Access' }] };
    const customCheck = jest.fn().mockReturnValue(false);
    const isAllowed = featureGuardHasPermission(user, ['READ'], 'any', customCheck);
    expect(isAllowed).toBe(false);
  });

  it('calls customCheck with user and result of permission check when supplied', () => {
    const user: IFeatureGuardUser = { featurePermissions: [{ code: 'READ', name: 'Read Access' }] };
    const customCheck = jest.fn().mockReturnValue(true);
    featureGuardHasPermission(user, ['READ'], 'any', customCheck);
    expect(customCheck).toHaveBeenCalledWith(user, true);
  });
});