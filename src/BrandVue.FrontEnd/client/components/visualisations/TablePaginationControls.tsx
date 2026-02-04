import React from "react";
import {PaginationData} from "./PaginationData";
import style from "./TablePagination.module.less";

interface ITablePaginationControls {
    currentPaginationData: PaginationData;
    setPagination: (pageNo: number, noOfTablesPerPage: number, totalNoOfTables: number) => void;
    maxNoOfTablesPerPage: number;
}

const TablePaginationControls = (props: ITablePaginationControls) => {
    const getTotalNoOfPages = () => {
        return Math.ceil(props.currentPaginationData.totalNoOfTables / props.currentPaginationData.noOfTablesPerPage)
    }
    
    const getPageSelector = () => {
        return (
            <div className={style.pageSelector}>
                <b className={`${style.arrowButton} ${props.currentPaginationData.currentPageNo <= 1 ? style.greyedOut : ""}`} onClick={() => {props.setPagination(props.currentPaginationData.currentPageNo - 1, props.currentPaginationData.noOfTablesPerPage, props.currentPaginationData.totalNoOfTables)}}>
                    <i className="material-symbols-outlined">chevron_left</i>
                </b>
                <div>
                    {`Page `}<b>{props.currentPaginationData.currentPageNo}</b>{` of `}<b>{getTotalNoOfPages()}</b>
                </div>
                <b className={`${style.arrowButton} ${props.currentPaginationData.currentPageNo >= getTotalNoOfPages() ? style.greyedOut : ""}`} onClick={() => {props.setPagination(props.currentPaginationData.currentPageNo + 1, props.currentPaginationData.noOfTablesPerPage, props.currentPaginationData.totalNoOfTables)}}>
                    <i className="material-symbols-outlined">chevron_right</i>
                </b>
            </div>
        )
    }
    
    const getStartIndex = () => {
        return ((props.currentPaginationData.currentPageNo - 1) * props.currentPaginationData.noOfTablesPerPage) + 1;
    }
    
    const getEndIndex = () => {
        const endIndex = props.currentPaginationData.currentPageNo * props.currentPaginationData.noOfTablesPerPage
        return endIndex < props.currentPaginationData.totalNoOfTables ? endIndex : props.currentPaginationData.totalNoOfTables;
    }
    
    const getDescriptor = () => {
        return (
            <div>
                {`Showing ${getStartIndex()} - ${getEndIndex()} of ${props.currentPaginationData.totalNoOfTables} tables`}
            </div>
        );
    }

    const getPagination = () => {
        return (
            <div className={style.reportTablePagination}>
                {getPageSelector()}
                {getDescriptor()}
            </div>
        );
    }
    
    return <>
        {props.currentPaginationData.totalNoOfTables > props.maxNoOfTablesPerPage && getPagination()}
    </>
}

export default TablePaginationControls