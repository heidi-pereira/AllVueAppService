import React from "react";
import { CatchReportAndDisplayErrors } from "../../../components/CatchReportAndDisplayErrors";
import { ProductConfiguration } from "../../../ProductConfiguration";
import { ApplicationConfiguration } from "../../../ApplicationConfiguration";
import { Factory, IApplicationUser, WebFileFileInformation } from "../../../BrandVueApi";
import AllVueWebAdminControl from "./AllVueWebAdminControl";



interface IAllVueWebPage {
    applicationConfiguration: ApplicationConfiguration;
    productConfiguration: ProductConfiguration;
    name: string;
    user: IApplicationUser|null;
}

const AllVueWebPage = (props: IAllVueWebPage) => {
    const [isGettingData, setIsGettingData] = React.useState(false);
    const [documents, setDocuments] = React.useState<WebFileFileInformation[]>();
    const [masterHtml, setMasterHtml] = React.useState<WebFileFileInformation>();
    const [htmlLoaded, setHtmlLoaded] = React.useState<string>("");
    const [forceReload, setForceReload] = React.useState<number>(0);

    const loadData = (url) => {
        fetch(url)
            .then(function (response) {
                if (response.ok) {
                    return response.text();
                }
                else {
                    setHtmlLoaded("");
                }
            })
            .then(function (data) {
                setHtmlLoaded(data);

            }.bind(this))
            .catch(function (err) {
                console.log("Failed to load the data");
                setHtmlLoaded("");
            }).finally
        {
            setIsGettingData(false);
        }
    }

    React.useEffect(() => {
        const client = Factory.AllVueWebPageClient(error => {
            setIsGettingData(false);
        });
        setIsGettingData(true);
        client.getFiles(props.name).then(documentsForPath => {
            setDocuments(documentsForPath);

            const htmlDocs = documentsForPath?.filter(x => x.name.endsWith(".html"));
            if (htmlDocs && htmlDocs.length >= 1) {
                setMasterHtml(htmlDocs[0]);
                loadData(htmlDocs[0].url);
            }
            else {
                setHtmlLoaded("");
                setIsGettingData(false);
            }
        });
    }, [props.name, forceReload]);

    const onFileReload = () => {
        setForceReload(forceReload + 1);
    }

    const getPageContent = () => {
        if (isGettingData) {
            return <div>Getting data</div>
        }

        return (
            <CatchReportAndDisplayErrors applicationConfiguration={props.applicationConfiguration} childInfo={{ "Component": "UsersSettingsPage" }}>
                {props.user?.isSystemAdministrator &&
                    <AllVueWebAdminControl 
                    productConfiguration={props.productConfiguration}
                    name={props.name}
                    user={props.user}
                    documents={documents}
                    masterHtml={masterHtml}
                    htmlLoaded={htmlLoaded}
                    path={props.name}
                    onFileReload={onFileReload}
                />
                }
                {!props.user?.isSystemAdministrator &&
                    <div dangerouslySetInnerHTML={{ __html: htmlLoaded }} />
                }
            </CatchReportAndDisplayErrors>
            );
    }
    return (getPageContent());
}

export default AllVueWebPage;
