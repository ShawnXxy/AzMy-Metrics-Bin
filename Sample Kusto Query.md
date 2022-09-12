
````sql
//////////////////////////////
//  MySQL DML
/////////////////////////////
AzMyStatus_CL
| where Time_t >= ago(1h)
| where LogicalServerName_s =~ "{replace to your server name here}"
| where MetricName_s in (
    'QUERIES',
    'COM_DELETE', 
    'COM_DELETE_MULTI', 
    'COM_INSERT', 
    'COM_INSERT_SELECT', 
    'COM_SELECT', 
    'COM_UPDATE', 
    'COM_UPDATE_MULTI')         
| project Time_t, MetricName_s, MetricValue_s
| extend Queries_tmp = iff(MetricName_s == 'QUERIES', toreal(MetricValue_s), 0.0),
            Com_delete_tmp = iff(MetricName_s == 'COM_DELETE', toreal(MetricValue_s), 0.0),
            Com_delete_multi_tmp = iff(MetricName_s == 'COM_DELETE_MULTI', toreal(MetricValue_s), 0.0),
            Com_insert_tmp = iff(MetricName_s == 'COM_INSERT', toreal(MetricValue_s), 0.0),
            Com_insert_select_tmp = iff(MetricName_s == 'COM_INSERT_SELECT', toreal(MetricValue_s), 0.0),
            Com_select_tmp = iff(MetricName_s == 'COM_SELECT', toreal(MetricValue_s), 0.0),
            Com_update_tmp = iff(MetricName_s == 'COM_UPDATE', toreal(MetricValue_s), 0.0),
            Com_update_multi_tmp = iff(MetricName_s == 'COM_UPDATE_MULTI', toreal(MetricValue_s), 0.0)
| summarize Queries = max(Queries_tmp),
            Com_delete = max(Com_delete_tmp),
            Com_delete_multi = max(Com_delete_multi_tmp),
            Com_insert = max(Com_insert_tmp),
            Com_insert_select = max(Com_insert_select_tmp),
            Com_select = max(Com_select_tmp),
            Com_update = max(Com_update_tmp),
            Com_update_multi = max(Com_update_multi_tmp)
            by Time_t
| order by Time_t asc
| render timechart


//////////////////////////////
//  MySQL IO request
/////////////////////////////
AzMyStatus_CL
| where Time_t >= ago(1h)
| where LogicalServerName_s =~ "{replace to your server name here}"
| where  MetricName_s  in (
    'BINLOG_IO_READ', 
    'BINLOG_IO_WRITTEN', 
    'BINLOG_DATA_READ', 
    'BINLOG_DATA_READS',
    'INNODB_DATA_READS',
    'INNODB_DATA_READ', 
    'INNODB_DATA_WRITTEN', 
    'INNODB_DATA_WRITES', 
    'INNODB_LOG_WRITES', 
    'INNODB_LOG_WRITTEN')
| extend Binlog_io_read_tmp = iff(MetricName_s =~ 'BINLOG_IO_READ', toreal(MetricValue_s), 0.0),
         Binlog_io_written_tmp = iff(MetricName_s =~ 'BINLOG_IO_WRITTEN', toreal(MetricValue_s), 0.0),
         Innodb_data_read_tmp = iff(MetricName_s =~ 'INNODB_DATA_READ', toreal(MetricValue_s), 0.0),
         Innodb_data_reads_tmp = iff(MetricName_s =~ 'INNODB_DATA_READS', toreal(MetricValue_s), 0.0),
         Innodb_data_writes_tmp = iff(MetricName_s =~ 'INNODB_DATA_WRITES', toreal(MetricValue_s), 0.0),
         Innodb_data_written_tmp = iff(MetricName_s =~ 'INNODB_DATA_WRITTEN', toreal(MetricValue_s), 0.0),
         Innodb_log_writes_tmp = iff(MetricName_s =~ 'INNODB_LOG_WRITES', toreal(MetricValue_s), 0.0),
         Innodb_log_written_tmp = iff(MetricName_s =~ 'INNODB_LOG_WRITTEN', toreal(MetricValue_s), 0.0)
