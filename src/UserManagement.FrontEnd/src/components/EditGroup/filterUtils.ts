import { Filter, FilterOption } from "./GroupFiltersDialog";
import { AllVueFilter } from "@/rtk/apiSlice";
import { toast } from "mui-sonner";
import { ProjectType } from "./orval/api/models/ProjectType";
import { userManagementApi as api } from "@/rtk/api/enhancedApi";
/**
 * Builds an array of AllVueFilter from filters and optionally a specific filter/options.
 */
export function buildSelectedVariablesWithOptions(
  filters: Array<Filter>,
  originalFilter?: Filter,
  options?: FilterOption[]
): AllVueFilter[] {
  const result: AllVueFilter[] = filters
    .filter(f => f.options.some(o => o.isSelected) && (!originalFilter || f.name !== originalFilter.name))
    .map(f => ({
      id: 0,
      variableConfigurationId: Number(f.id),
      entitySetId: Number(f.id),
      entityIds: f.options.filter(o => o.isSelected).map(o => Number(o.id))
    }));

  if (originalFilter && options) {
    const selectedEntityIds = options.filter(o => o.isSelected).map(o => Number(o.id));
    if (selectedEntityIds.length > 0) {
      result.push({
        id: 0,
        variableConfigurationId: Number(originalFilter.id),
        entitySetId: Number(originalFilter.id),
        entityIds: selectedEntityIds
      });
    }
  }

  return result;
}

export function useUpdateResponseFilterCount() {
  const [getResponseCount] = api.usePostApiProjectsByCompanyIdAndProjectTypeProjectIdFilterMutation();

  return async (
    params: { company?: string; projectType?: string; projectId?: string | number },
    selectedFilters: AllVueFilter[],
    setCount: (count: number) => void,
    setLoading: (loading: boolean) => void,
    setCountError: (error: string) => void,
    totalCountOfRespondents: number
  ) => {
    setCount(0);
    setLoading(true);
    setCountError("");
    if (selectedFilters.length) {
      try {
        const result = await getResponseCount({
          companyId: params.company as string,
          projectType: params.projectType as ProjectType,
          projectId: Number(params.projectId),
          body: selectedFilters
        }).unwrap();
        setCount(result);
      } catch (err) {
        toast.error("Failed to generate response count. " + (err?.data?.error || "Unknown error"));
        setCountError("Failed to generate response count.");
      } finally {
        setLoading(false);
      }
    } else {
      setCount(totalCountOfRespondents);
      setLoading(false);
    }
  };
}