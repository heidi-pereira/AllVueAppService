# streamlit_app.py

import streamlit as st
import numpy as np
import pandas as pd
import altair as alt
from snowflake.snowpark.context import get_active_session
from datetime import datetime, timezone

# Set page configuration for a wide layout and a title
st.set_page_config(layout="wide", page_title="Replication Sync Dashboard")

# --- SQL Queries ---

# This is the main query to get the LATEST status for each table.
# It handles the special join condition and calculates key metrics.
# Using QUALIFY is highly efficient in Snowflake for "greatest-n-per-group".
LATEST_STATUS_QUERY = """
WITH latest_sync_data AS (
    SELECT
        TO_TIMESTAMP_NTZ(sm.MONITORING_TIMESTAMP_UTC, 9) AS MONITORING_TIMESTAMP,
        sm.MSSQL_TABLE_NAME,
        sm.SNOWFLAKE_TABLE_NAME,
        sm.MSSQL_ROW_COUNT,
        sm.SNOWFLAKE_ROW_COUNT,
        sm.ROW_COUNT_DIFFERENCE,
        ct.LAST_SYNC_TIMESTAMP_UTC,
        ct.STATUS AS SYNC_JOB_STATUS,
        ct.LAST_ERROR_MESSAGE
    FROM
        AIRFLOW.SYNC_MONITORING AS sm
    LEFT JOIN
        AIRFLOW.CT_TIDEMARKS AS ct
        ON ct.TABLE_KEY = CASE
                              WHEN sm.MSSQL_TABLE_NAME = 'SurveyPortalMorar.vue.answers' THEN 'vue.AnswersSnowflakeSync'
                              ELSE sm.MSSQL_TABLE_NAME
                         END
    QUALIFY ROW_NUMBER() OVER (PARTITION BY sm.MSSQL_TABLE_NAME ORDER BY sm.MONITORING_TIMESTAMP_UTC DESC) = 1
)
SELECT * FROM latest_sync_data;
"""

ANSWERS_SYNC_QUERY = """
SELECT 
    SUM(CASE WHEN import_count IS NULL THEN 1 ELSE 0 END) AS missing_responses, 
    SUM(CASE WHEN answer_count IS NOT NULL AND answer_count <>import_count THEN 1 ELSE 0 END) AS mismatched_responses
FROM 
    live__vue.airflow.response_counts_stats;
"""

# --- Data Loading Functions ---

# Use Streamlit's cache to avoid re-running the query on every interaction.
# The data will be cached for 10 minutes (600 seconds).
@st.cache_data(ttl=600)
def load_latest_status(_session):
    """
    Fetches the latest sync status and calculates latency using Pandas
    for maximum reliability and control over timezones.
    """
    df = session.sql(LATEST_STATUS_QUERY).to_pandas()

    if df.empty:
        return df

    now_utc = datetime.now(timezone.utc)
    df['LAST_SYNC_TIMESTAMP_UTC'] = pd.to_datetime(df['LAST_SYNC_TIMESTAMP_UTC'])
    df['LAST_SYNC_TIMESTAMP_UTC'] = df['LAST_SYNC_TIMESTAMP_UTC'].dt.tz_localize('UTC')
    latency = now_utc - df['LAST_SYNC_TIMESTAMP_UTC']
    df['LATENCY_SECONDS'] = latency.dt.total_seconds()
    df['SYNC_STATUS'] = df['ROW_COUNT_DIFFERENCE'].apply(lambda x: 'In Sync' if x == 0 else 'Out of Sync')
    
    return df

def load_answers_sync_stats(session):
    """
    Returns missing_responses, mismatched_responses from the Answers sync audit.
    """
    df = session.sql(ANSWERS_SYNC_QUERY).to_pandas()
    if df.empty:
        return 0, 0
    return int(df['MISSING_RESPONSES'][0]), int(df['MISMATCHED_RESPONSES'][0])

def format_latency(total_seconds):
    """Converts a total number of seconds into a human-readable d/h/m/s format."""
    if pd.isna(total_seconds) or not isinstance(total_seconds, (int, float)):
        return "N/A"
    
    total_seconds = abs(int(total_seconds))
    
    if total_seconds < 60:
        return f"{total_seconds}s"
        
    days, remainder = divmod(total_seconds, 86400) # 86400 = 24 * 3600
    hours, remainder = divmod(remainder, 3600)     # 3600 = 60 * 60
    minutes, seconds = divmod(remainder, 60)
    
    if days > 0:
        return f"{days}d {hours}h"
    elif hours > 0:
        return f"{hours}h {minutes}m"
    else:
        return f"{minutes}m {seconds}s"