| summarize Binlog_io_read_in_MB = max(Binlog_io_read_tmp)/1024/1024,
            Binlog_io_written_in_MB = max(Binlog_io_written_tmp)/1024/1024,
            Innodb_data_read_in_MB = max(Innodb_data_read_tmp)/1024/1024,
            Innodb_data_reads = max(Innodb_data_reads_tmp),
            Innodb_data_writes = max(Innodb_data_writes_tmp),
            Innodb_data_written_in_MB = max(Innodb_data_written_tmp)/1024/1024,
            Innodb_log_writes = max(Innodb_log_writes_tmp),
            Innodb_log_written_in_MB = max(Innodb_log_written_tmp)/1024/1024
            by Time_t
| order by Time_t asc
| render timechart


//////////////////////////////
//  MySQL temp table usage
/////////////////////////////
AzMyStatus_CL
| where Time_t >= ago(1h)
| where LogicalServerName_s =~ "{replace to your server name here}"
| where MetricName_s in (
    'CREATED_TMP_DISK_TABLES', 
    'CREATED_TMP_FILES', 
    'CREATED_TMP_TABLES')
| project Time_t, MetricName_s, MetricValue_s
| extend Created_tmp_disk_tables_tmp = iff(MetricName_s == 'CREATED_TMP_DISK_TABLES', toreal(MetricValue_s), 0.0), 
         Created_tmp_files_tmp = iff(MetricName_s == 'CREATED_TMP_FILES', toreal(MetricValue_s), 0.0),
         Created_tmp_tables_tmp = iff(MetricName_s == 'CREATED_TMP_TABLES', toreal(MetricValue_s), 0.0)
| summarize Created_tmp_disk_tables = max(Created_tmp_disk_tables_tmp),
            Created_tmp_files = max(Created_tmp_files_tmp),
            Created_tmp_tables = max(Created_tmp_tables_tmp) by Time_t
| order by Time_t asc
| render timechart 


//////////////////////////////
//  MySQL buffer pool usage
/////////////////////////////
AzMyStatus_CL
| where Time_t >= ago(1h)
| where LogicalServerName_s =~ "{replace to your server name here}"
| where MetricName_s in (
    'INNODB_BUFFER_POOL_READ_REQUESTS', 
    'INNODB_BUFFER_POOL_READS', 
    'INNODB_BUFFER_POOL_WRITE_REQUESTS', 
    'INNODB_BUFFER_POOL_PAGES_DIRTY', 
    'INNODB_BUFFER_POOL_WAIT_FREE')
| project Time_t, MetricName_s, MetricValue_s
| extend Innodb_buffer_pool_read_requests_tmp = iff(MetricName_s == 'INNODB_BUFFER_POOL_READ_REQUESTS', toreal(MetricValue_s), 0.0), 
         Innodb_buffer_pool_reads_tmp = iff(MetricName_s == 'INNODB_BUFFER_POOL_READS', toreal(MetricValue_s), 0.0),
         Innodb_buffer_pool_write_requests_tmp = iff(MetricName_s == 'INNODB_BUFFER_POOL_WRITE_REQUESTS', toreal(MetricValue_s), 0.0),
         Innodb_buffer_pool_pages_dirty_tmp = iff(MetricName_s == 'INNODB_BUFFER_POOL_PAGES_DIRTY', toreal(MetricValue_s), 0.0),
         Innodb_buffer_pool_wait_free_tmp = iff(MetricName_s == 'INNODB_BUFFER_POOL_WAIT_FREE', toreal(MetricValue_s), 0.0)
| summarize Innodb_buffer_pool_read_requests = max(Innodb_buffer_pool_read_requests_tmp), 
            Innodb_buffer_pool_reads = max(Innodb_buffer_pool_reads_tmp),
            Innodb_buffer_pool_write_requests = max(Innodb_buffer_pool_write_requests_tmp),
            Innodb_buffer_pool_pages_dirty = max(Innodb_buffer_pool_pages_dirty_tmp),
            Innodb_buffer_pool_wait_free = max(Innodb_buffer_pool_wait_free_tmp) by Time_t
| extend Innodb_buffer_pool_reads_cache_hit_percentage = round((Innodb_buffer_pool_read_requests-Innodb_buffer_pool_reads)*100/Innodb_buffer_pool_read_requests,3)
| order by Time_t asc
| render timechart 
````