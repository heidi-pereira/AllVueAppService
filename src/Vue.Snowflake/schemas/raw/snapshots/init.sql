
-- Daily snapshots (Monday-Saturday, expire after 7 days)
create or replace snapshot policy impl_response_set.daily_short_term_policy
  SCHEDULE = 'USING CRON 0 4 * * 1-6 UTC'  -- Mon-Sat at 4 AM UTC
  EXPIRE_AFTER_DAYS = 7
  COMMENT = 'Daily snapshots Mon-Sat, 7-day retention';

-- Weekly snapshots (Sundays only, expire after 35 days)
create or replace snapshot policy impl_response_set.weekly_long_term_policy
  SCHEDULE = 'USING CRON 0 4 * * 0 UTC'    -- Sundays at 4 AM UTC
  EXPIRE_AFTER_DAYS = 35
  COMMENT = 'Weekly snapshots on Sundays, 5-week retention';

-- Create snapshot set for daily backups
create or replace snapshot set impl_response_set.daily_snapshots
for database live__vue
with snapshot policy impl_response_set.daily_short_term_policy;

-- Create snapshot set for weekly backups
create or replace snapshot set impl_response_set.weekly_snapshots
  for database live__vue
  with snapshot policy impl_response_set.weekly_long_term_policy;