def format_job_status(status):
    """Converts a job status string into an emoji-prefixed string."""
    if pd.isna(status):
        return "❓"
    
    status_lower = str(status).lower()
    
    if status_lower == 'done':
        return '✅'
    elif status_lower == 'failed':
        return '❌'
    elif status_lower == 'syncing':
        return '⏳'
    elif status_lower == 'pending':
        return '🕒'
    else:
        return f'❓'

# --- Main App Logic ---

st.title("📊 Data Replication Monitoring")
st.markdown("This dashboard displays the synchronization status of tables replicated from MSSQL to Snowflake.")

try:
    session = get_active_session()
except Exception as e:
    st.error(f"Could not connect to Snowflake. Please ensure this app is running as a Streamlit in Snowflake app. Error: {e}")
    st.stop()

main_df = load_latest_status(session)

if main_df.empty:
    st.warning("No data found in the monitoring tables. Please check if the monitoring process is running.")
    st.stop()


# --- Sidebar for Filters ---
st.sidebar.header("Filters")
status_filter = st.sidebar.selectbox(
    "Filter by Sync Status",
    options=['All', 'In Sync', 'Out of Sync'],
    index=0
)
STALE_THRESHOLD_HOURS = 4
is_stale_filter = st.sidebar.checkbox(
    f"Show only stale tables (last sync > {STALE_THRESHOLD_HOURS} hours ago)"
)

# Apply filters
filtered_df = main_df.copy()
if status_filter != 'All':
    filtered_df = filtered_df[filtered_df['SYNC_STATUS'] == status_filter]

if is_stale_filter:
    stale_threshold_seconds = STALE_THRESHOLD_HOURS * 3600
    filtered_df = filtered_df[filtered_df['LATENCY_SECONDS'] > stale_threshold_seconds]


# --- KPI Section ---
st.header("Overall Health")
total_tables = len(main_df)
out_of_sync_count = (main_df['ROW_COUNT_DIFFERENCE'] != 0).sum()
stale_count = (main_df['LATENCY_SECONDS'] > (STALE_THRESHOLD_HOURS * 3600)).sum()

col1, col2, col3 = st.columns(3)
col1.metric("Total Tables Monitored", total_tables)
col2.metric("Tables Out of Sync", f"{out_of_sync_count}", delta=f"{out_of_sync_count}", delta_color="inverse")
col3.metric(f"Stale Tables (> {STALE_THRESHOLD_HOURS}h)", f"{stale_count}", delta=f"{stale_count}", delta_color="inverse")


# --- Detailed Table View ---
st.header("Detailed Table Status")

def style_rows(row):
    """
    Applies high-contrast background and text colors to rows 
    based on their sync status for improved readability.
    """
    # Red (Out of Sync)
    RED_BG = '#FFCDD2'
    RED_TEXT = '#9C0006'
    
    # Yellow (Stale)
    YELLOW_BG = '#FFF9C4'
    YELLOW_TEXT = '#5F4300'
    
    # Green (In Sync)
    GREEN_BG = '#C8E6C9'
    GREEN_TEXT = '#00600F'

    if row['ROW_COUNT_DIFFERENCE'] != 0:
        style = f'background-color: {RED_BG}; color: {RED_TEXT}'
    elif row['LATENCY_SECONDS'] > (STALE_THRESHOLD_HOURS * 3600):
        style = f'background-color: {YELLOW_BG}; color: {YELLOW_TEXT}'
    else:
        style = f'background-color: {GREEN_BG}; color: {GREEN_TEXT}'
        
    return [style] * len(row)

display_df = filtered_df.copy()
display_df['Table'] = display_df['MSSQL_TABLE_NAME'].str.split('.').str[-1]
display_df['Status'] = display_df['SYNC_JOB_STATUS'].apply(format_job_status)

COLUMNS_TO_SHOW_IN_TABLE = [
    'Status',
    'Table',
    'LATENCY_SECONDS',
    'ROW_COUNT_DIFFERENCE'
]

styled_df = display_df.style \
    .apply(style_rows, axis=1) \
    .format({
        'LATENCY_SECONDS': format_latency 
    }) \
    .hide(axis="index")

