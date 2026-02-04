import Grid from "@mui/material/Grid";
import useGlobalDetailsStore from "@model/globalDetailsStore";
import Box from "@mui/material/Box";
import Header from "./Header/Header";
import { Outlet, useParams } from "react-router-dom";
import { useEffect } from "react";
import { ISurveyContext } from "./Header/ISurveyContext";
import { FeatureGuard } from "../components/FeatureGuard";
import { PermissionFeaturesOptions } from "@/orval/api/models/permissionFeaturesOptions";
import { Typography } from "@mui/material";

const NoPermissionMessage = () => (
    <Box
        display="flex"
        flexDirection="column"
        alignItems="center"
        justifyContent="center"
        textAlign="center"
        marginTop={20}
        px={2}
    >
        <Typography
            variant="body1"
            fontWeight={500}
            mb={3}>
            You do not have permission to view this content.
        </Typography>

        <Typography variant="body1" fontWeight={500}>
            Speak to your administrator if you think this is a mistake.
        </Typography>
    </Box>
);

const MainLayout = () => {
    const { surveyId } = useParams();
    const globalDetails = useGlobalDetailsStore((state) => state.details);
    const user = globalDetails.user;

    const surveyContext: ISurveyContext = {
        id: surveyId ? surveyId : '',
        name: globalDetails.surveyName,
        availableTabs: globalDetails.navigationTabs,
        customUiIntegrations: globalDetails.customUiIntegrations
    }

    useEffect(() => {
        if (surveyId) {
            useGlobalDetailsStore.getState().fetchGlobalDetails(surveyId);
        }
    }, [surveyId]);


    const renderContent = () => {
        if (!user.userName) {
            return null;
        }

        return (
            <FeatureGuard
                permissions={[PermissionFeaturesOptions.AnalysisAccess]}
                customCheck={(userContext, isAuthorized) =>
                    ((isAuthorized && (userContext.featurePermissions?.length ?? 0) > 0) || userContext.isAdministrator)
                }
                fallback={<NoPermissionMessage />}
            >
                <Outlet />
            </FeatureGuard>
        );
    };

    return <Grid container>
        <Grid item xs={12}>
            <Header user={user} associatedSurvey={surveyContext} />
        </Grid>

        <Grid item xs={12}>
            <Box padding={3}>
                {renderContent()}
            </Box>
        </Grid>
    </Grid>
}

export default MainLayout;