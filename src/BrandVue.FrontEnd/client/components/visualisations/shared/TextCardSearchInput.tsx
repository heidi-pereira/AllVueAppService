import React from 'react';
import Clipboard from 'react-clipboard.js';
import toast from 'react-hot-toast';

import SearchInput from "../../SearchInput";
import { IGoogleTagManager } from '../../../googleTagManager';
import { PageHandler } from '../../PageHandler';
import AilaSummariseButton from '../../buttons/AilaSummariseButton'

interface ITextCardSearchInput {
    handleSearchInput?(text: string): void;
    googleTagManager?: IGoogleTagManager;
    results?: string[];
    filteredResults?: string[];
    pageHandler?: PageHandler;
    onSummariseComplete?: (summary: string) => void;
    noNewDataForSummarise?: boolean;
    isLoading?: boolean;
}

const TextCardSearchInput = (props: ITextCardSearchInput) => {
    const {pageHandler, googleTagManager, handleSearchInput = () => {}, filteredResults, results, isLoading} = props;
    const [searchQuery, setSearchQuery] = React.useState("");

    const handleClipboardSuccess = () => {
        pageHandler && googleTagManager?.addEvent("reportsPageCopyTextToClipboard", pageHandler);
        toast.success("Text responses copied to clipboard");
    }

    const handleClipboardError = (e: ClipboardJS.Event) => {
        pageHandler && googleTagManager?.addEvent("reportsPageCopyTextToClipboardFailed", pageHandler);
        toast.error("Unable to copy to clipboard, please try again");
        console.error(`Failed to copy to clipboard: ${e.text}`);
    }

    return (
        <div className="text-card-search-input-container">
           <div className="question-search-container">
                <SearchInput 
                    id="question-search"
                    text={searchQuery}
                    onChange={(text) => { handleSearchInput(text); setSearchQuery(text) }} 
                    className="question-search-input-group"
                />
            </div>
            <div className="text-card-search-input-button-container">
                <AilaSummariseButton
                    results={results}
                    onSummariseComplete={props.onSummariseComplete}
                    loading={isLoading}
                    disabled={props.noNewDataForSummarise && (results?.length ?? 0) > 0} />
                <Clipboard 
                    className="hollow-button" 
                    component="button" 
                    data-clipboard-text={filteredResults?.join("\r\n")} 
                    onSuccess={handleClipboardSuccess} 
                    onError={handleClipboardError}
                >
                    <i className="material-symbols-outlined">content_copy</i> 
                    <span>Copy to clipboard</span>
                </Clipboard>  
            </div>
        </div>
    )
}

export default TextCardSearchInput;