st.dataframe(
    styled_df,
    column_order=COLUMNS_TO_SHOW_IN_TABLE,
    use_container_width=True,
    hide_index=True,
    on_select="rerun",
    key="status_table",
    selection_mode="single-row",
    column_config={
        "LATENCY_SECONDS": st.column_config.NumberColumn(
            "Last Synced",
            help="Time since last sync job. Click header to sort correctly by duration."
        ),
        "ROW_COUNT_DIFFERENCE": st.column_config.NumberColumn("Difference", format="%d"),
        "Table": st.column_config.TextColumn("Table"),
        "Status": st.column_config.TextColumn("Status"),
        "MONITORING_TIMESTAMP": None,
        "MSSQL_TABLE_NAME": None,
        "SNOWFLAKE_TABLE_NAME": None,
        "MSSQL_ROW_COUNT": None,
        "SNOWFLAKE_ROW_COUNT": None,
        "LAST_SYNC_TIMESTAMP_UTC": None,
        "SYNC_JOB_STATUS": None,
        "SYNC_STATUS": None,
        "LAST_ERROR_MESSAGE": None,
    }
)

selection = st.session_state.status_table.get("selection", {"rows": []})

if selection["rows"]:
    selected_row_index = selection["rows"][0]
    selected_row_data = filtered_df.iloc[selected_row_index]
    
    st.markdown("---")
    st.subheader(f"🔍 Details for `{selected_row_data['MSSQL_TABLE_NAME']}` {format_job_status(selected_row_data['SYNC_JOB_STATUS'])}")
    
    if selected_row_data['SYNC_JOB_STATUS'] == 'failed':
        error_message = selected_row_data['LAST_ERROR_MESSAGE'] 
        if pd.notna(error_message) and error_message.strip():
            with st.expander("Show Full Error Message", expanded=True):
                st.code(error_message, language=None)
        else:
            st.info("No error message recorded for this sync.")
    
    col1, col2 = st.columns(2)
    
    with col1:
        st.metric(
            label="Last Checked",
            value=selected_row_data['MONITORING_TIMESTAMP'].strftime('%Y-%m-%d %H:%M:%S')
        )
    with col2:
         st.metric(
            label="Last Sync Job",
            value=selected_row_data['LAST_SYNC_TIMESTAMP_UTC'].strftime('%Y-%m-%d %H:%M:%S')
        )

    col3, col4 = st.columns(2)

    with col3:
        st.metric(
            label="Row Count in SQL Server",
            value=f"{selected_row_data['MSSQL_ROW_COUNT']:,}"
        )
    with col4:
         st.metric(
            label="Row Count in Snowflake",
            value=f"{selected_row_data['SNOWFLAKE_ROW_COUNT']:,}"
        )

    st.markdown("---") 

col1, col2 = st.columns(2)

with col1:
    st.markdown("##### Job Status Legend")
    st.text("✅ Done: Sync job completed.")
    st.text("❌ Failed: Sync job failed.")
    st.text("⏳ Syncing: Job is in progress.")
    st.text("🕒 Pending: Job is waiting to run.")
    st.text("❓ Unknown: Status not recognized.")

with col2:
    st.markdown("##### Row Color Legend")
    st.markdown("<div style='background-color:#C8E6C9; color:#00600F; padding: 5px; border-radius: 5px;'><b>Green</b>: Table is in sync and healthy.</div>", unsafe_allow_html=True)
    st.markdown("<div style='background-color:#FFF9C4; color:#5F4300; padding: 5px; border-radius: 5px; margin-top: 5px;'><b>Yellow</b>: Table is stale (not synced recently).</div>", unsafe_allow_html=True)
    st.markdown("<div style='background-color:#FFCDD2; color:#9C0006; padding: 5px; border-radius: 5px; margin-top: 5px;'><b>Red</b>: Out of sync (row count mismatch).</div>", unsafe_allow_html=True)


# --- Answers Sync Section (NEW) ---
st.header("Answers Sync Health")

try:
    missing_responses, mismatched_responses = load_answers_sync_stats(session)
except Exception as e:
    st.error(f"Could not fetch Answers sync statistics. Error: {e}")
    missing_responses, mismatched_responses = 0, 0

col1, col2 = st.columns(2)
col1.metric("Missing Responses", f"{missing_responses:,}", delta=f"{missing_responses:,}", delta_color="inverse")
col2.metric("Mismatched Response Counts", f"{mismatched_responses:,}", delta=f"{mismatched_responses:,}", delta_color="inverse")

if missing_responses > 0 or mismatched_responses > 0:
    st.warning("Attention: There are missing or mismatched Answers responses. Investigate source and sync jobs.")
else:
    st.success("No missing or mismatched responses detected. Answers sync is healthy.")

st.markdown("---")