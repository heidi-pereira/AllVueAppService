import { createBrowserRouter, createRoutesFromElements, Route, RouterProvider } from "react-router-dom";
import MainLayout from "../Template/MainLayout";
import RouterError from "./RouterError";
import useGlobalDetailsStore from "@model/globalDetailsStore";
import QuestionPage from "../Pages/QuestionPage/QuestionPage";
import AnalysisLayout from "../Pages/AnalysisLayout";
import ConfigurationPage from "../Pages/ConfigurationPage/ConfigurationPage";
import AnalysisPage from "../Pages/AnalysisPage/AnalysisPage";

const AppRouterProvider = () => {

    const details = useGlobalDetailsStore((state) => state.details);

    if (details.basePath === '') {
        return null;
    }

    const router = createBrowserRouter(createRoutesFromElements(
        <Route path='/' element={<MainLayout />}>
            <Route path="" element={<div>No survey identifier passed. Please use the format: /survey/:surveyId</div>} />
            <Route path="survey/:surveyId">
                <Route path="" element={<QuestionPage />} />
                <Route path="question/:questionId" element={<AnalysisLayout />}>
                    <Route path="themes" element={<AnalysisPage />} />
                    <Route path="configuration" element={<ConfigurationPage />} />
                </Route>
            </Route>
            <Route path="admin" element={<div>Admin</div>} errorElement={<RouterError />} />,
        </Route>), {
        // If hosted as virtual app under {org}.all-vue.com/{openends}
        basename: details.basePath
    });

    return <RouterProvider router={router} />
}

export default AppRouterProvider;